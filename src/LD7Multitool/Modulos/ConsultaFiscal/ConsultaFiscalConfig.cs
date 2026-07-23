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
    // Padrões SVRS (atende SC). Confirme/ajuste conforme a UF/autorizador.
    public const string UrlNfeProducaoPadrao = "https://nfe.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx";
    public const string UrlNfeHomologacaoPadrao = "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx";
    public const string UrlCteProducaoPadrao = "https://cte.svrs.rs.gov.br/ws/cteConsulta/CTeConsulta.asmx";
    public const string UrlCteHomologacaoPadrao = "https://cte-homologacao.svrs.rs.gov.br/ws/cteConsulta/CTeConsulta.asmx";

    public string CaminhoCertificado { get; set; } = "";
    public string SenhaCertificado { get; set; } = "";

    /// <summary>1 = Produção, 2 = Homologação.</summary>
    public int Ambiente { get; set; } = 1;

    public bool ImprimirAutomaticamente { get; set; } = true;

    /// <summary>Nome da impressora; vazio = impressora padrão do Windows.</summary>
    public string Impressora { get; set; } = "";

    public string UrlNfe { get; set; } = "";
    public string UrlCte { get; set; } = "";

    public bool Producao => Ambiente != 2;

    public string EndpointNfe =>
        !string.IsNullOrWhiteSpace(UrlNfe) ? UrlNfe
        : Producao ? UrlNfeProducaoPadrao : UrlNfeHomologacaoPadrao;

    public string EndpointCte =>
        !string.IsNullOrWhiteSpace(UrlCte) ? UrlCte
        : Producao ? UrlCteProducaoPadrao : UrlCteHomologacaoPadrao;

    public bool CertificadoConfigurado =>
        !string.IsNullOrWhiteSpace(CaminhoCertificado) && File.Exists(CaminhoCertificado);

    public static ConsultaFiscalConfig Carregar() => new()
    {
        CaminhoCertificado = Obter("consulta_cert_caminho"),
        SenhaCertificado = DesprotegerSenha(ConfiguracaoRepository.Obter("consulta_cert_senha")),
        Ambiente = int.TryParse(Obter("consulta_ambiente"), out var a) ? a : 1,
        ImprimirAutomaticamente = Obter("consulta_autoimprimir") != "0",
        Impressora = Obter("consulta_impressora"),
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
