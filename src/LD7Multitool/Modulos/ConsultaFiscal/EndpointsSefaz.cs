namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>
/// Endpoints de "Consulta Protocolo" da SEFAZ por UF (código da chave).
/// Cada estado é atendido pela sua própria SEFAZ ou por um ambiente virtual
/// (SVRS ou SVAN). A UF vem nos 2 primeiros dígitos da chave.
///
/// URLs de produção. As dos grandes autorizadores (SP, RS, SVRS, SVAN) são as
/// mais consolidadas; as demais são o melhor conhecido e podem precisar de
/// ajuste — nesse caso dá para forçar a URL na configuração (⚙).
/// </summary>
public static class EndpointsSefaz
{
    private const string SvrsNfe = "https://nfe.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx";
    private const string SvrsNfeHom = "https://nfe-homologacao.svrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx";
    private const string SvanNfe = "https://www.sefazvirtual.fazenda.gov.br/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx";
    private const string SvrsCte = "https://cte.svrs.rs.gov.br/ws/cteConsulta/CTeConsulta.asmx";
    private const string SvrsCteHom = "https://cte-homologacao.svrs.rs.gov.br/ws/cteConsulta/CTeConsulta.asmx";

    // NF-e (modelo 55/65) — produção, por código de UF.
    private static readonly Dictionary<string, string> NfeProducao = new()
    {
        ["35"] = "https://nfe.fazenda.sp.gov.br/ws/nfeconsultaprotocolo4.asmx",              // SP
        ["31"] = "https://nfe.fazenda.mg.gov.br/nfe2/services/NFeConsultaProtocolo4",        // MG
        ["41"] = "https://nfe.sefa.pr.gov.br/nfe/NFeConsultaProtocolo4",                     // PR
        ["43"] = "https://nfe.sefazrs.rs.gov.br/ws/NfeConsulta/NfeConsulta4.asmx",           // RS
        ["51"] = "https://nfe.sefaz.mt.gov.br/nfews/v2/services/NfeConsulta4",               // MT
        ["50"] = "https://nfe.fazenda.ms.gov.br/ws/NFeConsultaProtocolo4",                   // MS
        ["29"] = "https://nfe.sefaz.ba.gov.br/webservices/NFeConsultaProtocolo4/NFeConsultaProtocolo4.asmx", // BA
        ["52"] = "https://nfe.sefaz.go.gov.br/nfe/services/NFeConsultaProtocolo4",           // GO
        ["23"] = "https://nfe.sefaz.ce.gov.br/nfe4/services/NFeConsultaProtocolo4",          // CE
        ["26"] = "https://nfe.sefaz.pe.gov.br/nfe-service/services/NFeConsultaProtocolo4",   // PE
        ["13"] = "https://nfe.sefaz.am.gov.br/services2/services/NfeConsulta4",              // AM
        // SVAN (Ambiente Nacional): MA, PA.
        ["21"] = SvanNfe,
        ["15"] = SvanNfe,
        // Demais UFs são atendidas pelo SVRS (mapeadas abaixo, no fallback).
    };

    private static readonly Dictionary<string, string> NfeHomologacao = new()
    {
        ["35"] = "https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeconsultaprotocolo4.asmx",
    };

    public static string Nfe(string codigoUf, bool producao)
    {
        if (producao)
            return NfeProducao.GetValueOrDefault(codigoUf, SvrsNfe);
        return NfeHomologacao.GetValueOrDefault(codigoUf, SvrsNfeHom);
    }

    // CT-e (modelo 57/67) — a maioria dos estados é atendida pelo SVRS;
    // SP tem serviço próprio.
    public static string Cte(string codigoUf, bool producao)
    {
        if (codigoUf == "35")
            return producao
                ? "https://nfe.fazenda.sp.gov.br/CTeWS/WS/CTeConsultaV4.asmx"
                : "https://homologacao.nfe.fazenda.sp.gov.br/CTeWS/WS/CTeConsultaV4.asmx";
        return producao ? SvrsCte : SvrsCteHom;
    }
}
