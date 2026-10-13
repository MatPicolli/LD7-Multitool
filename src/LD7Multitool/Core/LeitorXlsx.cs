using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace LD7Multitool.Core;

/// <summary>
/// Leitor mínimo de planilhas do Excel (.xlsx), sem pacote externo: o arquivo é
/// um zip de XMLs (Office Open XML). Lê só o <b>valor</b> de cada célula — para
/// fórmulas, o resultado que o Excel gravou junto — sem estilos. Por isso datas
/// chegam como número serial (use <see cref="CelulaXlsx.ComoData"/>).
/// Não lê o formato antigo .xls.
/// </summary>
public sealed class LeitorXlsx : IDisposable
{
    private readonly FileStream _arquivo;
    private readonly ZipArchive _zip;
    private readonly List<string> _textosCompartilhados;
    private readonly List<(string Nome, string Caminho)> _abas = new();

    /// <summary>Abre o arquivo (mesmo que esteja aberto no Excel).</summary>
    public LeitorXlsx(string caminho)
    {
        _arquivo = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        try
        {
            _zip = new ZipArchive(_arquivo, ZipArchiveMode.Read);
            var relacoes = LerRelacoes("xl/_rels/workbook.xml.rels");

            var pasta = CarregarXml("xl/workbook.xml")
                ?? throw new InvalidDataException("O arquivo não é uma planilha .xlsx válida.");
            foreach (var aba in pasta.Descendants().Where(e => e.Name.LocalName == "sheet"))
            {
                var nome = (string?)aba.Attribute("name") ?? "";
                // O id da relação fica no namespace de relacionamentos (r:id).
                var idRelacao = aba.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "id" && a.Name.NamespaceName.Contains("relationships"))
                    ?.Value;
                if (idRelacao is not null && relacoes.TryGetValue(idRelacao, out var alvo))
                    _abas.Add((nome, ResolverCaminho(alvo.Alvo)));
            }

            string? caminhoTextos = relacoes.Values
                .Where(r => r.Tipo.EndsWith("/sharedStrings"))
                .Select(r => r.Alvo)
                .FirstOrDefault();
            _textosCompartilhados = LerTextosCompartilhados(
                caminhoTextos is null ? "xl/sharedStrings.xml" : ResolverCaminho(caminhoTextos));
        }
        catch
        {
            _zip?.Dispose();
            _arquivo.Dispose();
            throw;
        }
    }

    /// <summary>Nomes das abas, na ordem em que aparecem no Excel.</summary>
    public IReadOnlyList<string> Abas => _abas.Select(a => a.Nome).ToList();

    /// <summary>Lê todas as células preenchidas de uma aba.</summary>
    public PlanilhaXlsx LerAba(string nome)
    {
        var caminho = _abas.First(a => a.Nome == nome).Caminho;
        var xml = CarregarXml(caminho)
            ?? throw new InvalidDataException($"A aba \"{nome}\" não foi encontrada dentro do arquivo.");

        var celulas = new Dictionary<(int, int), CelulaXlsx>();
        var linhaAtual = 0;
        foreach (var linha in xml.Descendants().Where(e => e.Name.LocalName == "row"))
        {
            // "r" (número da linha) e o endereço da célula são opcionais no
            // formato; quando faltam, valem a posição sequencial.
            linhaAtual = int.TryParse((string?)linha.Attribute("r"), out var r) ? r : linhaAtual + 1;
            var colunaAtual = 0;
            foreach (var c in linha.Elements().Where(e => e.Name.LocalName == "c"))
            {
                var endereco = (string?)c.Attribute("r");
                colunaAtual = endereco is null ? colunaAtual + 1 : ColunaDoEndereco(endereco);
                var celula = LerCelula(c);
                if (celula is not null)
                    celulas[(linhaAtual, colunaAtual)] = celula;
            }
        }
        return new PlanilhaXlsx(celulas);
    }

    public void Dispose()
    {
        _zip.Dispose();
        _arquivo.Dispose();
    }

    private CelulaXlsx? LerCelula(XElement c)
    {
        var tipo = (string?)c.Attribute("t") ?? "n";
        var valor = c.Elements().FirstOrDefault(e => e.Name.LocalName == "v")?.Value;

        switch (tipo)
        {
            case "s": // texto da tabela de textos compartilhados
                return int.TryParse(valor, out var indice) && indice >= 0 && indice < _textosCompartilhados.Count
                    ? new CelulaXlsx(_textosCompartilhados[indice], null)
                    : null;
            case "inlineStr":
                var embutido = c.Elements().FirstOrDefault(e => e.Name.LocalName == "is");
                return embutido is null ? null : new CelulaXlsx(JuntarTextos(embutido), null);
            case "str": // resultado texto de fórmula
            case "e":   // erro (#N/D, #VALOR!...)
                return valor is null ? null : new CelulaXlsx(valor, null);
            case "b":
                return valor is null ? null : new CelulaXlsx(valor == "1" ? "VERDADEIRO" : "FALSO", null);
            default: // número (datas também são números no Excel)
                if (valor is null)
                    return null;
                return double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero)
                    ? new CelulaXlsx(valor, numero)
                    : new CelulaXlsx(valor, null);
        }
    }

    /// <summary>"AB12" → 28 (A = 1).</summary>
    private static int ColunaDoEndereco(string endereco)
    {
        var coluna = 0;
        foreach (var caractere in endereco)
        {
            if (!char.IsLetter(caractere))
                break;
            coluna = coluna * 26 + (char.ToUpperInvariant(caractere) - 'A' + 1);
        }
        return coluna;
    }

    private List<string> LerTextosCompartilhados(string caminho)
    {
        var xml = CarregarXml(caminho);
        if (xml is null)
            return new List<string>();
        return xml.Root!.Elements().Where(e => e.Name.LocalName == "si").Select(JuntarTextos).ToList();
    }

    /// <summary>Concatena os trechos &lt;t&gt; (texto com formatação mista vem em vários), sem a fonética (rPh).</summary>
    private static string JuntarTextos(XElement elemento) => string.Concat(
        elemento.Descendants()
            .Where(e => e.Name.LocalName == "t" && e.Parent?.Name.LocalName != "rPh")
            .Select(e => e.Value));

    private Dictionary<string, (string Alvo, string Tipo)> LerRelacoes(string caminho)
    {
        var relacoes = new Dictionary<string, (string, string)>();
        var xml = CarregarXml(caminho);
        if (xml is null)
            return relacoes;
        foreach (var rel in xml.Descendants().Where(e => e.Name.LocalName == "Relationship"))
        {
            var id = (string?)rel.Attribute("Id");
            var alvo = (string?)rel.Attribute("Target");
            if (id is not null && alvo is not null)
                relacoes[id] = (alvo, (string?)rel.Attribute("Type") ?? "");
        }
        return relacoes;
    }

    /// <summary>Alvos são relativos a "xl/" (ex.: "worksheets/sheet1.xml") ou absolutos ("/xl/...").</summary>
    private static string ResolverCaminho(string alvo) =>
        alvo.StartsWith('/') ? alvo.TrimStart('/') : "xl/" + alvo;

    private XDocument? CarregarXml(string caminho)
    {
        var entrada = _zip.GetEntry(caminho)
            ?? _zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, caminho, StringComparison.OrdinalIgnoreCase));
        if (entrada is null)
            return null;
        using var fluxo = entrada.Open();
        return XDocument.Load(fluxo);
    }
}

