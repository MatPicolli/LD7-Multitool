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

            CREATE TABLE IF NOT EXISTS preferencia_cadastro_boleto (
                nome        TEXT PRIMARY KEY COLLATE NOCASE,
                cadastro_id INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS boletos (
                id              INTEGER PRIMARY KEY AUTOINCREMENT,
                nome            TEXT NOT NULL,
                valor           TEXT NOT NULL,
                validade        TEXT NOT NULL,
                nosso_numero    TEXT NOT NULL DEFAULT '',
                nfe_referente   TEXT NOT NULL DEFAULT '',
                estado          INTEGER NOT NULL DEFAULT 0,
                caminho_arquivo TEXT NOT NULL DEFAULT '',
                caminho_nfe     TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS representantes (
                id        INTEGER PRIMARY KEY AUTOINCREMENT,
                nome      TEXT NOT NULL,
                email     TEXT NOT NULL DEFAULT '',
                telefone1 TEXT NOT NULL DEFAULT '',
                telefone2 TEXT NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS clientes (
                id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                codigo              TEXT NOT NULL UNIQUE,
                tipo_cliente        INTEGER NOT NULL DEFAULT 1,
                ativo               INTEGER NOT NULL DEFAULT 1,
                razao_social        TEXT NOT NULL,
                nome_fantasia       TEXT NOT NULL DEFAULT '',
                cpf                 TEXT NOT NULL DEFAULT '',
                cnpj                TEXT NOT NULL DEFAULT '',
                ie                  TEXT NOT NULL DEFAULT '',
                inscricao_municipal TEXT NOT NULL DEFAULT '',
                rg                  TEXT NOT NULL DEFAULT '',
                estado_civil        TEXT NOT NULL DEFAULT '',
                sexo                TEXT NOT NULL DEFAULT '',
                data_nascimento     TEXT NOT NULL DEFAULT '',
                nacionalidade       TEXT NOT NULL DEFAULT '',
                naturalidade        TEXT NOT NULL DEFAULT '',
                endereco            TEXT NOT NULL DEFAULT '',
                numero              TEXT NOT NULL DEFAULT '',
                complemento         TEXT NOT NULL DEFAULT '',
                cep                 TEXT NOT NULL DEFAULT '',
                uf                  TEXT NOT NULL DEFAULT '',
                cidade              TEXT NOT NULL DEFAULT '',
                bairro              TEXT NOT NULL DEFAULT '',
                email1              TEXT NOT NULL DEFAULT '',
                email2              TEXT NOT NULL DEFAULT '',
                telefone1           TEXT NOT NULL DEFAULT '',
                telefone2           TEXT NOT NULL DEFAULT '',
                site                TEXT NOT NULL DEFAULT '',
                contato             TEXT NOT NULL DEFAULT '',
                contato_email       TEXT NOT NULL DEFAULT '',
                contato_telefone    TEXT NOT NULL DEFAULT '',
                representante_id    INTEGER REFERENCES representantes(id) ON DELETE SET NULL
            );
            """;
        comando.ExecuteNonQuery();

        // Migrações de bancos criados por versões anteriores.
        AdicionarColunaSeFaltar(conexao, "boletos", "caminho_arquivo",
            "ALTER TABLE boletos ADD COLUMN caminho_arquivo TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "boletos", "caminho_nfe",
            "ALTER TABLE boletos ADD COLUMN caminho_nfe TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "cadastros_email", "codigo",
            "ALTER TABLE cadastros_email ADD COLUMN codigo TEXT NOT NULL DEFAULT ''");

        // clientes: campos adicionados depois do lançamento inicial do módulo
        // (tela reformulada com pessoa física/jurídica e mais dados cadastrais).
        AdicionarColunaSeFaltar(conexao, "clientes", "tipo_cliente",
            "ALTER TABLE clientes ADD COLUMN tipo_cliente INTEGER NOT NULL DEFAULT 1");
        AdicionarColunaSeFaltar(conexao, "clientes", "ativo",
            "ALTER TABLE clientes ADD COLUMN ativo INTEGER NOT NULL DEFAULT 1");
        AdicionarColunaSeFaltar(conexao, "clientes", "cpf",
            "ALTER TABLE clientes ADD COLUMN cpf TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "cnpj",
            "ALTER TABLE clientes ADD COLUMN cnpj TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "inscricao_municipal",
            "ALTER TABLE clientes ADD COLUMN inscricao_municipal TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "rg",
            "ALTER TABLE clientes ADD COLUMN rg TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "estado_civil",
            "ALTER TABLE clientes ADD COLUMN estado_civil TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "sexo",
            "ALTER TABLE clientes ADD COLUMN sexo TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "data_nascimento",
            "ALTER TABLE clientes ADD COLUMN data_nascimento TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "nacionalidade",
            "ALTER TABLE clientes ADD COLUMN nacionalidade TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "naturalidade",
            "ALTER TABLE clientes ADD COLUMN naturalidade TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "numero",
            "ALTER TABLE clientes ADD COLUMN numero TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "complemento",
            "ALTER TABLE clientes ADD COLUMN complemento TEXT NOT NULL DEFAULT ''");
        AdicionarColunaSeFaltar(conexao, "clientes", "site",
            "ALTER TABLE clientes ADD COLUMN site TEXT NOT NULL DEFAULT ''");

        // O campo único "cpf_cnpj" foi separado em "cpf"/"cnpj" — se a coluna
        // antiga ainda existir, seus valores são migrados uma única vez (pelo
        // número de dígitos: 11 = CPF/Física, senão CNPJ/Jurídica) para as
        // colunas novas; a coluna antiga fica sem uso, mas não é apagada.
        if (ColunaExiste(conexao, "clientes", "cpf_cnpj"))
            MigrarCpfCnpj(conexao);

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

    private static void MigrarCpfCnpj(SqliteConnection conexao)
    {
        var atualizacoes = new List<(long Id, string Cpf, string Cnpj, int Tipo)>();
        using (var selecionar = conexao.CreateCommand())
        {
            selecionar.CommandText =
                "SELECT id, cpf_cnpj FROM clientes WHERE cpf = '' AND cnpj = '' AND cpf_cnpj <> ''";
            using var leitor = selecionar.ExecuteReader();
            while (leitor.Read())
            {
                var id = leitor.GetInt64(0);
                var valor = leitor.GetString(1);
                var digitos = new string(valor.Where(char.IsDigit).ToArray());
                atualizacoes.Add(digitos.Length == 11
                    ? (id, valor, "", 0)   // 0 = Física
                    : (id, "", valor, 1)); // 1 = Jurídica
            }
        }

        foreach (var (id, cpf, cnpj, tipo) in atualizacoes)
        {
            using var atualizar = conexao.CreateCommand();
            atualizar.CommandText = """
                UPDATE clientes SET cpf = $cpf, cnpj = $cnpj, tipo_cliente = $tipo WHERE id = $id
                """;
            atualizar.Parameters.AddWithValue("$cpf", cpf);
            atualizar.Parameters.AddWithValue("$cnpj", cnpj);
            atualizar.Parameters.AddWithValue("$tipo", tipo);
            atualizar.Parameters.AddWithValue("$id", id);
            atualizar.ExecuteNonQuery();
        }
    }

    private static bool ColunaExiste(SqliteConnection conexao, string tabela, string coluna)
    {
        using var verificar = conexao.CreateCommand();
        verificar.CommandText =
            $"SELECT COUNT(*) FROM pragma_table_info('{tabela}') WHERE name = '{coluna}'";
        return (long)verificar.ExecuteScalar()! != 0;
    }

    private static void AdicionarColunaSeFaltar(
        SqliteConnection conexao, string tabela, string coluna, string comandoAlter)
    {
        if (ColunaExiste(conexao, tabela, coluna))
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
