using System.Globalization;
using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Despesas;

public static class LancamentoDespesaRepository
{
    // Mesma convenção do módulo Boletos: valor em cultura invariante e datas em
    // ISO (yyyy-MM-dd), para não depender da configuração regional da máquina.

    private const string Colunas = """
        id, despesa_id, competencia, vencimento, valor, linha_digitavel,
        situacao, caminho_arquivo, origem, coletado_em, chave_origem
        """;

    public static List<LancamentoDespesa> ListarPorDespesa(long despesaId)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"""
            SELECT {Colunas} FROM despesas_lancamentos
            WHERE despesa_id = $despesa
            ORDER BY vencimento DESC, id DESC
            """;
        comando.Parameters.AddWithValue("$despesa", despesaId);

        var lancamentos = new List<LancamentoDespesa>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            lancamentos.Add(Ler(leitor));
        return lancamentos;
    }

    /// <summary>
    /// O lançamento mais recente de cada despesa, indexado pelo id da despesa.
    /// "Mais recente" é o de maior vencimento (desempate pelo id) — é o que a
    /// tela principal mostra como "última despesa" de cada item.
    /// </summary>
    public static Dictionary<long, LancamentoDespesa> UltimoPorDespesa()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"""
            SELECT {Colunas} FROM despesas_lancamentos
            ORDER BY despesa_id, vencimento, id
            """;

        // Percorrendo em ordem crescente, o último lido de cada despesa vence.
        var ultimos = new Dictionary<long, LancamentoDespesa>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            var lancamento = Ler(leitor);
            ultimos[lancamento.DespesaId] = lancamento;
        }
        return ultimos;
    }

    /// <summary>
    /// Grava o lançamento. Devolve <c>false</c> (sem gravar) quando a mesma
    /// chave de origem já existe para a despesa — é o que impede a coleta
    /// automática de duplicar um boleto já cadastrado.
    /// </summary>
    public static bool Inserir(LancamentoDespesa lancamento)
    {
        if (lancamento.ChaveOrigem.Length == 0)
            lancamento.ChaveOrigem = "manual:" + Guid.NewGuid().ToString("N");

        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"""
            INSERT OR IGNORE INTO despesas_lancamentos
                (despesa_id, competencia, vencimento, valor, linha_digitavel,
                 situacao, caminho_arquivo, origem, coletado_em, chave_origem)
            VALUES ($despesa, $competencia, $vencimento, $valor, $linha,
                    $situacao, $arquivo, $origem, $coletadoEm, $chave)
            """;
        PreencherParametros(comando, lancamento);
        if (comando.ExecuteNonQuery() == 0)
            return false;

        using var ultimoId = conexao.CreateCommand();
        ultimoId.CommandText = "SELECT last_insert_rowid()";
        lancamento.Id = (long)ultimoId.ExecuteScalar()!;
        return true;
    }

    public static void Atualizar(LancamentoDespesa lancamento)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE despesas_lancamentos SET
                competencia = $competencia,
                vencimento = $vencimento,
                valor = $valor,
                linha_digitavel = $linha,
                situacao = $situacao,
                caminho_arquivo = $arquivo,
                origem = $origem,
                coletado_em = $coletadoEm
            WHERE id = $id
            """;
        PreencherParametros(comando, lancamento);
        comando.Parameters.AddWithValue("$id", lancamento.Id);
        comando.ExecuteNonQuery();
    }

    public static void AlterarSituacao(long id, SituacaoDespesa situacao)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "UPDATE despesas_lancamentos SET situacao = $situacao WHERE id = $id";
        comando.Parameters.AddWithValue("$situacao", (int)situacao);
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    public static void Excluir(long id)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM despesas_lancamentos WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    /// <summary>Chaves de origem já usadas por uma despesa (a coleta consulta antes de baixar).</summary>
    public static HashSet<string> ChavesDe(long despesaId)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText =
            "SELECT chave_origem FROM despesas_lancamentos WHERE despesa_id = $despesa";
        comando.Parameters.AddWithValue("$despesa", despesaId);

        var chaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            chaves.Add(leitor.GetString(0));
        return chaves;
    }

    private static void PreencherParametros(SqliteCommand comando, LancamentoDespesa lancamento)
    {
        comando.Parameters.AddWithValue("$despesa", lancamento.DespesaId);
        comando.Parameters.AddWithValue("$competencia", lancamento.Competencia);
        comando.Parameters.AddWithValue("$vencimento", lancamento.Vencimento.ToString("yyyy-MM-dd"));
        comando.Parameters.AddWithValue("$valor", lancamento.Valor.ToString(CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$linha", lancamento.LinhaDigitavel);
        comando.Parameters.AddWithValue("$situacao", (int)lancamento.Situacao);
        comando.Parameters.AddWithValue("$arquivo", lancamento.CaminhoArquivo);
        comando.Parameters.AddWithValue("$origem", (int)lancamento.Origem);
        comando.Parameters.AddWithValue("$coletadoEm", lancamento.ColetadoEm.ToString("yyyy-MM-dd HH:mm:ss"));
        comando.Parameters.AddWithValue("$chave", lancamento.ChaveOrigem);
    }

    private static LancamentoDespesa Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        DespesaId = leitor.GetInt64(1),
        Competencia = leitor.GetString(2),
        Vencimento = DateTime.ParseExact(leitor.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        Valor = decimal.Parse(leitor.GetString(4), CultureInfo.InvariantCulture),
        LinhaDigitavel = leitor.GetString(5),
        Situacao = (SituacaoDespesa)leitor.GetInt32(6),
        CaminhoArquivo = leitor.GetString(7),
        Origem = (OrigemLancamento)leitor.GetInt32(8),
        ColetadoEm = DateTime.TryParseExact(leitor.GetString(9), "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var quando) ? quando : DateTime.MinValue,
        ChaveOrigem = leitor.GetString(10),
    };
}