/// <summary>Valor de uma célula: o texto como gravado e, se for número, o número.</summary>
public sealed record CelulaXlsx(string Texto, double? Numero)
{
    /// <summary>
    /// A célula como data: número serial do Excel (o normal para datas) ou texto
    /// no formato brasileiro ("22/11/2025"). Null se não for uma data.
    /// </summary>
    public DateTime? ComoData()
    {
        if (Numero is { } serial)
            return serial is >= 1 and < 2958466 ? DateTime.FromOADate(serial).Date : null;
        return DateTime.TryParseExact(Texto.Trim(),
            new[] { "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy", "yyyy-MM-dd" },
            CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var data)
            ? data.Date
            : null;
    }

    /// <summary>
    /// A célula como valor em reais: número, ou texto como "R$ 1.234,56" / "1234,56".
    /// Null se não for um valor.
    /// </summary>
    public decimal? ComoDecimal()
    {
        if (Numero is { } numero)
            return Math.Round(Convert.ToDecimal(numero), 2);
        var texto = Texto.Replace("R$", "").Replace(" ", "").Replace(" ", "").Trim();
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var valor)
            ? valor
            : null;
    }

    /// <summary>A célula como texto de exibição: "10195140" e não "10195140.0" para inteiros.</summary>
    public string ComoTexto()
    {
        if (Numero is { } numero && Math.Abs(numero) < 1e15 && numero == Math.Floor(numero))
            return ((long)numero).ToString(CultureInfo.InvariantCulture);
        return Texto.Trim();
    }
}

/// <summary>As células preenchidas de uma aba, endereçadas por (linha, coluna), começando em 1.</summary>
public sealed class PlanilhaXlsx
{
    private readonly Dictionary<(int Linha, int Coluna), CelulaXlsx> _celulas;

    public PlanilhaXlsx(Dictionary<(int Linha, int Coluna), CelulaXlsx> celulas)
    {
        _celulas = celulas;
        UltimaLinha = celulas.Count == 0 ? 0 : celulas.Keys.Max(k => k.Linha);
        UltimaColuna = celulas.Count == 0 ? 0 : celulas.Keys.Max(k => k.Coluna);
    }

    public int UltimaLinha { get; }
    public int UltimaColuna { get; }

    public CelulaXlsx? Celula(int linha, int coluna) =>
        _celulas.TryGetValue((linha, coluna), out var celula) ? celula : null;
}
