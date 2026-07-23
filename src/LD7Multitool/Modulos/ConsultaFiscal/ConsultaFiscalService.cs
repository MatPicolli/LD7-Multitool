using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>
/// Consulta a situação de uma NF-e/CT-e no web service da SEFAZ, autenticando
/// com o certificado digital A1 (mTLS). Retorna a situação/protocolo — o que
/// se imprime no verso como comprovante de autenticidade.
/// </summary>
public sealed class ConsultaFiscalService
{
    private readonly ConsultaFiscalConfig _config;

    public ConsultaFiscalService(ConsultaFiscalConfig config) => _config = config;

    public async Task<ResultadoConsulta> ConsultarAsync(ChaveAcesso chave)
    {
        if (!_config.CertificadoConfigurado)
        {
            return new ResultadoConsulta
            {
                Sucesso = false,
                Erro = "Certificado digital não configurado (abra as configurações ⚙).",
                TipoDocumento = chave.TipoDescricao,
                ChaveFormatada = chave.Formatada,
            };
        }

        var ehCte = chave.Modelo == ModeloDocumento.CTe;
        var endpoint = ehCte
            ? _config.ResolverEndpointCte(chave.CodigoUf)
            : _config.ResolverEndpointNfe(chave.CodigoUf);
        var envelope = ehCte ? MontarEnvelopeCte(chave) : MontarEnvelopeNfe(chave);
        var action = ehCte
            ? "http://www.portalfiscal.inf.br/cte/wsdl/CTeConsultaV4/cteConsultaCT"
            : "http://www.portalfiscal.inf.br/nfe/wsdl/NFeConsultaProtocolo4/nfeConsultaNF";

        try
        {
            var respostaXml = await EnviarSoapAsync(endpoint, envelope, action);
            return Parsear(respostaXml, chave);
        }
        catch (Exception ex)
        {
            return new ResultadoConsulta
            {
                Sucesso = false,
                Erro = ex.Message,
                TipoDocumento = chave.TipoDescricao,
                ChaveFormatada = chave.Formatada,
            };
        }
    }

    // IMPORTANTE: a SEFAZ rejeita (588) espaços/quebras de linha entre as tags.
    // O XML precisa ir "achatado", sem indentação nem declaração <?xml?>.
    private string MontarEnvelopeNfe(ChaveAcesso chave) =>
        "<soap12:Envelope xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap12:Body>" +
        "<nfeDadosMsg xmlns=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeConsultaProtocolo4\">" +
        "<consSitNFe versao=\"4.00\" xmlns=\"http://www.portalfiscal.inf.br/nfe\">" +
        $"<tpAmb>{_config.Ambiente}</tpAmb><xServ>CONSULTAR</xServ><chNFe>{chave.Digitos}</chNFe>" +
        "</consSitNFe></nfeDadosMsg></soap12:Body></soap12:Envelope>";

    private string MontarEnvelopeCte(ChaveAcesso chave) =>
        "<soap12:Envelope xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">" +
        "<soap12:Body>" +
        "<cteDadosMsg xmlns=\"http://www.portalfiscal.inf.br/cte/wsdl/CTeConsultaV4\">" +
        "<consSitCTe versao=\"4.00\" xmlns=\"http://www.portalfiscal.inf.br/cte\">" +
        $"<tpAmb>{_config.Ambiente}</tpAmb><xServ>CONSULTAR</xServ><chCTe>{chave.Digitos}</chCTe>" +
        "</consSitCTe></cteDadosMsg></soap12:Body></soap12:Envelope>";

    private async Task<string> EnviarSoapAsync(string endpoint, string envelope, string action)
    {
        // UserKeySet (não exige admin) + PersistKeySet: necessário no Windows
        // para o schannel enxergar a chave privada do PFX na autenticação TLS.
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
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("application/soap+xml")
        {
            CharSet = "utf-8",
        };
        // No SOAP 1.2 a ação vai como parâmetro do Content-Type.
        conteudo.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("action", $"\"{action}\""));

        using var resposta = await http.PostAsync(endpoint, conteudo);
        var corpo = await resposta.Content.ReadAsStringAsync();
        if (!resposta.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)resposta.StatusCode}: {resposta.ReasonPhrase}\n{corpo}");
        return corpo;
    }

    /// <summary>Extrai situação/protocolo do XML de retorno, ignorando namespaces.</summary>
    private static ResultadoConsulta Parsear(string xml, ChaveAcesso chave)
    {
        var doc = XDocument.Parse(xml);

        // cStat/xMotivo do nível superior (retConsSitNFe/retConsSitCTe).
        var cStatTopo = PrimeiroValor(doc, "cStat");
        var xMotivoTopo = PrimeiroValor(doc, "xMotivo");
        var tpAmb = PrimeiroValor(doc, "tpAmb");

        // Dentro de protNFe/protCTe (infProt) ficam o protocolo e a data, e um
        // cStat 100 quando autorizada — preferimos esse quando existir.
        var infProt = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "infProt");
        var cStat = infProt is not null ? ValorLocal(infProt, "cStat") ?? cStatTopo : cStatTopo;
        var xMotivo = infProt is not null ? ValorLocal(infProt, "xMotivo") ?? xMotivoTopo : xMotivoTopo;
        var nProt = infProt is not null ? ValorLocal(infProt, "nProt") ?? "" : "";
        var dhRecbto = infProt is not null ? ValorLocal(infProt, "dhRecbto") ?? "" : "";

        return new ResultadoConsulta
        {
            Sucesso = true,
            TipoDocumento = chave.TipoDescricao,
            ChaveFormatada = chave.Formatada,
            CStat = int.TryParse(cStat, out var c) ? c : 0,
            XMotivo = xMotivo ?? "",
            Protocolo = nProt,
            DataHora = FormatarData(dhRecbto),
            Ambiente = tpAmb == "2" ? "Homologação" : "Produção",
        };
    }

    private static string? PrimeiroValor(XDocument doc, string nomeLocal) =>
        doc.Descendants().FirstOrDefault(e => e.Name.LocalName == nomeLocal)?.Value;

    private static string? ValorLocal(XElement pai, string nomeLocal) =>
        pai.Descendants().FirstOrDefault(e => e.Name.LocalName == nomeLocal)?.Value;

    private static string FormatarData(string iso)
    {
        if (DateTimeOffset.TryParse(iso, out var data))
            return data.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss");
        return iso;
    }
}
