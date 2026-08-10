using System.Text.RegularExpressions;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Leitura de valor e vencimento a partir da <b>linha digitável</b> de um
/// boleto — mesma regra usada em <c>LeitorBoletoPdf</c>, aqui aplicada a texto
/// vindo de páginas de portal (não de PDF).
///
/// Nos 14 dígitos finais da ficha de compensação, os 4 primeiros são o fator de
/// vencimento (dias desde 07/10/1997, com a numeração reiniciada em 1000 no dia
/// 22/02/2025 pela circular FEBRABAN) e os 10 seguintes o valor em centavos.
/// </summary>
public static class Febraban
{
    /// <summary>Linha digitável de cobrança (com ou sem pontos/espaços).</summary>
    public static readonly Regex LinhaDigitavelRegex = new(
        @"\d{5}[.\s]?\d{5}[.\s]*\d{5}[.\s]?\d{6}[.\s]*\d{5}[.\s]?\d{6}[.\s]*\d[.\s]*(\d{14})");

    private static readonly DateTime BaseFatorAntiga = new(1997, 10, 7);
    private static readonly DateTime InicioNovoCiclo = new(2025, 2, 22);

    /// <summary>
    /// Procura uma linha digitável no texto. Devolve <c>false</c> se não achar;
    /// valor e vencimento podem vir zerados/nulos quando o boleto não os traz.
    /// </summary>
    public static bool TentarLer(string texto, out string linhaDigitavel, out decimal valor, out DateTime? vencimento)
    {
        linhaDigitavel = "";
        valor = 0m;
        vencimento = null;

        var casamento = LinhaDigitavelRegex.Match(texto ?? "");
        if (!casamento.Success)
            return false;

        linhaDigitavel = SomenteDigitos(casamento.Value);

        var campo = casamento.Groups[1].Value;
        var fator = int.Parse(campo[..4]);
        var centavos = long.Parse(campo[4..]);

        if (centavos > 0)
            valor = centavos / 100m;

        if (fator >= 1000)
        {
            var data = BaseFatorAntiga.AddDays(fator);
            if (data < InicioNovoCiclo)
                data = InicioNovoCiclo.AddDays(fator - 1000);
            vencimento = data;
        }

        return true;
    }

    public static string SomenteDigitos(string texto) =>
        new((texto ?? "").Where(char.IsDigit).ToArray());
}
