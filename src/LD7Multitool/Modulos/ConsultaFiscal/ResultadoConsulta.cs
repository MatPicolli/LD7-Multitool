namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>Resultado da consulta de situação de um documento fiscal.</summary>
public sealed class ResultadoConsulta
{
    public bool Sucesso { get; init; }

    /// <summary>Mensagem de erro (rede, certificado, endpoint), quando não houve sucesso.</summary>
    public string? Erro { get; init; }

    public string TipoDocumento { get; init; } = "";
    public string ChaveFormatada { get; init; } = "";

    public int CStat { get; init; }
    public string XMotivo { get; init; } = "";
    public string Protocolo { get; init; } = "";
    public string DataHora { get; init; } = "";
    public string Ambiente { get; init; } = "";

    /// <summary>Considera-se autorizada/autêntica quando cStat = 100.</summary>
    public bool Autorizada => CStat == 100;

    public string Resumo => Sucesso
        ? $"{XMotivo} ({CStat})"
        : "Falha na consulta";
}
