using System.Globalization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Gera o PDF do fechamento de comissão (para entregar ao vendedor): a mesma
/// tabela da aba da planilha — Nota fiscal, Nº b, Cliente, REF. Pedido, Valor NF,
/// Vencimento, Nº de parcelas, Valor da parcela, Comissão — e a SOMA no fim.
/// </summary>
public static class FechamentoComissaoPdf
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private const double Margem = 36;
    private const double AlturaLinha = 18;
    private const double AlturaCabecalho = 22;

    private sealed record Coluna(string Titulo, double Largura, bool Direita, Func<Comissao, string> Valor);

    // Larguras somam 770 pt = A4 deitado (842) menos as margens.
    private static readonly Coluna[] Colunas =
    {
        new("Nota fiscal", 66, false, c => c.NotaFiscal),
        new("Nº b", 56, false, c => c.NumeroBoleto),
        new("Cliente", 206, false, c => c.Cliente),
        new("REF. Pedido", 72, false, c => c.RefPedido),
        new("Valor NF", 80, true, c => Moeda(c.ValorNf)),
        new("Vencimento", 68, false, c => c.Vencimento.ToString("dd/MM/yyyy", CulturaBr)),
        new("Nº de parcelas", 70, false, c => c.Parcela),
        new("Valor da parcela", 76, true, c => Moeda(c.ValorParcela)),
        new("Comissão", 76, true, c => Moeda(c.ValorComissao)),
    };

    /// <summary>Gera em Fechamentos_Comissao (ao lado do executável) e devolve o caminho.</summary>
    public static string Gerar(IReadOnlyList<Comissao> parcelas, DateTime de, DateTime ate)
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "Fechamentos_Comissao");
        Directory.CreateDirectory(pasta);
        var nome = $"Comissão {de:yyyy-MM-dd} a {ate:yyyy-MM-dd}";
        var caminho = Path.Combine(pasta, nome + ".pdf");
        // Um PDF anterior aberto no leitor não pode ser sobrescrito: grava ao lado.
        if (EmUso(caminho))
            caminho = Path.Combine(pasta, $"{nome} ({DateTime.Now:HH-mm-ss}).pdf");

        using var documento = new PdfDocument();
        documento.Info.Title = $"Comissão MILITARI — {de:dd/MM/yyyy} a {ate:dd/MM/yyyy}";

        var fTitulo = new XFont("Segoe UI", 15, XFontStyle.Bold);
        var fSubtitulo = new XFont("Segoe UI", 9.5, XFontStyle.Regular);
        var fCabecalho = new XFont("Segoe UI", 8.5, XFontStyle.Bold);
        var fTexto = new XFont("Segoe UI", 8.5, XFontStyle.Regular);
        var fSoma = new XFont("Segoe UI", 10.5, XFontStyle.Bold);
        var fRodape = new XFont("Segoe UI", 7.5, XFontStyle.Italic);
        var cinzaCabecalho = new XSolidBrush(XColor.FromArgb(102, 102, 102)); // igual ao cabeçalho da planilha
        var listra = new XSolidBrush(XColor.FromArgb(246, 247, 250));
        var linhaFina = new XPen(XColor.FromArgb(215, 218, 228), 0.5);

        PdfPage pagina = null!;
        XGraphics gfx = null!;
        double y = 0;
        var numeroPagina = 0;

        void NovaPagina()
        {
            gfx?.Dispose();
            pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.A4;
            pagina.Orientation = PdfSharpCore.PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(pagina);
            numeroPagina++;
            y = Margem;

            if (numeroPagina == 1)
            {
                gfx.DrawString("Comissão MILITARI — fechamento", fTitulo, XBrushes.Black,
                    new XRect(Margem, y, 600, 22), XStringFormats.TopLeft);
                y += 24;
                gfx.DrawString(
                    $"Vencimentos de {de:dd/MM/yyyy} a {ate:dd/MM/yyyy}   •   {parcelas.Count} parcela(s)   •   " +
                    $"gerado em {DateTime.Now:dd/MM/yyyy HH:mm}",
                    fSubtitulo, XBrushes.Gray, new XRect(Margem, y, 700, 14), XStringFormats.TopLeft);
                y += 24;
            }

            // Cabeçalho da tabela (repetido em cada página).
            var x = Margem;
            foreach (var coluna in Colunas)
            {
                gfx.DrawRectangle(cinzaCabecalho, x, y, coluna.Largura, AlturaCabecalho);
                gfx.DrawString(coluna.Titulo, fCabecalho, XBrushes.White,
                    new XRect(x + 4, y, coluna.Largura - 8, AlturaCabecalho),
                    coluna.Direita ? XStringFormats.CenterRight : XStringFormats.CenterLeft);
                x += coluna.Largura;
            }
            y += AlturaCabecalho;

            gfx.DrawString($"Página {numeroPagina}", fRodape, XBrushes.Gray,
                new XRect(Margem, pagina.Height.Point - Margem + 8, pagina.Width.Point - 2 * Margem, 12),
                XStringFormats.TopRight);
        }

        NovaPagina();
        var largura = Colunas.Sum(c => c.Largura);
        for (var i = 0; i < parcelas.Count; i++)
        {
            if (y + AlturaLinha > pagina.Height.Point - Margem)
                NovaPagina();

            if (i % 2 == 1)
                gfx.DrawRectangle(listra, Margem, y, largura, AlturaLinha);
            var x = Margem;
            foreach (var coluna in Colunas)
            {
                var texto = Ajustar(gfx, coluna.Valor(parcelas[i]), fTexto, coluna.Largura - 8);
                gfx.DrawString(texto, fTexto, XBrushes.Black,
                    new XRect(x + 4, y, coluna.Largura - 8, AlturaLinha),
                    coluna.Direita ? XStringFormats.CenterRight : XStringFormats.CenterLeft);
                x += coluna.Largura;
            }
            y += AlturaLinha;
            gfx.DrawLine(linhaFina, Margem, y, Margem + largura, y);
        }

        // SOMA, como no rodapé da tabela da planilha.
        if (y + 34 > pagina.Height.Point - Margem)
            NovaPagina();
        y += 8;
        var larguraSoma = Colunas[^2].Largura + Colunas[^1].Largura;
        var xSoma = Margem + largura - larguraSoma;
        gfx.DrawRectangle(new XPen(XColors.Black, 1.2), xSoma, y, larguraSoma, 24);
        gfx.DrawString("SOMA:", fSoma, XBrushes.Black, new XRect(xSoma + 6, y, 60, 24), XStringFormats.CenterLeft);
        gfx.DrawString(Moeda(parcelas.Sum(c => c.ValorComissao)), fSoma, XBrushes.Black,
            new XRect(xSoma, y, larguraSoma - 6, 24), XStringFormats.CenterRight);

        gfx.Dispose();
        documento.Save(caminho);
        return caminho;
    }

    private static string Moeda(decimal valor) => valor.ToString("C2", CulturaBr);

    /// <summary>Corta o texto com "…" para caber na coluna (o PDF não corta sozinho).</summary>
    private static string Ajustar(XGraphics gfx, string texto, XFont fonte, double largura)
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
