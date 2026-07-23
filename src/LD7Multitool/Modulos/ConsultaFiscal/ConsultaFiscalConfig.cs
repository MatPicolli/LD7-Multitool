using System.Security.Cryptography;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>
/// Configuração do módulo de consulta: certificado A1, ambiente, impressora e
/// os endpoints da SEFAZ (editáveis, com padrões SVRS usados por SC).
/// </summary>
public class ConsultaFiscalConfig
{
    public string CaminhoCertificado { get; set; } = "";
    public string SenhaCertificado { get; set; } = "";

    /// <summary>1 = Produção, 2 = Homologação.</summary>
    public int Ambiente { get; set; } = 1;

    /// <summary>Se true, imprime assim que consulta; se false, espera Enter (para inserir a nota antes).</summary>
    public bool ImprimirAutomaticamente { get; set; }

    /// <summary>Nome da impressora; vazio = impressora padrão do Windows.</summary>
    public string Impressora { get; set; } = "";

    /// <summary>Buscar o conteúdo completo da nota (emitente/dest/produtos) via Distribuição DFe.</summary>
    public bool BuscarDetalhes { get; set; } = true;

    /// <summary>CNPJ da sua empresa (usado na Distribuição DFe — deve ser parte da nota).</summary>
    public string Cnpj { get; set; } = "";

    /// <summary>Código IBGE da UF da sua empresa (ex.: 42 = SC), usado como cUFAutor.</summary>
    public string UfEmpresaCodigo { get; set; } = "42";

    public string CnpjSomenteDigitos => new(Cnpj.Where(char.IsDigit).ToArray());

    // Overrides opcionais: se preenchidos, forçam a URL para TODAS as consultas,
    // ignorando o roteamento automático por UF. Deixe em branco para o automático.
    public string UrlNfe { get; set; } = "";
    public string UrlCte { get; set; } = "";

    public bool Producao => Ambiente != 2;

    /// <summary>Endpoint da NF-e para a UF da chave (ou o override, se definido).</summary>
    public string ResolverEndpointNfe(string codigoUf) =>
        !string.IsNullOrWhiteSpace(UrlNfe) ? UrlNfe : EndpointsSefaz.Nfe(codigoUf, Producao);

    /// <summary>Endpoint do CT-e para a UF da chave (ou o override, se definido).</summary>
    public string ResolverEndpointCte(string codigoUf) =>
        !string.IsNullOrWhiteSpace(UrlCte) ? UrlCte : EndpointsSefaz.Cte(codigoUf, Producao);

    public bool CertificadoConfigurado =>
        !string.IsNullOrWhiteSpace(CaminhoCertificado) && File.Exists(CaminhoCertificado);

    public static ConsultaFiscalConfig Carregar() => new()
    {
        CaminhoCertificado = Obter("consulta_cert_caminho"),
        SenhaCertificado = DesprotegerSenha(ConfiguracaoRepository.Obter("consulta_cert_senha")),
        Ambiente = int.TryParse(Obter("consulta_ambiente"), out var a) ? a : 1,
        ImprimirAutomaticamente = Obter("consulta_autoimprimir") == "1",
        Impressora = Obter("consulta_impressora"),
        BuscarDetalhes = Obter("consulta_detalhes") != "0",
        Cnpj = Obter("consulta_cnpj"),
        UfEmpresaCodigo = string.IsNullOrEmpty(Obter("consulta_uf")) ? "42" : Obter("consulta_uf"),
        UrlNfe = Obter("consulta_url_nfe"),
        UrlCte = Obter("consulta_url_cte"),
    };

    public void Gravar()
    {
        ConfiguracaoRepository.Definir("consulta_cert_caminho", CaminhoCertificado);
        ConfiguracaoRepository.Definir("consulta_cert_senha", ProtegerSenha(SenhaCertificado));
        ConfiguracaoRepository.Definir("consulta_ambiente", Ambiente.ToString());
        ConfiguracaoRepository.Definir("consulta_autoimprimir", ImprimirAutomaticamente ? "1" : "0");
        ConfiguracaoRepository.Definir("consulta_impressora", Impressora);
        ConfiguracaoRepository.Definir("consulta_detalhes", BuscarDetalhes ? "1" : "0");
        ConfiguracaoRepository.Definir("consulta_cnpj", Cnpj);
        ConfiguracaoRepository.Definir("consulta_uf", UfEmpresaCodigo);
        ConfiguracaoRepository.Definir("consulta_url_nfe", UrlNfe);
        ConfiguracaoRepository.Definir("consulta_url_cte", UrlCte);
    }

    private static string Obter(string chave) => ConfiguracaoRepository.Obter(chave) ?? "";

    // A senha do certificado é protegida com DPAPI (vinculada ao usuário Windows).
    private static string ProtegerSenha(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            return "";
        var protegida = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(senha), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protegida);
    }

    private static string DesprotegerSenha(string? protegida)
    {
        if (string.IsNullOrEmpty(protegida))
            return "";
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(protegida), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException) { return ""; }
        catch (FormatException) { return ""; }
    }
}
