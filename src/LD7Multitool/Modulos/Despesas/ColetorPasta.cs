using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Coleta pelos PDFs que o usuário baixou do portal.
///
/// É o caminho mais confiável para os portais que exigem login com captcha,
/// token ou app: o usuário baixa a segunda via como sempre fez, na pasta de
/// downloads, e o programa lê valor e vencimento do PDF sozinho (pela linha
/// digitável, que segue o padrão FEBRABAN e vale para qualquer banco).
///
/// Cada item diz quais arquivos são dele pela máscara em
/// <see cref="Despesa.PadraoArquivo"/> (ex.: <c>celesc*haras*.pdf</c>).
/// </summary>
public class ColetorPasta : IColetorDespesa
{
    public MetodoColeta Metodo => MetodoColeta.Pasta;

    public string? MotivoIndisponivel(Despesa despesa)
    {
        var pasta = DespesasConfigForm.PastaDownloads;
        if (pasta.Length == 0)
            return "Configure a pasta de downloads no botão ⚙ do módulo.";
        if (!Directory.Exists(pasta))
            return $"A pasta de downloads não existe: {pasta}";
        if (despesa.PadraoArquivo.Trim().Length == 0)
            return "Preencha \"Máscara do arquivo\" no item (ex.: celesc*haras*.pdf).";
        return null;
    }

    public Task<IReadOnlyList<LancamentoDespesa>> ColetarAsync(
        Despesa despesa, CancellationToken cancelamento)
    {
        var pasta = DespesasConfigForm.PastaDownloads;
        var mascara = despesa.PadraoArquivo.Trim();
        if (!mascara.Contains('.'))
            mascara += ".pdf";

        // Só arquivos recentes: evita reprocessar anos de downloads a cada coleta.
        var limite = DateTime.Now.AddDays(-DespesasConfigForm.DiasBusca);
        var arquivos = Directory
            .EnumerateFiles(pasta, mascara, SearchOption.TopDirectoryOnly)
            .Where(a => File.GetLastWriteTime(a) >= limite)
            .OrderBy(File.GetLastWriteTime)
            .ToList();

        var lancamentos = new List<LancamentoDespesa>();
        foreach (var arquivo in arquivos)
        {
            cancelamento.ThrowIfCancellationRequested();
            lancamentos.AddRange(LerArquivo(arquivo));
        }

        return Task.FromResult<IReadOnlyList<LancamentoDespesa>>(lancamentos);
    }

    /// <summary>Um lançamento por página com linha digitável (um PDF pode trazer várias parcelas).</summary>
    private static IEnumerable<LancamentoDespesa> LerArquivo(string arquivo)
    {
        // PDF protegido ou corrompido devolve lista vazia e cai no "pendente" abaixo.
        var paginas = LerPaginas(arquivo);

        var achou = false;
        foreach (var pagina in paginas)
        {
            if (!Febraban.TentarLer(pagina, out var linha, out var valor, out var vencimento))
                continue;

            achou = true;
            yield return new LancamentoDespesa
            {
                Vencimento = vencimento ?? File.GetLastWriteTime(arquivo).Date,
                Valor = valor,
                LinhaDigitavel = linha,
                CaminhoArquivo = arquivo,
                Origem = OrigemLancamento.Pasta,
                ChaveOrigem = "linha:" + linha,
            };
        }

        // Arquivo do item, mas sem linha digitável (fatura só informativa,
        // PDF escaneado): vale registrar mesmo assim, zerado, para o usuário
        // completar — é melhor do que sumir com a conta.
        if (!achou)
            yield return Pendente(arquivo);
    }

    private static List<string> LerPaginas(string arquivo)
    {
        try
        {
            using var documento = PdfDocument.Open(arquivo);
            return documento.GetPages().Select(ContentOrderTextExtractor.GetText).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static LancamentoDespesa Pendente(string arquivo) => new()
    {
        Vencimento = File.GetLastWriteTime(arquivo).Date,
        CaminhoArquivo = arquivo,
        Origem = OrigemLancamento.Pasta,
        ChaveOrigem = "arquivo:" + Path.GetFileName(arquivo).ToLowerInvariant(),
    };
}
