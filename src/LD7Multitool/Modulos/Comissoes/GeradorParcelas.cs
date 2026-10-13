using System.Globalization;
using System.Text.RegularExpressions;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Gera as linhas de comissão de uma venda parcelada, do jeito que a planilha
/// era preenchida à mão: valor dividido em parcelas, boletos numerados em
/// sequência e vencimentos a cada N dias (ou todo mês).
/// </summary>
public static partial class GeradorParcelas
{
    /// <summary>Opções de intervalo entre vencimentos; 0 dias = mensal (mesmo dia do mês).</summary>
    public static readonly (string Descricao, int Dias)[] Intervalos =
    {
        ("A cada 7 dias", 7),
        ("A cada 14 dias", 14),
        ("A cada 15 dias", 15),
        ("A cada 21 dias", 21),
        ("A cada 28 dias", 28),
        ("A cada 30 dias", 30),
        ("Mensal (mesmo dia do mês)", 0),
    };

    /// <summary>
    /// Cria uma linha por parcela, copiando NF/cliente/pedido/percentual do
    /// <paramref name="modelo"/>.
    /// </summary>
    public static List<Comissao> Gerar(Comissao modelo, int quantidade, DateTime primeiroVencimento,
        int intervaloDias, string primeiroBoleto)
    {
        quantidade = Math.Max(1, quantidade);
        var valores = DividirValor(modelo.ValorNf, quantidade);
        var parcelas = new List<Comissao>(quantidade);
        for (var i = 0; i < quantidade; i++)
        {
            var parcela = modelo.Clonar();
            parcela.Id = 0;
            parcela.Parcela = RotuloParcela(i, quantidade);
            parcela.ValorParcela = valores[i];
            parcela.NumeroBoleto = NumeroBoleto(primeiroBoleto, i);
            parcela.Vencimento = intervaloDias > 0
                ? primeiroVencimento.Date.AddDays((double)intervaloDias * i)
                : primeiroVencimento.Date.AddMonths(i);
            parcela.Paga = false;
            parcela.DataPagamento = null;
            parcelas.Add(parcela);
        }
        return parcelas;
    }

    /// <summary>
    /// Divide o valor em parcelas truncando nos centavos; a última absorve a
    /// diferença. Ex.: 17.230,40 em 13 → 12 × 1.325,41 + 1.325,48 (como nos
    /// boletos da planilha).
    /// </summary>
    public static decimal[] DividirValor(decimal total, int quantidade)
    {
        var valores = new decimal[quantidade];
        var parcela = Math.Floor(total / quantidade * 100m) / 100m;
        for (var i = 0; i < quantidade - 1; i++)
            valores[i] = parcela;
        valores[quantidade - 1] = total - parcela * (quantidade - 1);
        return valores;
    }

    /// <summary>"01/13", "02/13"... ou "UNICA" quando é à vista.</summary>
    public static string RotuloParcela(int indice, int quantidade) =>
        quantidade == 1 ? "UNICA" : $"{indice + 1:00}/{quantidade:00}";

    /// <summary>
    /// Número do boleto da parcela <paramref name="deslocamento"/> (0 = a primeira).
    /// No formato "364-6" a sequência continua 364-7, 364-8, 364-9, 365-0...;
    /// qualquer outro texto (ex.: "PIX") se repete em todas as parcelas.
    /// </summary>
    public static string NumeroBoleto(string primeiro, int deslocamento)
    {
        var texto = primeiro.Trim();
        var correspondencia = FormatoBoleto().Match(texto);
        if (!correspondencia.Success || deslocamento == 0)
            return texto;

        var digitos = correspondencia.Groups[1].Value + correspondencia.Groups[2].Value;
        if (!long.TryParse(digitos, out var numero))
            return texto;
        var proximo = (numero + deslocamento).ToString("D" + digitos.Length, CultureInfo.InvariantCulture);
        return proximo[..^1] + "-" + proximo[^1];
    }

    [GeneratedRegex(@"^(\d+)-(\d)$")]
    private static partial Regex FormatoBoleto();
}
