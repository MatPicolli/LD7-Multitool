using System.Net.Http;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Roda a coleta automática dos itens: escolhe o coletor de cada despesa,
/// descarta o que já está cadastrado e grava o que é novo.
///
/// Para acrescentar uma forma de coleta, implemente <see cref="IColetorDespesa"/>
/// e inclua a classe em <see cref="Coletores"/>.
/// </summary>
public static class ServicoColeta
{
    private static readonly IColetorDespesa[] Coletores =
    {
        new ColetorPasta(),
        new ColetorEmail(),
        new ColetorHttp(),
    };

    /// <summary>Itens que têm coleta automática ligada.</summary>
    public static List<Despesa> Automatizaveis(IEnumerable<Despesa> despesas) =>
        despesas.Where(d => d.Ativo && d.Metodo != MetodoColeta.Nenhum).ToList();

    /// <summary>
    /// Coleta as despesas informadas, uma a uma, relatando o progresso.
    /// Nunca estoura: erro de rede/login vira uma linha de erro no relatório.
    /// </summary>
    public static async Task<List<ResultadoColeta>> ColetarAsync(
        IEnumerable<Despesa> despesas,
        IProgress<string>? progresso,
        CancellationToken cancelamento)
    {
        var resultados = new List<ResultadoColeta>();

        foreach (var despesa in despesas)
        {
            cancelamento.ThrowIfCancellationRequested();
            progresso?.Report($"Buscando: {despesa.Nome}...");

            var coletor = Coletores.FirstOrDefault(c => c.Metodo == despesa.Metodo);
            if (coletor is null)
            {
                resultados.Add(ResultadoColeta.Ignorado(despesa, "Sem coleta automática configurada."));
                continue;
            }

            if (coletor.MotivoIndisponivel(despesa) is { } motivo)
            {
                resultados.Add(ResultadoColeta.Ignorado(despesa, motivo));
                continue;
            }

            try
            {
                var encontrados = await coletor.ColetarAsync(despesa, cancelamento);
                resultados.Add(Gravar(despesa, encontrados));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                resultados.Add(ResultadoColeta.Falha(despesa, MensagemDeErro(ex)));
            }
        }

        return resultados;
    }

    /// <summary>Grava os lançamentos novos e conta quantos já existiam.</summary>
    private static ResultadoColeta Gravar(Despesa despesa, IReadOnlyList<LancamentoDespesa> encontrados)
    {
        if (encontrados.Count == 0)
            return ResultadoColeta.Ignorado(despesa, "Nada novo encontrado.");

        var jaCadastradas = LancamentoDespesaRepository.ChavesDe(despesa.Id);
        int novos = 0, repetidos = 0;

        foreach (var lancamento in encontrados)
        {
            if (lancamento.ChaveOrigem.Length > 0 && !jaCadastradas.Add(lancamento.ChaveOrigem))
            {
                repetidos++;
                continue;
            }

            lancamento.DespesaId = despesa.Id;
            lancamento.ColetadoEm = DateTime.Now;
            if (lancamento.Competencia.Length == 0)
                lancamento.Competencia = CompetenciaDe(lancamento.Vencimento);

            if (LancamentoDespesaRepository.Inserir(lancamento))
                novos++;
            else
                repetidos++;
        }

        var detalhe = novos > 0
            ? $"{novos} conta(s) nova(s)."
            : "Já estava tudo cadastrado.";
        return new ResultadoColeta(despesa.Nome, despesa.Metodo.Descricao(), novos, repetidos, detalhe, false);
    }

    /// <summary>
    /// Competência presumida a partir do vencimento: contas costumam vencer no
    /// mês seguinte ao de referência, então até o dia 10 assumimos o mês anterior.
    /// O usuário pode corrigir no lançamento.
    /// </summary>
    public static string CompetenciaDe(DateTime vencimento) =>
        (vencimento.Day <= 10 ? vencimento.AddMonths(-1) : vencimento).ToString("yyyy-MM");

    private static string MensagemDeErro(Exception ex) => ex switch
    {
        HttpRequestException => "Falha de conexão com o portal: " + ex.Message,
        TaskCanceledException => "O portal demorou demais para responder.",
        _ => ex.Message,
    };
}
