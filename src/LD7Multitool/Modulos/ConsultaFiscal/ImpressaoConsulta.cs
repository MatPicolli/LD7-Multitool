using System.Drawing.Printing;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>Imprime um comprovante compacto da consulta (para o verso da nota).</summary>
public static class ImpressaoConsulta
{
    public static void Imprimir(ResultadoConsulta resultado, string impressora)
    {
        using var doc = new PrintDocument();
        doc.DocumentName = $"Consulta {resultado.TipoDocumento}";
        if (!string.IsNullOrWhiteSpace(impressora))
            doc.PrinterSettings.PrinterName = impressora;

        doc.PrintPage += (_, e) => Desenhar(e, resultado);
        doc.Print();
    }

    private static void Desenhar(PrintPageEventArgs e, ResultadoConsulta r)
    {
        var g = e.Graphics!;
        using var fonteTitulo = new Font("Segoe UI", 11, FontStyle.Bold);
        using var fonteRotulo = new Font("Segoe UI", 9, FontStyle.Bold);
        using var fonte = new Font("Segoe UI", 9);
        using var fonteChave = new Font("Consolas", 10, FontStyle.Bold);

        float x = e.MarginBounds.Left;
        float y = e.MarginBounds.Top;

        void Linha(string texto, Font f, float espacoDepois = 4)
        {
            g.DrawString(texto, f, Brushes.Black, x, y);
            y += f.GetHeight(g) + espacoDepois;
        }

        Linha($"CONSULTA DE {r.TipoDocumento.ToUpperInvariant()} — SEFAZ", fonteTitulo, 8);

        Linha("Chave de acesso:", fonteRotulo, 1);
        Linha(r.ChaveFormatada, fonteChave, 8);

        Linha("Situação:", fonteRotulo, 1);
        Linha($"{r.XMotivo} ({r.CStat})", fonte, 6);

        if (!string.IsNullOrWhiteSpace(r.Protocolo))
            Linha($"Protocolo: {r.Protocolo}", fonte, 2);
        if (!string.IsNullOrWhiteSpace(r.DataHora))
            Linha($"Data/hora: {r.DataHora}", fonte, 2);
        Linha($"Ambiente: {r.Ambiente}", fonte, 8);

        Linha($"Consultado em {DateTime.Now:dd/MM/yyyy HH:mm:ss} via LD7 Multitool", fonte);
    }
}
