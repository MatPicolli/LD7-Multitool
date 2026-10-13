using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>Uma linha lida da planilha: a comissão montada, ou o motivo de não ter dado.</summary>
public sealed record LinhaPlanilha(int NumeroLinha, Comissao? Comissao, string? Problema);

/// <summary>
/// Converte uma aba da planilha "Comissão" (2023, 2024, 2025, 2026 NOVA MILITAR...)
/// em linhas de <see cref="Comissao"/>. As colunas são localizadas pelo
/// <b>texto do cabeçalho</b> ("Nota fiscal", "Nº b", "Cliente"...), não pela
/// posição — as abas variam (cabeçalho na linha 2 ou 3, "Comissão" ou
/// "Comissão 10%") e algumas têm uma segunda tabela auxiliar à direita, que é
/// ignorada.
/// </summary>
public static class PlanilhaComissoes
{
    private enum Campo
    {
        NotaFiscal, Boleto, Cliente, Pedido, ValorNf, Vencimento,
        Parcela, ValorParcela, Comissao, Status, DataPagamento,
    }

    // A ordem importa: cada coluna fica com o PRIMEIRO campo cuja regra casar.
    private static readonly (Campo Campo, Func<string, bool> Casa)[] Regras =
    {
        (Campo.NotaFiscal, h => h.StartsWith("nota fiscal") || h == "nf"),
        (Campo.Boleto, h => h is "no b" or "n b" or "boleto" || h.StartsWith("no boleto") || h.StartsWith("no do boleto")),
        (Campo.Cliente, h => h.StartsWith("cliente")),
        (Campo.ValorParcela, h => h.StartsWith("valor da parcela") || h.StartsWith("valor parcela")),
        (Campo.Parcela, h => h.Contains("parcelas") || h == "parcela"),
        (Campo.ValorNf, h => h.StartsWith("valor nf") || h.StartsWith("valor da nf") || h.StartsWith("valor da nota")),
        (Campo.Vencimento, h => h.StartsWith("vencimento")),
        (Campo.Status, h => h.StartsWith("status")),
        (Campo.DataPagamento, h => h.StartsWith("pag") || h.StartsWith("data pag")),
        (Campo.Comissao, h => h.StartsWith("comissao")),
        (Campo.Pedido, h => h.StartsWith("ref") || h.Contains("pedido")),
    };

    // Sem estes três não há como montar uma linha de comissão.
    private static readonly Campo[] Obrigatorios = { Campo.Cliente, Campo.Vencimento, Campo.ValorParcela };

    /// <summary>
    /// Lê a aba. Devolve null em <c>Linhas</c> (com a mensagem em <c>Erro</c>)
    /// quando não encontra o cabeçalho.
    /// </summary>
    public static (List<LinhaPlanilha>? Linhas, string? Erro) Ler(PlanilhaXlsx aba, decimal percentualPadrao)
    {
        var (linhaCabecalho, colunas) = LocalizarCabecalho(aba);
        if (linhaCabecalho == 0)
        {
            return (null,
                "Não encontrei nesta aba o cabeçalho da tabela de comissões " +
                "(colunas \"Cliente\", \"Vencimento\" e \"Valor da parcela\").");
        }

        var linhas = new List<LinhaPlanilha>();
        for (var numero = linhaCabecalho + 1; numero <= aba.UltimaLinha; numero++)
        {
            CelulaXlsx? Celula(Campo campo) =>
                colunas.TryGetValue(campo, out var coluna) ? aba.Celula(numero, coluna) : null;
            string Texto(Campo campo) => Celula(campo)?.ComoTexto() ?? "";

            var cliente = Texto(Campo.Cliente);
            var notaFiscal = Texto(Campo.NotaFiscal);
            var valorParcela = Celula(Campo.ValorParcela)?.ComoDecimal() ?? 0m;

            // Linhas "modelo" da planilha (só o Nº, a fórmula e "Não Pago"
            // preenchidos) não são vendas — ficam de fora sem aviso.
            if (cliente.Length == 0 && notaFiscal.Length == 0 && valorParcela == 0m)
                continue;

            if (Celula(Campo.Vencimento)?.ComoData() is not { } vencimento)
            {
                linhas.Add(new LinhaPlanilha(numero, null, "Sem vencimento válido"));
                continue;
            }

            var comissao = new Comissao
            {
                NotaFiscal = notaFiscal,
                NumeroBoleto = Texto(Campo.Boleto),
                Cliente = cliente,
                RefPedido = Texto(Campo.Pedido),
                ValorNf = Celula(Campo.ValorNf)?.ComoDecimal() ?? 0m,
                Vencimento = vencimento,
                Parcela = LerParcela(Celula(Campo.Parcela)),
                ValorParcela = valorParcela,
                Percentual = DeduzirPercentual(Celula(Campo.Comissao), valorParcela, percentualPadrao),
                Paga = Comissao.Normalizar(Texto(Campo.Status)) is "pago" or "paga",
            };
            if (comissao.Paga)
                comissao.DataPagamento = Celula(Campo.DataPagamento)?.ComoData();

            linhas.Add(new LinhaPlanilha(numero, comissao, null));
        }
        return (linhas, null);
    }

