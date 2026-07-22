using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Lê o número da NF-e (DANFE) de um PDF e indexa uma pasta de notas fiscais
/// para permitir vincular um boleto à sua NF-e pelo número.
///
/// O número aparece no topo do DANFE como "Nº 000.009.028"; comparamos sempre
/// pela forma normalizada (só dígitos, sem zeros à esquerda), para casar
/// "000.009.028", "9028" e "9.028".
/// </summary>
public static class LeitorNfePdf
{
    // "Nº", "N°", "N.º", "No" seguido do número com pontos de milhar opcionais.
    private static readonly Regex NumeroRegex = new(
        @"N[º°o\.\s]{1,3}\s*(\d{1,3}(?:\.\d{3})+|\d{3,12})",
        RegexOptions.IgnoreCase);

    /// <summary>Número da NF-e normalizado (só dígitos, sem zeros à esquerda), ou null.</summary>
    public static string? ExtrairNumero(string caminho)
    {
        string texto;
        try
        {
            using var documento = PdfDocument.Open(caminho);
            // O número fica no topo; a primeira página basta.
            var primeira = documento.GetPages().FirstOrDefault();
            if (primeira is null)
                return null;
            texto = ContentOrderTextExtractor.GetText(primeira);
        }
        catch
        {
            return null;
        }

        var m = NumeroRegex.Match(texto);
        return m.Success ? Normalizar(m.Groups[1].Value) : null;
    }

    /// <summary>Só dígitos, sem zeros à esquerda (ex.: "000.009.028" → "9028").</summary>
    public static string Normalizar(string numero)
    {
        var digitos = new string(numero.Where(char.IsDigit).ToArray()).TrimStart('0');
        return digitos.Length == 0 ? "" : digitos;
    }

    /// <summary>
    /// Percorre a pasta de NF-e uma vez e monta um índice número→caminho.
    /// Em caso de números repetidos, mantém o PDF mais recente.
    /// </summary>
    public static Dictionary<string, string> IndexarPasta(string pastaNfe)
    {
        var indice = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(pastaNfe) || !Directory.Exists(pastaNfe))
            return indice;

        // Mais recentes primeiro: assim, ao encontrar um número repetido, o
        // primeiro (mais novo) prevalece.
        var arquivos = Directory.EnumerateFiles(pastaNfe, "*.pdf", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetCreationTime);

        foreach (var arquivo in arquivos)
        {
            var numero = ExtrairNumero(arquivo);
            if (numero is { Length: > 0 } && !indice.ContainsKey(numero))
                indice[numero] = arquivo;
        }
        return indice;
    }
}
