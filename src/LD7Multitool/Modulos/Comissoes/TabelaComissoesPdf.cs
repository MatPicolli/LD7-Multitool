using System.Globalization;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// A tabela de comissões para imprimir e preencher à mão, no layout da aba da
/// planilha: Nº, Nota fiscal, Nº b, Cliente, REF. Pedido, Valor NF, Vencimento,
/// Nº de parcelas, Valor da parcela, Comissão, Status da comissão e pag.
/// comissão, com grade pontilhada e, depois das vendas, linhas numeradas em
/// branco (a última página é sempre completada até o fim).
/// </summary>
public static class TabelaComissoesPdf
{
    private const double Margem = 24;
    private const double AlturaLinha = 14;
    private const double AlturaCabecalho = 16;

    private enum Alinhamento { Esquerda, Centro, Direita, Moeda }

    private sealed record Coluna(string Titulo, double Largura, Alinhamento Alinhamento, Func<Comissao, string> Valor);

    // Larguras somam 794 pt = A4 deitado (842) menos as margens; proporções da planilha.
    private static readonly Coluna[] Colunas =
    {
        new("Nº", 22, Alinhamento.Centro, _ => ""), // preenchido com a numeração da impressão
        new("Nota fiscal", 62, Alinhamento.Centro, c => Milhar(c.NotaFiscal)),
        new("Nº b", 38, Alinhamento.Centro, c => c.NumeroBoleto),
        new("Cliente", 138, Alinhamento.Esquerda, c => c.Cliente),
        new("REF. Pedido", 58, Alinhamento.Esquerda, c => Milhar(c.RefPedido)),
        new("Valor NF", 70, Alinhamento.Moeda, c => Numero(c.ValorNf)),
        new("Vencimento", 68, Alinhamento.Centro, c => c.Vencimento.ToString("dd/MM/yyyy", PdfComum.CulturaBr)),
        new("Nº de parcelas", 64, Alinhamento.Centro, c => c.Parcela),
        new("Valor da parcela", 70, Alinhamento.Moeda, c => Numero(c.ValorParcela)),
        new("Comissão", 64, Alinhamento.Moeda, c => Numero(c.ValorComissao)),
        new("Status da comissão", 74, Alinhamento.Centro, c => c.StatusDescricao),
        new("pag. comissão", 66, Alinhamento.Centro,
            c => c.DataPagamento?.ToString("dd/MM/yyyy", PdfComum.CulturaBr) ?? ""),
    };

