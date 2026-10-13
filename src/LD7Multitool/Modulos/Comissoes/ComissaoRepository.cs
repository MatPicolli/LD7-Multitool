using System.Globalization;
using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Comissoes;

public static class ComissaoRepository
{
    // Valores gravados como texto em cultura invariante e datas como ISO
    // (yyyy-MM-dd), igual aos boletos, para não depender da região da máquina.
    private const string FormatoData = "yyyy-MM-dd";

    private const string Colunas = """
        id, nota_fiscal, numero_boleto, cliente, ref_pedido, valor_nf, vencimento,
        parcela, valor_parcela, percentual, paga, data_pagamento
        """;

    /// <summary>Todas as parcelas, na ordem da planilha (vencimento).</summary>
    public static List<Comissao> Listar()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"SELECT {Colunas} FROM comissoes ORDER BY vencimento, id";

        var lista = new List<Comissao>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            lista.Add(Ler(leitor));
        return lista;
    }

    public static void Inserir(Comissao comissao) => InserirVarias(new[] { comissao });

    /// <summary>Insere várias parcelas numa transação só (lançamento de venda / importação).</summary>
    public static void InserirVarias(IEnumerable<Comissao> comissoes)
    {
        using var conexao = Database.AbrirConexao();
        using var transacao = conexao.BeginTransaction();
        foreach (var comissao in comissoes)
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = """
                INSERT INTO comissoes (nota_fiscal, numero_boleto, cliente, ref_pedido, valor_nf, vencimento,
                    parcela, valor_parcela, percentual, paga, data_pagamento)
                VALUES ($nf, $boleto, $cliente, $pedido, $valorNf, $vencimento,
                    $parcela, $valorParcela, $percentual, $paga, $dataPagamento)
                RETURNING id
                """;
            PreencherParametros(comando, comissao);
            comissao.Id = (long)comando.ExecuteScalar()!;
        }
        transacao.Commit();
    }

    public static void Atualizar(Comissao comissao)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE comissoes SET
                nota_fiscal = $nf, numero_boleto = $boleto, cliente = $cliente, ref_pedido = $pedido,
                valor_nf = $valorNf, vencimento = $vencimento, parcela = $parcela,
                valor_parcela = $valorParcela, percentual = $percentual,
                paga = $paga, data_pagamento = $dataPagamento
            WHERE id = $id
            """;
        PreencherParametros(comando, comissao);
        comando.Parameters.AddWithValue("$id", comissao.Id);
        comando.ExecuteNonQuery();
    }

    /// <summary>Marca várias parcelas como pagas (com a data) ou como não pagas (limpa a data).</summary>
    public static void DefinirPagamento(IEnumerable<long> ids, bool paga, DateTime? data)
    {
        using var conexao = Database.AbrirConexao();
        using var transacao = conexao.BeginTransaction();
        foreach (var id in ids)
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = "UPDATE comissoes SET paga = $paga, data_pagamento = $data WHERE id = $id";
            comando.Parameters.AddWithValue("$paga", paga ? 1 : 0);
            comando.Parameters.AddWithValue("$data",
                paga && data is { } d ? d.ToString(FormatoData, CultureInfo.InvariantCulture) : "");
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
        transacao.Commit();
    }

    public static void Excluir(IEnumerable<long> ids)
    {
        using var conexao = Database.AbrirConexao();
        using var transacao = conexao.BeginTransaction();
        foreach (var id in ids)
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = "DELETE FROM comissoes WHERE id = $id";
            comando.Parameters.AddWithValue("$id", id);
            comando.ExecuteNonQuery();
        }
        transacao.Commit();
    }

    /// <summary>Nomes de cliente já usados (sugestões de autocompletar).</summary>
    public static List<string> ListarClientes()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT DISTINCT cliente FROM comissoes WHERE cliente <> '' ORDER BY cliente";
        var nomes = new List<string>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            nomes.Add(leitor.GetString(0));
        return nomes;
    }

    private static void PreencherParametros(SqliteCommand comando, Comissao c)
    {
        comando.Parameters.AddWithValue("$nf", c.NotaFiscal);
        comando.Parameters.AddWithValue("$boleto", c.NumeroBoleto);
        comando.Parameters.AddWithValue("$cliente", c.Cliente);
        comando.Parameters.AddWithValue("$pedido", c.RefPedido);
        comando.Parameters.AddWithValue("$valorNf", c.ValorNf.ToString(CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$vencimento", c.Vencimento.ToString(FormatoData, CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$parcela", c.Parcela);
        comando.Parameters.AddWithValue("$valorParcela", c.ValorParcela.ToString(CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$percentual", c.Percentual.ToString(CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$paga", c.Paga ? 1 : 0);
        comando.Parameters.AddWithValue("$dataPagamento",
            c.Paga && c.DataPagamento is { } d ? d.ToString(FormatoData, CultureInfo.InvariantCulture) : "");
    }

    private static Comissao Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        NotaFiscal = leitor.GetString(1),
        NumeroBoleto = leitor.GetString(2),
        Cliente = leitor.GetString(3),
        RefPedido = leitor.GetString(4),
        ValorNf = decimal.Parse(leitor.GetString(5), CultureInfo.InvariantCulture),
        Vencimento = DateTime.ParseExact(leitor.GetString(6), FormatoData, CultureInfo.InvariantCulture),
        Parcela = leitor.GetString(7),
        ValorParcela = decimal.Parse(leitor.GetString(8), CultureInfo.InvariantCulture),
        Percentual = decimal.Parse(leitor.GetString(9), CultureInfo.InvariantCulture),
        Paga = leitor.GetInt32(10) != 0,
        DataPagamento = DateTime.TryParseExact(leitor.GetString(11), FormatoData,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var data) ? data : null,
    };
}
