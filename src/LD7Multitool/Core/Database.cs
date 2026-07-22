using Microsoft.Data.Sqlite;

namespace LD7Multitool.Core;

/// <summary>
/// Acesso ao banco SQLite local. O arquivo dados.db (que também guarda todas
/// as configurações) fica na mesma pasta do executável, tornando o programa
/// portátil — copiar a pasta leva os dados junto.
/// </summary>
public static class Database
{
    public static string CaminhoBanco { get; } =
        Path.Combine(AppContext.BaseDirectory, "dados.db");

    // Local usado pelas versões anteriores; ainda é lido para migração.
    private static string CaminhoBancoAntigo => Path.Combine(
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
        // Migração: versões antigas gravavam em %AppData%\LD7Multitool.
        // Se ainda não há banco ao lado do executável, os dados antigos são
        // copiados para cá (o original fica no lugar, como backup).
        if (!File.Exists(CaminhoBanco) && File.Exists(CaminhoBancoAntigo))
            File.Copy(CaminhoBancoAntigo, CaminhoBanco);

        using var conexao = AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            CREATE TABLE IF NOT EXISTS configuracoes (
                chave TEXT PRIMARY KEY,
                valor TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cadastros_email (
                id     INTEGER PRIMARY KEY AUTOINCREMENT,
                codigo TEXT NOT NULL DEFAULT '',
                nome   TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS cadastro_email_destinatarios (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                cadastro_id INTEGER NOT NULL REFERENCES cadastros_email(id) ON DELETE CASCADE,
                email      TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS historico_email (
                id         INTEGER PRIMARY KEY AUTOINCREMENT,
                enviado_em TEXT NOT NULL,
                para       TEXT NOT NULL,
                assunto    TEXT NOT NULL DEFAULT '',
                anexos     INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS boletos (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                nome            TEXT NOT NULL,
                valor           TEXT NOT NULL,
                validade        TEXT NOT NULL,
                nosso_numero    TEXT NOT NULL DEFAULT '',
                nfe_referente   TEXT NOT NULL DEFAULT '',
                estado          INTEGER NOT NULL DEFAULT 0,
                caminho_arquivo TEXT NOT NULL DEFAULT ''
            );
            """;
        comando.ExecuteNonQuery();

        // Migrações de bancos criados por versões anteriores.
        AdicionarColunaSeFaltar(conexao, "boletos", "caminho_arquivo",
            "ALTER TABLE boletos ADD COLUMN caminho_arquivo TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "cadastros_email", "codigo",
            "ALTER TABLE cadastros_email ADD COLUMN codigo TEXT NOT NULL DEFAULT ''");

        // Os anexos por cadastro foram substituídos pelas pastas de NF-e e
        // boletos resolvidas na hora do envio — a tabela antiga é descartada.
        using (var limpar = conexao.CreateCommand())
        {
            limpar.CommandText = "DROP TABLE IF EXISTS cadastro_email_arquivos";
            limpar.ExecuteNonQuery();
        }

        // Chaves estrangeiras precisam ser habilitadas por conexão no SQLite;
        // como usamos ON DELETE CASCADE, os repositórios cuidam disso ao abrir.
    }

    private static void AdicionarColunaSeFaltar(
        SqliteConnection conexao, string tabela, string coluna, string comandoAlter)
    {
        using var verificar = conexao.CreateCommand();
        verificar.CommandText =
            $"SELECT COUNT(*) FROM pragma_table_info('{tabela}') WHERE name = '{coluna}'";
        if ((long)verificar.ExecuteScalar()! != 0)
            return;
        using var alterar = conexao.CreateCommand();
        alterar.CommandText = comandoAlter;
        alterar.ExecuteNonQuery();
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
