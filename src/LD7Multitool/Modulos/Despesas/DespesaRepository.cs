using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Despesas;

public static class DespesaRepository
{
    public static List<Despesa> Listar(bool somenteAtivos = false)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT id, nome, fornecedor, forma, metodo_coleta, url_portal, identificador,
                   documento, login, senha, dia_vencimento, padrao_arquivo, email_remetente,
                   email_assunto, config_http, observacoes, ativo, ordem
            FROM despesas
            """;
        if (somenteAtivos)
            comando.CommandText += " WHERE ativo = 1";
        comando.CommandText += " ORDER BY ordem, nome";

        var despesas = new List<Despesa>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            despesas.Add(Ler(leitor));
        return despesas;
    }

    public static void Inserir(Despesa despesa)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO despesas (nome, fornecedor, forma, metodo_coleta, url_portal, identificador,
                                  documento, login, senha, dia_vencimento, padrao_arquivo,
                                  email_remetente, email_assunto, config_http, observacoes, ativo, ordem)
            VALUES ($nome, $fornecedor, $forma, $metodo, $url, $identificador,
                    $documento, $login, $senha, $dia, $padraoArquivo,
                    $emailRemetente, $emailAssunto, $configHttp, $observacoes, $ativo, $ordem)
            RETURNING id
            """;
        PreencherParametros(comando, despesa);
        despesa.Id = (long)comando.ExecuteScalar()!;
    }

    public static void Atualizar(Despesa despesa)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE despesas SET
                nome = $nome,
                fornecedor = $fornecedor,
                forma = $forma,
                metodo_coleta = $metodo,
                url_portal = $url,
                identificador = $identificador,
                documento = $documento,
                login = $login,
                senha = $senha,
                dia_vencimento = $dia,
                padrao_arquivo = $padraoArquivo,
                email_remetente = $emailRemetente,
                email_assunto = $emailAssunto,
                config_http = $configHttp,
                observacoes = $observacoes,
                ativo = $ativo,
                ordem = $ordem
            WHERE id = $id
            """;
        PreencherParametros(comando, despesa);
        comando.Parameters.AddWithValue("$id", despesa.Id);
        comando.ExecuteNonQuery();
    }

    public static void Excluir(long id)
    {
        // Conexão com FK ligada: os lançamentos do item saem junto (ON DELETE CASCADE).
        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM despesas WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    public static int Contar()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM despesas";
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    public static int ProximaOrdem()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COALESCE(MAX(ordem), 0) + 1 FROM despesas";
        return Convert.ToInt32(comando.ExecuteScalar());
    }

    private static void PreencherParametros(SqliteCommand comando, Despesa despesa)
    {
        comando.Parameters.AddWithValue("$nome", despesa.Nome);
        comando.Parameters.AddWithValue("$fornecedor", despesa.Fornecedor);
        comando.Parameters.AddWithValue("$forma", (int)despesa.Forma);
        comando.Parameters.AddWithValue("$metodo", (int)despesa.Metodo);
        comando.Parameters.AddWithValue("$url", despesa.UrlPortal);
        comando.Parameters.AddWithValue("$identificador", despesa.Identificador);
        comando.Parameters.AddWithValue("$documento", despesa.Documento);
        comando.Parameters.AddWithValue("$login", despesa.Login);
        comando.Parameters.AddWithValue("$senha", despesa.SenhaProtegida);
        comando.Parameters.AddWithValue("$dia", despesa.DiaVencimento);
        comando.Parameters.AddWithValue("$padraoArquivo", despesa.PadraoArquivo);
        comando.Parameters.AddWithValue("$emailRemetente", despesa.EmailRemetente);
        comando.Parameters.AddWithValue("$emailAssunto", despesa.EmailAssunto);
        comando.Parameters.AddWithValue("$configHttp", despesa.ConfigHttp);
        comando.Parameters.AddWithValue("$observacoes", despesa.Observacoes);
        comando.Parameters.AddWithValue("$ativo", despesa.Ativo ? 1 : 0);
        comando.Parameters.AddWithValue("$ordem", despesa.Ordem);
    }

    private static Despesa Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        Nome = leitor.GetString(1),
        Fornecedor = leitor.GetString(2),
        Forma = (FormaObtencao)leitor.GetInt32(3),
        Metodo = (MetodoColeta)leitor.GetInt32(4),
        UrlPortal = leitor.GetString(5),
        Identificador = leitor.GetString(6),
        Documento = leitor.GetString(7),
        Login = leitor.GetString(8),
        SenhaProtegida = leitor.GetString(9),
        DiaVencimento = leitor.GetInt32(10),
        PadraoArquivo = leitor.GetString(11),
        EmailRemetente = leitor.GetString(12),
        EmailAssunto = leitor.GetString(13),
        ConfigHttp = leitor.GetString(14),
        Observacoes = leitor.GetString(15),
        Ativo = leitor.GetInt32(16) != 0,
        Ordem = leitor.GetInt32(17),
    };
}
