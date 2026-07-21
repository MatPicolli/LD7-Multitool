using Microsoft.Data.Sqlite;

namespace LD7Multitool.Core;

/// <summary>Armazenamento simples de pares chave/valor na tabela `configuracoes`.</summary>
public static class ConfiguracaoRepository
{
    public static string? Obter(string chave)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT valor FROM configuracoes WHERE chave = $chave";
        comando.Parameters.AddWithValue("$chave", chave);
        return comando.ExecuteScalar() as string;
    }

    public static void Definir(string chave, string valor)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO configuracoes (chave, valor) VALUES ($chave, $valor)
            ON CONFLICT(chave) DO UPDATE SET valor = excluded.valor
            """;
        comando.Parameters.AddWithValue("$chave", chave);
        comando.Parameters.AddWithValue("$valor", valor);
        comando.ExecuteNonQuery();
    }
}
