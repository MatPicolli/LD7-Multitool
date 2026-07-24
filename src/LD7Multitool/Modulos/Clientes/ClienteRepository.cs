using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Clientes;

public static class ClienteRepository
{
    public static List<Cliente> Listar()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT c.id, c.codigo, c.razao_social, c.nome_fantasia, c.cpf_cnpj, c.ie,
                   c.endereco, c.cep, c.uf, c.cidade, c.bairro,
                   c.email1, c.email2, c.telefone1, c.telefone2,
                   c.contato, c.contato_email, c.contato_telefone,
                   c.representante_id, COALESCE(r.nome, '')
            FROM clientes c
            LEFT JOIN representantes r ON r.id = c.representante_id
            ORDER BY c.razao_social
            """;

        var lista = new List<Cliente>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            lista.Add(Ler(leitor));
        return lista;
    }

    public static void Inserir(Cliente c)
    {
        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO clientes (codigo, razao_social, nome_fantasia, cpf_cnpj, ie,
                endereco, cep, uf, cidade, bairro, email1, email2, telefone1, telefone2,
                contato, contato_email, contato_telefone, representante_id)
            VALUES ($codigo, $razaoSocial, $nomeFantasia, $cpfCnpj, $ie,
                $endereco, $cep, $uf, $cidade, $bairro, $email1, $email2, $tel1, $tel2,
                $contato, $contatoEmail, $contatoTel, $representanteId)
            RETURNING id
            """;
        PreencherParametros(comando, c, incluirCodigo: true);
        c.Id = (long)comando.ExecuteScalar()!;
    }

    public static void Atualizar(Cliente c)
    {
        using var conexao = Database.AbrirConexaoComFk();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE clientes SET
                razao_social = $razaoSocial, nome_fantasia = $nomeFantasia,
                cpf_cnpj = $cpfCnpj, ie = $ie,
                endereco = $endereco, cep = $cep, uf = $uf, cidade = $cidade, bairro = $bairro,
                email1 = $email1, email2 = $email2, telefone1 = $tel1, telefone2 = $tel2,
                contato = $contato, contato_email = $contatoEmail, contato_telefone = $contatoTel,
                representante_id = $representanteId
            WHERE id = $id
            """;
        PreencherParametros(comando, c, incluirCodigo: false);
        comando.Parameters.AddWithValue("$id", c.Id);
        comando.ExecuteNonQuery();
    }

    public static void Excluir(long id)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM clientes WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    /// <summary>Gera um código de 4 dígitos ainda não usado (ex.: "0492").</summary>
    public static string GerarCodigoUnico()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT COUNT(*) FROM clientes WHERE codigo = $codigo";
        var parametro = comando.Parameters.Add("$codigo", SqliteType.Text);

        var aleatorio = Random.Shared;
        string codigo;
        do
        {
            codigo = aleatorio.Next(0, 10000).ToString("D4");
            parametro.Value = codigo;
        } while ((long)comando.ExecuteScalar()! > 0);

        return codigo;
    }

    private static void PreencherParametros(SqliteCommand comando, Cliente c, bool incluirCodigo)
    {
        if (incluirCodigo)
            comando.Parameters.AddWithValue("$codigo", c.Codigo);
        comando.Parameters.AddWithValue("$razaoSocial", c.RazaoSocial);
        comando.Parameters.AddWithValue("$nomeFantasia", c.NomeFantasia);
        comando.Parameters.AddWithValue("$cpfCnpj", c.CpfCnpj);
        comando.Parameters.AddWithValue("$ie", c.Ie);
        comando.Parameters.AddWithValue("$endereco", c.Endereco);
        comando.Parameters.AddWithValue("$cep", c.Cep);
        comando.Parameters.AddWithValue("$uf", c.Uf);
        comando.Parameters.AddWithValue("$cidade", c.Cidade);
        comando.Parameters.AddWithValue("$bairro", c.Bairro);
        comando.Parameters.AddWithValue("$email1", c.Email1);
        comando.Parameters.AddWithValue("$email2", c.Email2);
        comando.Parameters.AddWithValue("$tel1", c.Telefone1);
        comando.Parameters.AddWithValue("$tel2", c.Telefone2);
        comando.Parameters.AddWithValue("$contato", c.Contato);
        comando.Parameters.AddWithValue("$contatoEmail", c.ContatoEmail);
        comando.Parameters.AddWithValue("$contatoTel", c.ContatoTelefone);
        comando.Parameters.AddWithValue("$representanteId", (object?)c.RepresentanteId ?? DBNull.Value);
    }

    private static Cliente Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        Codigo = leitor.GetString(1),
        RazaoSocial = leitor.GetString(2),
        NomeFantasia = leitor.GetString(3),
        CpfCnpj = leitor.GetString(4),
        Ie = leitor.GetString(5),
        Endereco = leitor.GetString(6),
        Cep = leitor.GetString(7),
        Uf = leitor.GetString(8),
        Cidade = leitor.GetString(9),
        Bairro = leitor.GetString(10),
        Email1 = leitor.GetString(11),
        Email2 = leitor.GetString(12),
        Telefone1 = leitor.GetString(13),
        Telefone2 = leitor.GetString(14),
        Contato = leitor.GetString(15),
        ContatoEmail = leitor.GetString(16),
        ContatoTelefone = leitor.GetString(17),
        RepresentanteId = leitor.IsDBNull(18) ? null : leitor.GetInt64(18),
        RepresentanteNome = leitor.GetString(19),
    };
}
