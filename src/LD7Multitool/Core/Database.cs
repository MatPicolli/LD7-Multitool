using Microsoft.Data.Sqlite;

namespace LD7Multitool.Core;

/// <summary>
/// Acesso ao banco SQLite local. O arquivo fica em %AppData%\LD7Multitool\dados.db.
/// </summary>
public static class Database
{
    public static string CaminhoBanco { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LD7Multitool",
        "dados.db");

    public static SqliteConnection AbrirConexao()
    {
        var conexao = new SqliteConnection($"Data Source={CaminhoBanco}");
        conexao.Open();
        return conexao;
    }

    public static void Inicializar()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoBanco)!);

        using var conexao = AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            CREATE TABLE IF NOT EXISTS configuracoes (
                chave TEXT PRIMARY KEY,
                valor TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cadastros_email (
                id      INTEGER PRIMARY KEY AUTOINCREMENT,
                nome    TEXT NOT NULL,
                assunto TEXT NOT NULL DEFAULT '',
                corpo   TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS cadastro_email_destinatarios (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                cadastro_id INTEGER NOT NULL REFERENCES cadastros_email(id) ON DELETE CASCADE,
                email      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cadastro_email_arquivos (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                cadastro_id INTEGER NOT NULL REFERENCES cadastros_email(id) ON DELETE CASCADE,
                caminho     TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS boletos (
                id            INTEGER PRIMARY KEY AUTOINCREMENT,
                nome          TEXT NOT NULL,
                valor         TEXT NOT NULL,
                validade      TEXT NOT NULL,
                nosso_numero  TEXT NOT NULL DEFAULT '',
                nfe_referente TEXT NOT NULL DEFAULT '',
                estado        INTEGER NOT NULL DEFAULT 0
            );
            """;
        comando.ExecuteNonQuery();

        // Chaves estrangeiras precisam ser habilitadas por conexão no SQLite;
        // como usamos ON DELETE CASCADE, os repositórios cuidam disso ao abrir.
    }

    /// <summary>Abre uma conexão com suporte a chaves estrangeiras (ON DELETE CASCADE).</summary>
    public static SqliteConnection AbrirConexaoComFk()
    {
        var conexao = AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "PRAGMA foreign_keys = ON;";
        comando.ExecuteNonQuery();
        return conexao;
    }
}