    /// <summary>
    /// Procura, nas primeiras linhas, a que tem mais títulos reconhecidos (a aba
    /// de 2026 tem um cabeçalho parcial da tabela auxiliar uma linha acima).
    /// </summary>
    private static (int Linha, Dictionary<Campo, int> Colunas) LocalizarCabecalho(PlanilhaXlsx aba)
    {
        var melhorLinha = 0;
        var melhores = new Dictionary<Campo, int>();
        for (var linha = 1; linha <= Math.Min(15, aba.UltimaLinha); linha++)
        {
            var colunas = new Dictionary<Campo, int>();
            for (var coluna = 1; coluna <= aba.UltimaColuna; coluna++)
            {
                if (aba.Celula(linha, coluna) is not { Numero: null } celula)
                    continue;
                var titulo = NormalizarTitulo(celula.Texto);
                foreach (var (campo, casa) in Regras)
                {
                    if (colunas.ContainsKey(campo) || !casa(titulo))
                        continue;
                    colunas[campo] = coluna;
                    break;
                }
            }

            if (Obrigatorios.All(colunas.ContainsKey) && colunas.Count > melhores.Count)
            {
                melhorLinha = linha;
                melhores = colunas;
            }
        }
        return (melhorLinha, melhores);
    }

    /// <summary>"Nº de parcelas" → "no de parcelas" (minúsculas, sem acento, º → o).</summary>
    private static string NormalizarTitulo(string titulo) =>
        string.Join(' ', Comissao.Normalizar(titulo.Replace('º', 'o').Replace('°', 'o'))
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// O Excel converteu boa parte das parcelas digitadas como "1/5" em datas
    /// (1º de maio, exibido "01/05"). Uma data volta a ser "dia/mês" = parcela/total.
    /// </summary>
    private static string LerParcela(CelulaXlsx? celula)
    {
        if (celula is null)
            return "";
        if (celula.Numero is { } numero && numero >= 367 && celula.ComoData() is { } data)
            return $"{data.Day:00}/{data.Month:00}";
        return celula.ComoTexto();
    }

    /// <summary>
    /// O percentual sai da própria planilha (comissão ÷ parcela), para abas
    /// antigas com outra taxa; sem a coluna ou sem valor, usa o padrão.
    /// </summary>
    private static decimal DeduzirPercentual(CelulaXlsx? celulaComissao, decimal valorParcela, decimal padrao)
    {
        if (valorParcela <= 0m || celulaComissao?.Numero is not { } comissao || comissao <= 0)
            return padrao;
        var percentual = Math.Round(Convert.ToDecimal(comissao) / valorParcela * 100m, 2);
        return percentual is > 0m and <= 100m ? percentual : padrao;
    }

    /// <summary>Texto amigável de um valor para a prévia ("R$ 1.234,56").</summary>
    public static string Moeda(decimal valor) => valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
}
