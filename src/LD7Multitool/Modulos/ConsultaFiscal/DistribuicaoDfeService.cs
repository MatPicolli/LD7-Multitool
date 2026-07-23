using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>
/// Busca o XML completo de uma NF-e via NFeDistribuiçãoDFe (Ambiente Nacional).
/// Só retorna o documento completo quando o CNPJ configurado é parte da nota
/// (tipicamente o destinatário). Caso contrário, retorna null.
/// </summary>
public sealed class DistribuicaoDfeService
{
    private const string UrlProducao = "https://www1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
    private const string UrlHomologacao = "https://hom1.nfe.fazenda.gov.br/NFeDistribuicaoDFe/NFeDistribuicaoDFe.asmx";
    private const string Action = "http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe/nfeDistDFeInteresse";

    private readonly ConsultaFiscalConfig _config;

    public DistribuicaoDfeService(ConsultaFiscalConfig config) => _config = config;

    /// <summary>Nota detalhada (ou null) e um diagnóstico do que a SEFAZ respondeu.</summary>
    public sealed record Resultado(NotaFiscalDetalhada? Nota, string Diagnostico);

    public async Task<Resultado> ObterNotaAsync(ChaveAcesso chave)
    {
        if (!_config.CertificadoConfigurado)
            return new Resultado(null, "certificado não configurado");
        if (_config.CnpjSomenteDigitos.Length != 14)
            return new Resultado(null, $"CNPJ da empresa inválido/não configurado (\"{_config.Cnpj}\") — preencha nas configurações ⚙");

        var envelope =
            "<soap12:Envelope xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<soap12:Body>" +
            "<nfeDistDFeInteresse xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeDistribuicaoDFe\">" +
            "<nfeDadosMsg>" +
            "<distDFeInt versao=\"1.01\" xmlns=\"http://www.portalfiscal.inf.br/nfe\">" +
            $"<tpAmb>{_config.Ambiente}</tpAmb>" +
            $"<cUFAutor>{_config.UfEmpresaCodigo}</cUFAutor>" +
            $"<CNPJ>{_config.CnpjSomenteDigitos}</CNPJ>" +
            $"<consChNFe><chNFe>{chave.Digitos}</chNFe></consChNFe>" +
            "</distDFeInt>" +
            "</nfeDadosMsg>" +
            "</nfeDistDFeInteresse>" +
            "</soap12:Body></soap12:Envelope>";

        try
        {
            var endpoint = _config.Producao ? UrlProducao : UrlHomologacao;
            var respostaXml = await EnviarAsync(endpoint, envelope);

            var doc = XDocument.Parse(respostaXml);
            var cStat = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "cStat")?.Value ?? "?";
            var xMotivo = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "xMotivo")?.Value ?? "";

            var nota = ExtrairNota(doc);
            if (nota is not null)
                return new Resultado(nota, $"OK (cStat {cStat})");

            var temDocZip = doc.Descendants().Any(e => e.Name.LocalName == "docZip");
            var msg = temDocZip
                ? $"SEFAZ devolveu documento, mas não a nota completa (cStat {cStat}: {xMotivo})"
                : $"cStat {cStat}: {xMotivo}";
            return new Resultado(null, msg);
        }
        catch (Exception ex)
        {
            return new Resultado(null, "erro na requisição: " + ex.Message);
        }
    }

    private async Task<string> EnviarAsync(string endpoint, string envelope)
    {
        using var certificado = X509CertificateLoader.LoadPkcs12FromFile(
            _config.CaminhoCertificado, _config.SenhaCertificado,
            X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.PersistKeySet);

        using var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
        };
        handler.ClientCertificates.Add(certificado);

        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        using var conteudo = new StringContent(envelope, new UTF8Encoding(false));
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml") { CharSet = "utf-8" };
        conteudo.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("action", $"\"{Action}\""));

        using var resposta = await http.PostAsync(endpoint, conteudo);
        return await resposta.Content.ReadAsStringAsync();
    }

    private static NotaFiscalDetalhada? ExtrairNota(XDocument doc)
    {
        // Cada docZip é um XML (procNFe, resNFe ou evento) em base64+gzip.
        foreach (var docZip in doc.Descendants().Where(e => e.Name.LocalName == "docZip"))
        {
            var xml = DescompactarGzip(docZip.Value);
            if (xml is null)
                continue;

            var raizLocal = ObterNomeRaiz(xml);
            // Só a nota completa (nfeProc) tem produtos/emitente/destinatário.
            if (raizLocal == "nfeProc")
            {
                var nota = NotaFiscalDetalhada.DeProcNFe(xml);
                if (nota is not null)
                    return nota;
            }
        }
        return null;
    }

    private static string? DescompactarGzip(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64.Trim());
            using var entrada = new MemoryStream(bytes);
            using var gzip = new GZipStream(entrada, CompressionMode.Decompress);
            using var leitor = new StreamReader(gzip, Encoding.UTF8);
            return leitor.ReadToEnd();
        }
        catch
        {
            return null;
        }
    }

    private static string ObterNomeRaiz(string xml)
    {
        try
        {
            return XDocument.Parse(xml).Root?.Name.LocalName ?? "";
        }
        catch
        {
            return "";
        }
    }
}
