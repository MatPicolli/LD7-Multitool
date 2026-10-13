namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Regras do fechamento mensal da comissão — o que a tabela auxiliar à direita
/// da aba da planilha fazia à mão. Pelos dados da planilha, o ciclo é:
/// <list type="number">
/// <item>quando o cliente paga a parcela, o status vira "Pago" (ainda sem data);</item>
/// <item>no fechamento, somam-se as "Pago" sem data em "pag. comissão" do período,
///   paga-se o vendedor e preenche-se a data.</item>
/// </list>
/// As "Não pago" do período (cliente ainda não pagou) aparecem, mas desmarcadas.
/// </summary>
public static class FechamentoComissoes
{
    /// <summary>
    /// Período sugerido: do dia seguinte ao último vencimento já acertado (com
    /// data de pagamento) até hoje — ou seja, tudo o que venceu desde o último
    /// fechamento.
    /// </summary>
    public static (DateTime De, DateTime Ate) PeriodoSugerido(IEnumerable<Comissao> todas, DateTime hoje)
    {
        var ate = hoje.Date;
        var ultimoAcertado = todas
            .Where(c => c.DataPagamento is not null)
            .Select(c => (DateTime?)c.Vencimento.Date)
            .Max();
        var de = ultimoAcertado is { } ultimo ? ultimo.AddDays(1) : new DateTime(ate.Year, ate.Month, 1);
        if (de > ate)
            de = ate.AddMonths(-1).AddDays(1);
        return (de, ate);
    }

    /// <summary>
    /// Parcelas do fechamento: comissão ainda não acertada (sem data de
    /// pagamento) e vencimento até <paramref name="ate"/>. Antes de
    /// <paramref name="de"/> entram sempre as "Pago" esquecidas (são devidas);
    /// as "Não pago" antigas só se <paramref name="incluirNaoPagasAnteriores"/>.
    /// </summary>
    public static List<Comissao> Candidatas(IEnumerable<Comissao> todas, DateTime de, DateTime ate,
        bool incluirNaoPagasAnteriores) =>
        todas
            .Where(c => c.DataPagamento is null && c.Vencimento.Date <= ate.Date)
            .Where(c => c.Vencimento.Date >= de.Date || c.Paga || incluirNaoPagasAnteriores)
            .OrderBy(c => c.Vencimento)
            .ThenBy(c => c.Id)
            .ToList();

    /// <summary>Vem marcada para pagar: o status já é "Pago" (cliente pagou).</summary>
    public static bool MarcadaPorPadrao(Comissao comissao) => comissao.Paga;
}
