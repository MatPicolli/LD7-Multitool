namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Contrato de uma estratégia de busca automática da conta do mês.
///
/// Cada coletor sabe procurar a conta em <b>uma</b> origem (pasta de downloads,
/// caixa de e-mail, portal HTTP) e devolve os lançamentos que encontrou — sem
/// gravar nada. Quem grava, filtra duplicados e trata erro é o
/// <see cref="ServicoColeta"/>.
///
/// Para criar um coletor novo, implemente esta interface e registre-o em
/// <see cref="ServicoColeta"/>.
/// </summary>
public interface IColetorDespesa
{
    /// <summary>Método que este coletor atende (o item escolhe um em "Coleta automática").</summary>
    MetodoColeta Metodo { get; }

    /// <summary>
    /// Explica por que o coletor não consegue rodar para esta despesa
    /// (ex.: falta configurar a pasta). <c>null</c> quando está tudo pronto.
    /// </summary>
    string? MotivoIndisponivel(Despesa despesa);

    /// <summary>
    /// Busca as contas em aberto da despesa. Só precisa preencher
    /// <see cref="LancamentoDespesa.ChaveOrigem"/>, vencimento, valor e o que
    /// mais conseguir; o resto o serviço completa.
    /// </summary>
    Task<IReadOnlyList<LancamentoDespesa>> ColetarAsync(Despesa despesa, CancellationToken cancelamento);
}

/// <summary>Como terminou a coleta de um item (uma linha no relatório da tela de coleta).</summary>
public sealed record ResultadoColeta(
    string Despesa,
    string Metodo,
    int Novos,
    int Repetidos,
    string Detalhe,
    bool Erro)
{
    public static ResultadoColeta Falha(Despesa despesa, string motivo) =>
        new(despesa.Nome, despesa.Metodo.Descricao(), 0, 0, motivo, true);

    public static ResultadoColeta Ignorado(Despesa despesa, string motivo) =>
        new(despesa.Nome, despesa.Metodo.Descricao(), 0, 0, motivo, false);
}
