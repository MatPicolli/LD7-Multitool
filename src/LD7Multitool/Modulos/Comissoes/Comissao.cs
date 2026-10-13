using System.Globalization;
using System.Text;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Uma linha da planilha de comissões: a comissão de <b>uma parcela</b> de uma
/// venda (NF). Uma NF em 13 vezes vira 13 linhas, como na planilha.
/// </summary>
public class Comissao
{
    public long Id { get; set; }

    public string NotaFiscal { get; set; } = "";

    /// <summary>Número do boleto da parcela (ex.: "364-6"), ou "PIX".</summary>
    public string NumeroBoleto { get; set; } = "";

    public string Cliente { get; set; } = "";

    /// <summary>Referência do pedido (número, "SEM PEDIDO", nome de quem fez...).</summary>
    public string RefPedido { get; set; } = "";

    public decimal ValorNf { get; set; }
    public DateTime Vencimento { get; set; } = DateTime.Today;

    /// <summary>Texto da parcela, como na planilha: "01/13", "UNICA"...</summary>
    public string Parcela { get; set; } = "";

    public decimal ValorParcela { get; set; }

    /// <summary>
    /// Percentual da comissão gravado em cada linha (e não só numa configuração
    /// global), para que mudar o padrão no futuro não altere o histórico.
    /// </summary>
    public decimal Percentual { get; set; } = 10m;

    /// <summary>Se a comissão desta parcela já foi paga ("Status da comissão").</summary>
    public bool Paga { get; set; }

    /// <summary>Quando a comissão foi paga ("pag. comissão").</summary>
    public DateTime? DataPagamento { get; set; }

    /// <summary>Valor da comissão (sem arredondar, igual à fórmula da planilha =10%*parcela).</summary>
    public decimal ValorComissao => ValorParcela * Percentual / 100m;

    public string StatusDescricao => Paga ? "Pago" : "Não pago";

    /// <summary>Comissão ainda não paga de uma parcela cujo vencimento já passou.</summary>
    public bool VencidaNaoPaga => !Paga && Vencimento.Date < DateTime.Today;

    /// <summary>
    /// Identifica a parcela para a importação não cadastrar a mesma linha duas
    /// vezes: NF + boleto + parcela + vencimento + valor.
    /// </summary>
    public string ChaveImportacao => string.Join('|',
        Normalizar(NotaFiscal),
        Normalizar(NumeroBoleto),
        Normalizar(Parcela),
        Vencimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ValorParcela.ToString("0.00", CultureInfo.InvariantCulture));

    public Comissao Clonar() => (Comissao)MemberwiseClone();

    /// <summary>Minúsculas, sem acentos e sem espaços nas pontas.</summary>
    public static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                construtor.Append(char.ToLowerInvariant(caractere));
        }
        return construtor.ToString();
    }
}
