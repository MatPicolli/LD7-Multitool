using System.Globalization;
using System.Xml.Linq;

namespace LD7Multitool.Modulos.ConsultaFiscal;

public sealed record Participante(string Cnpj, string Ie, string Nome, string Municipio, string Uf);

public sealed record ProdutoNota(string Descricao, string Quantidade, string Unidade, string ValorUnit, string ValorTotal);

/// <summary>
/// Dados completos de uma NF-e (emitente, destinatário, produtos, valores e
/// protocolo), extraídos do XML "nfeProc" retornado pela Distribuição DFe.
/// </summary>
public sealed class NotaFiscalDetalhada
{
    public string Chave { get; init; } = "";
    public string Numero { get; init; } = "";
    public string Serie { get; init; } = "";
    public string Modelo { get; init; } = "";
    public string NaturezaOperacao { get; init; } = "";
    public string TipoOperacao { get; init; } = "";
    public string DataEmissao { get; init; } = "";

    public Participante? Emitente { get; init; }
    public Participante? Destinatario { get; init; }
    public List<ProdutoNota> Produtos { get; init; } = new();
    public string ValorTotal { get; init; } = "";

    public int SituacaoCStat { get; init; }
    public string SituacaoMotivo { get; init; } = "";
    public string Protocolo { get; init; } = "";
    public string DataAutorizacao { get; init; } = "";

    /// <summary>Monta a partir do XML "nfeProc" (NFe + protNFe). Retorna null se não for uma NF-e.</summary>
    public static NotaFiscalDetalhada? DeProcNFe(string xml)
    {
        XElement raiz;
        try
        {
            raiz = XDocument.Parse(xml).Root!;
        }
        catch
        {
            return null;
        }

        var infNFe = Descendente(raiz, "infNFe");
        if (infNFe is null)
            return null;

        var ide = Descendente(infNFe, "ide");
        var emit = Descendente(infNFe, "emit");
        var dest = Descendente(infNFe, "dest");
        var total = Descendente(infNFe, "ICMSTot");
        var infProt = Descendente(raiz, "infProt");

        var chave = (infNFe.Attribute("Id")?.Value ?? "").Replace("NFe", "");

        return new NotaFiscalDetalhada
        {
            Chave = chave,
            Numero = Valor(ide, "nNF"),
            Serie = Valor(ide, "serie"),
            Modelo = Valor(ide, "mod"),
            NaturezaOperacao = Valor(ide, "natOp"),
            TipoOperacao = Valor(ide, "tpNF") == "0" ? "0 - Entrada" : "1 - Saída",
            DataEmissao = FormatarData(Valor(ide, "dhEmi")),
            Emitente = LerParticipante(emit),
            Destinatario = LerParticipante(dest),
            Produtos = LerProdutos(infNFe),
            ValorTotal = FormatarValor(Valor(total, "vNF")),
            SituacaoCStat = int.TryParse(Valor(infProt, "cStat"), out var c) ? c : 0,
            SituacaoMotivo = Valor(infProt, "xMotivo"),
            Protocolo = Valor(infProt, "nProt"),
            DataAutorizacao = FormatarData(Valor(infProt, "dhRecbto")),
        };
    }

    private static Participante? LerParticipante(XElement? no)
    {
        if (no is null)
            return null;
        var ender = Descendente(no, no.Name.LocalName == "emit" ? "enderEmit" : "enderDest");
        var doc = Valor(no, "CNPJ");
        if (doc.Length == 0)
            doc = Valor(no, "CPF");
        return new Participante(
            Cnpj: FormatarCnpj(doc),
            Ie: Valor(no, "IE"),
            Nome: Valor(no, "xNome"),
            Municipio: Valor(ender, "xMun"),
            Uf: Valor(ender, "UF"));
    }

    private static List<ProdutoNota> LerProdutos(XElement infNFe)
    {
        var lista = new List<ProdutoNota>();
        foreach (var det in infNFe.Elements().Where(e => e.Name.LocalName == "det"))
        {
            var prod = Descendente(det, "prod");
            if (prod is null)
                continue;
            lista.Add(new ProdutoNota(
                Descricao: Valor(prod, "xProd"),
                Quantidade: FormatarValor(Valor(prod, "qCom"), 4),
                Unidade: Valor(prod, "uCom"),
                ValorUnit: FormatarValor(Valor(prod, "vUnCom"), 4),
                ValorTotal: FormatarValor(Valor(prod, "vProd"))));
        }
        return lista;
    }

    private static XElement? Descendente(XElement? pai, string nomeLocal) =>
        pai?.Descendants().FirstOrDefault(e => e.Name.LocalName == nomeLocal);

    private static string Valor(XElement? pai, string nomeLocal) =>
        pai?.Descendants().FirstOrDefault(e => e.Name.LocalName == nomeLocal)?.Value ?? "";

    private static string FormatarData(string iso)
    {
        if (DateTimeOffset.TryParse(iso, out var d))
            return d.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss");
        return iso;
    }

    private static string FormatarValor(string texto, int casas = 2)
    {
        if (decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v.ToString("N" + casas, CultureInfo.GetCultureInfo("pt-BR"));
        return texto;
    }

    private static string FormatarCnpj(string doc)
    {
        var d = new string(doc.Where(char.IsDigit).ToArray());
        return d.Length == 14
            ? $"{d[..2]}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}"
            : doc;
    }
}
