using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.AutoEmail;

public static class CadastroEmailRepository
{
    public static List<CadastroEmail> Listar()
    {
        using var conexao = Database.AbrirConexao();

        var cadastros = new Dictionary<long, CadastroEmail>();
        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = "SELECT id, nome, assunto, corpo FROM cadastros_email ORDER BY nome";
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                var cadastro = new CadastroEmail
                {
                    Id = leitor.GetInt64(0),
                    Nome = leitor.GetString(1),
                    Assunto = leitor.GetString(2),
                    Corpo = leitor.GetString(3),
                };
                cadastros[cadastro.Id] = cadastro;
            }
        }

        CarregarFilhos(conexao, "cadastro_email_destinatarios", "email",
            (id, valor) => cadastros[id].Destinatarios.Add(valor), cadastros);
        CarregarFilhos(conexao, "cadastro_email_arquivos", "caminho",
            (id, valor) => cadastros[id].Arquivos.Add(valor), cadastros);

        return cadastros.Values.ToList();
    }

    private static void CarregarFilhos(
        SqliteConnection conexao,
        string tabela,
        string coluna,
        Action<long, string> adicionar,
        Dictionary<long, CadastroEmail> cadastros)
    {
        using var comando = conexao.CreateCommand();
        comando.CommandText = $"SELECT cadastro_id, {coluna} FROM {tabela} ORDER BY id";
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            var cadastroId = leitor.GetInt64(0);
            if (cadastros.ContainsKey(cadastroId))
                adicionar(cadastroId, leitor.GetString(1));
        }
    }

    public static void Salvar(CadastroEmail cadastro)
    {
        using var conexao = Database.AbrirConexaoComFk();
        using var transacao = conexao.BeginTransaction();

        if (cadastro.Id == 0)
        {
            using var inserir = conexao.CreateCommand();
            inserir.Transaction = transacao;
            inserir.CommandText = """
                INSERT INTO cadastros_email (nome, assunto, corpo)
                VALUES ($nome, $assunto, $corpo)
                RETURNING id
                """;
            inserir.Parameters.AddWithValue("$nome", cadastro.Nome);
            inserir.Parameters.AddWithValue("$assunto", cadastro.Assunto);
            inserir.Parameters.AddWithValue("$corpo", cadastro.Corpo);
            cadastro.Id = (long)inserir.ExecuteScalar()!;
        }
        else
        {
            using var atualizar = conexao.CreateCommand();
            atualizar.Transaction = transacao;
            atualizar.CommandText = """
                UPDATE cadastros_email SET nome = $nome, assunto = $assunto, corpo = $corpo
                WHERE id = $id
                """;
            atualizar.Parameters.AddWithValue("$nome", cadastro.Nome);
            atualizar.Parameters.AddWithValue("$assunto", cadastro.Assunto);
            atualizar.Parameters.AddWithValue("$corpo", cadastro.Corpo);
            atualizar.Parameters.AddWithValue("$id", cadastro.Id);
            atualizar.ExecuteNonQuery();

            using var limpar = conexao.CreateCommand();
            limpar.Transaction = transacao;
            limpar.CommandText = """
                DELETE FROM cadastro_email_destinatarios WHERE cadastro_id = $id;
                DELETE FROM cadastro_email_arquivos WHERE cadastro_id = $id;
                """;
            limpar.Parameters.AddWithValue("$id", cadastro.Id);
            limpar.ExecuteNonQuery();
        }

        InserirFilhos(conexao, transacao, "cadastro_email_destinatarios", "email",
            cadastro.Id, cadastro.Destinatarios);
        InserirFilhos(conexao, transacao, "cadastro_email_arquivos", "caminho",
            cadastro.Id, cadastro.Arquivos);

        transacao.Commit();
    }

    private static void InserirFilhos(
        SqliteConnection conexao,
        SqliteTransaction transacao,
        string tabela,
        string coluna,
        long cadastroId,
        IEnumerable<string> valores)
    {
        foreach (var valor in valores)
        {
            using var comando = conexao.CreateCommand();
            comando.Transaction = transacao;
            comando.CommandText = $"INSERT INTO {tabela} (cadastro_id, {coluna}) VALUES ($id, $valor)";
            comando.Parameters.AddWithValue("$id", cadastroId);
            comando.Parameters.AddWithValue("$valor", valor);
            comando.ExecuteNonQuery();
        }
    }

    public static void Excluir(long id)
    {
        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM cadastros_email WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }
}
