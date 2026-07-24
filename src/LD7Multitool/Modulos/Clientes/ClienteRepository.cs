using System.Globalization;
using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Clientes;

public static class ClienteRepository
{
    private const string FormatoData = "yyyy-MM-dd";

    public static List<Cliente> Listar()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT c.id, c.codigo, c.tipo_cliente, c.ativo, c.razao_social, c.nome_fantasia,
                   c.cpf, c.cnpj, c.ie, c.inscricao_municipal,
                   c.rg, c.estado_civil, c.sexo, c.data_nascimento, c.nacionalidade, c.naturalidade,
                   c.endereco, c.numero, c.complemento, c.cep, c.uf, c.cidade, c.bairro,
                   c.email1, c.email2, c.telefone1, c.telefone2, c.site,
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
            INSERT INTO clientes (codigo, tipo_cliente, ativo, razao_social, nome_fantasia,
                cpf, cnpj, ie, inscricao_municipal,
                rg, estado_civil, sexo, data_nascimento, nacionalidade, naturalidade,
                endereco, numero, complemento, cep, uf, cidade, bairro,
                email1, email2, telefone1, telefone2, site,
                contato, contato_email, contato_telefone, representante_id)
            VALUES ($codigo, $tipo, $ativo, $razaoSocial, $nomeFantasia,
                $cpf, $cnpj, $ie, $inscricaoMunicipal,
                $rg, $estadoCivil, $sexo, $dataNascimento, $nacionalidade, $naturalidade,
                $endereco, $numero, $complemento, $cep, $uf, $cidade, $bairro,
                $email1, $email2, $tel1, $tel2, $site,
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
                tipo_cliente = $tipo, ativo = $ativo,
                razao_social = $razaoSocial, nome_fantasia = $nomeFantasia,
                cpf = $cpf, cnpj = $cnpj, ie = $ie, inscricao_municipal = $inscricaoMunicipal,
                rg = $rg, estado_civil = $estadoCivil, sexo = $sexo,
                data_nascimento = $dataNascimento, nacionalidade = $nacionalidade, naturalidade = $naturalidade,
                endereco = $endereco, numero = $numero, complemento = $complemento,
                cep = $cep, uf = $uf, cidade = $cidade, bairro = $bairro,
                email1 = $email1, email2 = $email2, telefone1 = $tel1, telefone2 = $tel2, site = $site,
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

    /// <summary>
    /// Procura um cliente já cadastrado com o mesmo documento (CPF ou CNPJ),
    /// comparando só os dígitos. <paramref name="ignorarId"/> exclui o próprio
    /// cliente ao editar. Retorna null se não houver duplicado.
    /// </summary>
    public static Cliente? BuscarPorDocumento(string documento, long ignorarId = 0)
    {
        var digitos = new string(documento.Where(char.IsDigit).ToArray());
        if (digitos.Length == 0)
            return null;

        return Listar().FirstOrDefault(c =>
            c.Id != ignorarId &&
            (MesmoDocumento(c.Cpf, digitos) || MesmoDocumento(c.Cnpj, digitos)));
    }

    private static bool MesmoDocumento(string armazenado, string digitos)
    {
        var d = new string(armazenado.Where(char.IsDigit).ToArray());
        return d.Length > 0 && d == digitos;
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
        comando.Parameters.AddWithValue("$tipo", (int)c.Tipo);
        comando.Parameters.AddWithValue("$ativo", c.Ativo ? 1 : 0);
        comando.Parameters.AddWithValue("$razaoSocial", c.RazaoSocial);
        comando.Parameters.AddWithValue("$nomeFantasia", c.NomeFantasia);
        comando.Parameters.AddWithValue("$cpf", c.Cpf);
        comando.Parameters.AddWithValue("$cnpj", c.Cnpj);
        comando.Parameters.AddWithValue("$ie", c.Ie);
        comando.Parameters.AddWithValue("$inscricaoMunicipal", c.InscricaoMunicipal);
        comando.Parameters.AddWithValue("$rg", c.Rg);
        comando.Parameters.AddWithValue("$estadoCivil", c.EstadoCivil);
        comando.Parameters.AddWithValue("$sexo", c.Sexo);
        comando.Parameters.AddWithValue("$dataNascimento",
            c.DataNascimento?.ToString(FormatoData, CultureInfo.InvariantCulture) ?? "");
        comando.Parameters.AddWithValue("$nacionalidade", c.Nacionalidade);
        comando.Parameters.AddWithValue("$naturalidade", c.Naturalidade);
        comando.Parameters.AddWithValue("$endereco", c.Endereco);
        comando.Parameters.AddWithValue("$numero", c.Numero);
        comando.Parameters.AddWithValue("$complemento", c.Complemento);
        comando.Parameters.AddWithValue("$cep", c.Cep);
        comando.Parameters.AddWithValue("$uf", c.Uf);
        comando.Parameters.AddWithValue("$cidade", c.Cidade);
        comando.Parameters.AddWithValue("$bairro", c.Bairro);
        comando.Parameters.AddWithValue("$email1", c.Email1);
        comando.Parameters.AddWithValue("$email2", c.Email2);
        comando.Parameters.AddWithValue("$tel1", c.Telefone);
        comando.Parameters.AddWithValue("$tel2", c.Celular);
        comando.Parameters.AddWithValue("$site", c.Site);
        comando.Parameters.AddWithValue("$contato", c.Contato);
        comando.Parameters.AddWithValue("$contatoEmail", c.ContatoEmail);
        comando.Parameters.AddWithValue("$contatoTel", c.ContatoTelefone);
        comando.Parameters.AddWithValue("$representanteId", (object?)c.RepresentanteId ?? DBNull.Value);
    }

    private static Cliente Ler(SqliteDataReader leitor)
    {
        var dataNascimentoTexto = leitor.GetString(13);
        return new Cliente
        {
            Id = leitor.GetInt64(0),
            Codigo = leitor.GetString(1),
            Tipo = (TipoCliente)leitor.GetInt32(2),
            Ativo = leitor.GetInt32(3) != 0,
            RazaoSocial = leitor.GetString(4),
            NomeFantasia = leitor.GetString(5),
            Cpf = leitor.GetString(6),
            Cnpj = leitor.GetString(7),
            Ie = leitor.GetString(8),
            InscricaoMunicipal = leitor.GetString(9),
            Rg = leitor.GetString(10),
            EstadoCivil = leitor.GetString(11),
            Sexo = leitor.GetString(12),
            DataNascimento = DateTime.TryParseExact(
                dataNascimentoTexto, FormatoData, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var data) ? data : null,
            Nacionalidade = leitor.GetString(14),
            Naturalidade = leitor.GetString(15),
            Endereco = leitor.GetString(16),
            Numero = leitor.GetString(17),
            Complemento = leitor.GetString(18),
            Cep = leitor.GetString(19),
            Uf = leitor.GetString(20),
            Cidade = leitor.GetString(21),
            Bairro = leitor.GetString(22),
            Email1 = leitor.GetString(23),
            Email2 = leitor.GetString(24),
            Telefone = leitor.GetString(25),
            Celular = leitor.GetString(26),
            Site = leitor.GetString(27),
            Contato = leitor.GetString(28),
            ContatoEmail = leitor.GetString(29),
            ContatoTelefone = leitor.GetString(30),
            RepresentanteId = leitor.IsDBNull(31) ? null : leitor.GetInt64(31),
            RepresentanteNome = leitor.GetString(32),
        };
    }
}
