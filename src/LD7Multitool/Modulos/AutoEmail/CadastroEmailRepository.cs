using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

public static class CadastroEmailRepository
{
    public static List<CadastroEmail> Listar()
    {
        using var conexao = Database.AbrirConexao();

        var cadastros = new Dictionary<long, CadastroEmail>();
        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText = "SELECT id, codigo, nome FROM cadastros_email ORDER BY nome";
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                var cadastro = new CadastroEmail
                {
                    Id = leitor.GetInt64(0),
                    Codigo = leitor.GetString(1),
                    Nome = leitor.GetString(2),
                };
                cadastros[cadastro.Id] = cadastro;
            }
        }

        using (var comando = conexao.CreateCommand())
        {
            comando.CommandText =
                "SELECT cadastro_id, email FROM cadastro_email_destinatarios ORDER BY id";
            using var leitor = comando.ExecuteReader();
            while (leitor.Read())
            {
                var cadastroId = leitor.GetInt64(0);
                if (cadastros.TryGetValue(cadastroId, out var cadastro))
                    cadastro.Destinatarios.Add(leitor.GetString(1));
            }
        }

        return cadastros.Values.ToList();
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
                INSERT INTO cadastros_email (codigo, nome)
                VALUES ($codigo, $nome)
                RETURNING id
                """;
            inserir.Parameters.AddWithValue("$codigo", cadastro.Codigo);
            inserir.Parameters.AddWithValue("$nome", cadastro.Nome);
            cadastro.Id = (long)inserir.ExecuteScalar()!;
        }
        else
        {
            using var atualizar = conexao.CreateCommand();
            atualizar.Transaction = transacao;
            atualizar.CommandText =
                "UPDATE cadastros_email SET codigo = $codigo, nome = $nome WHERE id = $id";
            atualizar.Parameters.AddWithValue("$codigo", cadastro.Codigo);
            atualizar.Parameters.AddWithValue("$nome", cadastro.Nome);
            atualizar.Parameters.AddWithValue("$id", cadastro.Id);
            atualizar.ExecuteNonQuery();

            using var limpar = conexao.CreateCommand();
            limpar.Transaction = transacao;
            limpar.CommandText =
                "DELETE FROM cadastro_email_destinatarios WHERE cadastro_id = $id";
            limpar.Parameters.AddWithValue("$id", cadastro.Id);
            limpar.ExecuteNonQuery();
        }

        foreach (var email in cadastro.Destinatarios)
        {
            using var inserirEmail = conexao.CreateCommand();
            inserirEmail.Transaction = transacao;
            inserirEmail.CommandText = """
                INSERT INTO cadastro_email_destinatarios (cadastro_id, email)
                VALUES ($id, $email)
                """;
            inserirEmail.Parameters.AddWithValue("$id", cadastro.Id);
            inserirEmail.Parameters.AddWithValue("$email", email);
            inserirEmail.ExecuteNonQuery();
        }

        transacao.Commit();
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
