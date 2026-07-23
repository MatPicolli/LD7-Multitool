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

    /// <summary>Impressão detalhada (emitente, destinatário, produtos), estilo portal.</summary>
    public static void ImprimirNota(NotaFiscalDetalhada nota, ChaveAcesso chave, string impressora)
    {
        using var doc = new PrintDocument();
        doc.DocumentName = $"NF-e {nota.Numero}";
        if (!string.IsNullOrWhiteSpace(impressora))
            doc.PrinterSettings.PrinterName = impressora;

        doc.PrintPage += (_, e) => DesenharNota(e, nota, chave);
        doc.Print();
    }

    private static void DesenharNota(PrintPageEventArgs e, NotaFiscalDetalhada n, ChaveAcesso chave)
    {
        var g = e.Graphics!;
        using var fTitulo = new Font("Segoe UI", 11, FontStyle.Bold);
        using var fSecao = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var f = new Font("Segoe UI", 8.5f);
        using var fChave = new Font("Consolas", 9, FontStyle.Bold);

        float x = e.MarginBounds.Left;
        float y = e.MarginBounds.Top;
        float larg = e.MarginBounds.Width;

        void Linha(string texto, Font fonte, float depois = 3)
        {
            g.DrawString(texto, fonte, Brushes.Black, x, y);
            y += fonte.GetHeight(g) + depois;
        }
        void Separador()
        {
            y += 2;
            g.DrawLine(Pens.Black, x, y, x + larg, y);
            y += 5;
        }

        Linha($"NF-e nº {n.Numero}   Série {n.Serie}   Modelo {n.Modelo}", fTitulo, 4);
        Linha("Chave de acesso:", f, 0);
        Linha(chave.Formatada, fChave, 4);
        Linha($"Natureza: {n.NaturezaOperacao}", f);
        Linha($"Emissão: {n.DataEmissao}    {n.TipoOperacao}", f);
        Separador();

        Linha("EMITENTE", fSecao);
        DesenharParticipante(g, n.Emitente, f, x, ref y);
        y += 4;
        Linha("DESTINATÁRIO", fSecao);
        DesenharParticipante(g, n.Destinatario, f, x, ref y);
        Separador();

        Linha("PRODUTOS", fSecao);
        foreach (var p in n.Produtos)
            Linha($"• {p.Descricao}  —  {p.Quantidade} {p.Unidade} x {p.ValorUnit} = {p.ValorTotal}", f, 2);
        y += 2;
        Linha($"VALOR TOTAL DA NOTA: R$ {n.ValorTotal}", fSecao, 4);
        Separador();

        Linha("SITUAÇÃO", fSecao);
        Linha($"{n.SituacaoMotivo} ({n.SituacaoCStat})", f);
        if (n.Protocolo.Length > 0)
            Linha($"Protocolo de autorização: {n.Protocolo}", f, 2);
        if (n.DataAutorizacao.Length > 0)
            Linha($"Data da autorização: {n.DataAutorizacao}", f, 2);
        y += 4;
        Linha($"Consultado em {DateTime.Now:dd/MM/yyyy HH:mm:ss} via LD7 Multitool", f);
    }

    private static void DesenharParticipante(Graphics g, Participante? p, Font f, float x, ref float y)
    {
        if (p is null)
        {
            g.DrawString("(não informado)", f, Brushes.Black, x, y);
            y += f.GetHeight(g) + 3;
            return;
        }
        g.DrawString($"{p.Nome}", f, Brushes.Black, x, y);
        y += f.GetHeight(g) + 2;
        g.DrawString($"CNPJ/CPF: {p.Cnpj}   IE: {p.Ie}   {p.Municipio}/{p.Uf}", f, Brushes.Black, x, y);
        y += f.GetHeight(g) + 3;
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