    /// <summary>
    /// Gera "Tabela de comissões.pdf" em Comissoes_Impressao (ao lado do
    /// executável) e devolve o caminho. As parcelas saem na ordem recebida,
    /// numeradas 1, 2, 3...; as linhas em branco continuam a numeração.
    /// </summary>
    public static string Gerar(IReadOnlyList<Comissao> parcelas, int linhasEmBranco, string titulo)
    {
        var caminho = PdfComum.Caminho("Comissoes_Impressao", "Tabela de comissões");

        using var documento = new PdfDocument();
        documento.Info.Title = titulo;

        var fTitulo = new XFont("Segoe UI", 9, XFontStyle.Bold);
        var fCabecalho = new XFont("Segoe UI", 7, XFontStyle.Bold);
        var fTexto = new XFont("Segoe UI", 7.5, XFontStyle.Regular);
        var fRodape = new XFont("Segoe UI", 6.5, XFontStyle.Italic);
        // Grade pontilhada como a da planilha; contorno da tabela contínuo.
        // (padrão em múltiplos da espessura: traço de 0,8 pt e espaço de 1,2 pt —
        // o "Dot" padrão é tão miúdo que impresso parece linha contínua).
        var pontilhado = new XPen(XColors.Black, 0.4) { DashPattern = new double[] { 2, 3 } };
        var contorno = new XPen(XColors.Black, 0.8);
        var largura = Colunas.Sum(c => c.Largura);

        PdfPage pagina = null!;
        XGraphics gfx = null!;
        double y = 0, topoTabela = 0;
        var numeroPagina = 0;

        double Fundo() => pagina.Height.Point - Margem;

        void FecharPagina()
        {
            if (gfx is null)
                return;
            // Linhas verticais pontilhadas (abaixo do cabeçalho) + contorno da tabela.
            var x = Margem;
            foreach (var coluna in Colunas.Take(Colunas.Length - 1))
            {
                x += coluna.Largura;
                gfx.DrawLine(pontilhado, x, topoTabela + AlturaCabecalho, x, y);
            }
            gfx.DrawRectangle(contorno, Margem, topoTabela, largura, y - topoTabela);
            gfx.Dispose();
        }

        void NovaPagina()
        {
            FecharPagina();
            pagina = documento.AddPage();
            pagina.Size = PdfSharpCore.PageSize.A4;
            pagina.Orientation = PdfSharpCore.PageOrientation.Landscape;
            gfx = XGraphics.FromPdfPage(pagina);
            numeroPagina++;
            y = Margem;

            if (numeroPagina == 1)
            {
                gfx.DrawString(titulo, fTitulo, XBrushes.Black, new XRect(Margem, y, largura, 12), XStringFormats.TopLeft);
                y += 16;
            }

            topoTabela = y;
            var x = Margem;
            foreach (var coluna in Colunas)
            {
                gfx.DrawRectangle(PdfComum.CinzaCabecalho, x, y, coluna.Largura, AlturaCabecalho);
                gfx.DrawString(PdfComum.Ajustar(gfx, coluna.Titulo, fCabecalho, coluna.Largura - 4), fCabecalho,
                    XBrushes.White, new XRect(x, y, coluna.Largura, AlturaCabecalho), XStringFormats.Center);
                x += coluna.Largura;
            }
            y += AlturaCabecalho;

            gfx.DrawString($"Página {numeroPagina}", fRodape, XBrushes.Gray,
                new XRect(Margem, Fundo() + 6, largura, 10), XStringFormats.TopRight);
        }

        void Linha(int numero, Comissao? comissao)
        {
            if (y + AlturaLinha > Fundo())
                NovaPagina();
            var x = Margem;
            foreach (var coluna in Colunas)
            {
                var texto = coluna.Titulo == "Nº"
                    ? numero.ToString(CultureInfo.InvariantCulture)
                    : comissao is null ? "" : coluna.Valor(comissao);
                Escrever(gfx, texto, fTexto, coluna, x, y);
                x += coluna.Largura;
            }
            y += AlturaLinha;
            gfx.DrawLine(pontilhado, Margem, y, Margem + largura, y);
        }

        NovaPagina();
        var numero = 0;
        foreach (var parcela in parcelas)
            Linha(++numero, parcela);

        // Linhas em branco pedidas e, depois, o que faltar para completar a página.
        for (var i = 0; i < linhasEmBranco; i++)
            Linha(++numero, null);
        while (linhasEmBranco > 0 && y + AlturaLinha <= Fundo())
            Linha(++numero, null);

        FecharPagina();
        documento.Save(caminho);
        return caminho;
    }

    private static void Escrever(XGraphics gfx, string texto, XFont fonte, Coluna coluna, double x, double y)
    {
        if (texto.Length == 0)
            return;
        var caixa = new XRect(x + 3, y, coluna.Largura - 6, AlturaLinha);
        switch (coluna.Alinhamento)
        {
            case Alinhamento.Moeda:
                // "R$" à esquerda e o número à direita, como o formato contábil do Excel.
                gfx.DrawString("R$", fonte, XBrushes.Black, caixa, XStringFormats.CenterLeft);
                gfx.DrawString(texto, fonte, XBrushes.Black, caixa, XStringFormats.CenterRight);
                break;
            case Alinhamento.Centro:
                gfx.DrawString(PdfComum.Ajustar(gfx, texto, fonte, caixa.Width), fonte, XBrushes.Black, caixa, XStringFormats.Center);
                break;
            case Alinhamento.Direita:
                gfx.DrawString(PdfComum.Ajustar(gfx, texto, fonte, caixa.Width), fonte, XBrushes.Black, caixa, XStringFormats.CenterRight);
                break;
            default:
                gfx.DrawString(PdfComum.Ajustar(gfx, texto, fonte, caixa.Width), fonte, XBrushes.Black, caixa, XStringFormats.CenterLeft);
                break;
        }
    }

    private static string Numero(decimal valor) => valor.ToString("N2", PdfComum.CulturaBr);

    /// <summary>"10195140" → "10.195.140", como o Excel exibe; texto ("SEM PEDIDO") fica igual.</summary>
    private static string Milhar(string texto) =>
        texto.Length > 3 && texto[0] != '0' && texto.All(char.IsDigit) && long.TryParse(texto, out var numero)
            ? numero.ToString("N0", PdfComum.CulturaBr)
            : texto;
}
