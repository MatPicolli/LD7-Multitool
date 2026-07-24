using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Clientes;

public static class RepresentanteRepository
{
    public static List<Representante> Listar()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT id, nome, email, telefone1, telefone2 FROM representantes ORDER BY nome";

        var lista = new List<Representante>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            lista.Add(Ler(leitor));
        return lista;
    }

    public static void Inserir(Representante r)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO representantes (nome, email, telefone1, telefone2)
            VALUES ($nome, $email, $tel1, $tel2)
            RETURNING id
            """;
        PreencherParametros(comando, r);
        r.Id = (long)comando.ExecuteScalar()!;
    }

    public static void Atualizar(Representante r)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE representantes SET nome = $nome, email = $email,
                telefone1 = $tel1, telefone2 = $tel2
            WHERE id = $id
            """;
        PreencherParametros(comando, r);
        comando.Parameters.AddWithValue("$id", r.Id);
        comando.ExecuteNonQuery();
    }

    public static void Excluir(long id)
    {
        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM representantes WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    private static void PreencherParametros(SqliteCommand comando, Representante r)
    {
        comando.Parameters.AddWithValue("$nome", r.Nome);
        comando.Parameters.AddWithValue("$email", r.Email);
        comando.Parameters.AddWithValue("$tel1", r.Telefone1);
        comando.Parameters.AddWithValue("$tel2", r.Telefone2);
    }

    private static Representante Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        Nome = leitor.GetString(1),
        Email = leitor.GetString(2),
        Telefone1 = leitor.GetString(3),
        Telefone2 = leitor.GetString(4),
    };
}
