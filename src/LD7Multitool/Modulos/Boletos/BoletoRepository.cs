using System.Globalization;
using LD7Multitool.Core;
using Microsoft.Data.Sqlite;

namespace LD7Multitool.Modulos.Boletos;

public static class BoletoRepository
{
    // Valor é gravado como texto em cultura invariante e a validade como ISO
    // (yyyy-MM-dd) para não depender da configuração regional da máquina.

    public static List<Boleto> Listar(EstadoBoleto? filtroEstado = null)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT id, nome, valor, validade, nosso_numero, nfe_referente, estado, caminho_arquivo, caminho_nfe
            FROM boletos
            """;
        if (filtroEstado is not null)
        {
            comando.CommandText += " WHERE estado = $estado";
            comando.Parameters.AddWithValue("$estado", (int)filtroEstado.Value);
        }
        comando.CommandText += " ORDER BY validade, nome";

        var boletos = new List<Boleto>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            boletos.Add(Ler(leitor));
        return boletos;
    }

    public static void Inserir(Boleto boleto)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO boletos (nome, valor, validade, nosso_numero, nfe_referente, estado, caminho_arquivo, caminho_nfe)
            VALUES ($nome, $valor, $validade, $nossoNumero, $nfeReferente, $estado, $caminhoArquivo, $caminhoNfe)
            RETURNING id
            """;
        PreencherParametros(comando, boleto);
        boleto.Id = (long)comando.ExecuteScalar()!;
    }

    public static void Atualizar(Boleto boleto)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            UPDATE boletos SET
                nome = $nome,
                valor = $valor,
                validade = $validade,
                nosso_numero = $nossoNumero,
                nfe_referente = $nfeReferente,
                estado = $estado,
                caminho_arquivo = $caminhoArquivo,
                caminho_nfe = $caminhoNfe
            WHERE id = $id
            """;
        PreencherParametros(comando, boleto);
        comando.Parameters.AddWithValue("$id", boleto.Id);
        comando.ExecuteNonQuery();
    }

    public static void Excluir(long id)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM boletos WHERE id = $id";
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    public static void AlterarEstado(long id, EstadoBoleto estado)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "UPDATE boletos SET estado = $estado WHERE id = $id";
        comando.Parameters.AddWithValue("$estado", (int)estado);
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    public static void AtualizarCaminhoNfe(long id, string caminho)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "UPDATE boletos SET caminho_nfe = $caminho WHERE id = $id";
        comando.Parameters.AddWithValue("$caminho", caminho);
        comando.Parameters.AddWithValue("$id", id);
        comando.ExecuteNonQuery();
    }

    private static void PreencherParametros(SqliteCommand comando, Boleto boleto)
    {
        comando.Parameters.AddWithValue("$nome", boleto.Nome);
        comando.Parameters.AddWithValue("$valor", boleto.Valor.ToString(CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$validade", boleto.Validade.ToString("yyyy-MM-dd"));
        comando.Parameters.AddWithValue("$nossoNumero", boleto.NossoNumero);
        comando.Parameters.AddWithValue("$nfeReferente", boleto.NfeReferente);
        comando.Parameters.AddWithValue("$estado", (int)boleto.Estado);
        comando.Parameters.AddWithValue("$caminhoArquivo", boleto.CaminhoArquivo);
        comando.Parameters.AddWithValue("$caminhoNfe", boleto.CaminhoNfe);
    }

    /// <summary>Caminhos de PDF já vinculados a algum boleto (para a importação não duplicar).</summary>
    public static HashSet<string> ListarCaminhosImportados()
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT caminho_arquivo FROM boletos WHERE caminho_arquivo <> ''";
        var caminhos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
            caminhos.Add(leitor.GetString(0));
        return caminhos;
    }

    private static Boleto Ler(SqliteDataReader leitor) => new()
    {
        Id = leitor.GetInt64(0),
        Nome = leitor.GetString(1),
        Valor = decimal.Parse(leitor.GetString(2), CultureInfo.InvariantCulture),
        Validade = DateTime.ParseExact(leitor.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture),
        NossoNumero = leitor.GetString(4),
        NfeReferente = leitor.GetString(5),
        Estado = (EstadoBoleto)leitor.GetInt32(6),
        CaminhoArquivo = leitor.GetString(7),
        CaminhoNfe = leitor.GetString(8),
    };
}
