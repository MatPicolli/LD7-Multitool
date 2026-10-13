using System.Globalization;
using PdfSharpCore.Drawing;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>Peças comuns aos PDFs de comissão (fechamento e tabela para impressão).</summary>
internal static class PdfComum
{
    public static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Cinza do cabeçalho da planilha.</summary>
    public static readonly XSolidBrush CinzaCabecalho = new(XColor.FromArgb(102, 102, 102));

    public static string Moeda(decimal valor) => valor.ToString("C2", CulturaBr);

    /// <summary>
    /// Caminho para gravar <paramref name="nome"/>.pdf em <paramref name="subpasta"/>
    /// (ao lado do executável). Se o arquivo anterior estiver aberto no leitor de
    /// PDF — e por isso não puder ser sobrescrito —, grava um ao lado com a hora.
    /// </summary>
    public static string Caminho(string subpasta, string nome)
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, subpasta);
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, nome + ".pdf");
        return EmUso(caminho) ? Path.Combine(pasta, $"{nome} ({DateTime.Now:HH-mm-ss}).pdf") : caminho;
    }

    /// <summary>Corta o texto com "…" para caber na largura (o PDF não corta sozinho).</summary>
    public static string Ajustar(XGraphics gfx, string texto, XFont fonte, double largura)
    {
        if (gfx.MeasureString(texto, fonte).Width <= largura)
            return texto;
        while (texto.Length > 1 && gfx.MeasureString(texto + "…", fonte).Width > largura)
            texto = texto[..^1];
        return texto.TrimEnd() + "…";
    }

    private static bool EmUso(string caminho)
    {
        if (!File.Exists(caminho))
            return false;
        try
        {
            using var _ = new FileStream(caminho, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}
