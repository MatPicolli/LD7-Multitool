using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Lembra qual cadastro (do Auto-Email) foi usado para enviar boletos de um
/// determinado nome de pagador, para pré-selecioná-lo da próxima vez.
/// </summary>
public static class PreferenciaCadastroRepository
{
    public static long? ObterCadastro(string nomeBoleto)
    {
        if (string.IsNullOrWhiteSpace(nomeBoleto))
            return null;

        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText =
            "SELECT cadastro_id FROM preferencia_cadastro_boleto WHERE nome = $nome";
        comando.Parameters.AddWithValue("$nome", nomeBoleto);
        return comando.ExecuteScalar() is long id ? id : null;
    }

    public static void Definir(string nomeBoleto, long cadastroId)
    {
        if (string.IsNullOrWhiteSpace(nomeBoleto))
            return;

        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO preferencia_cadastro_boleto (nome, cadastro_id)
            VALUES ($nome, $cadastroId)
            ON CONFLICT(nome) DO UPDATE SET cadastro_id = excluded.cadastro_id
            """;
        comando.Parameters.AddWithValue("$nome", nomeBoleto);
        comando.Parameters.AddWithValue("$cadastroId", cadastroId);
        comando.ExecuteNonQuery();
    }
}
