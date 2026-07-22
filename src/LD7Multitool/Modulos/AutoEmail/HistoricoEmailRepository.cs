using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>Um registro do histórico de e-mails enviados.</summary>
public record EmailEnviado(DateTime EnviadoEm, string Para, string Assunto, int Anexos);

/// <summary>Registra e consulta o histórico de e-mails efetivamente enviados.</summary>
public static class HistoricoEmailRepository
{
    public static void Registrar(IEnumerable<string> destinatarios, string assunto, int qtdeAnexos)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            INSERT INTO historico_email (enviado_em, para, assunto, anexos)
            VALUES ($enviadoEm, $para, $assunto, $anexos)
            """;
        comando.Parameters.AddWithValue("$enviadoEm", DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
        comando.Parameters.AddWithValue("$para", string.Join("; ", destinatarios));
        comando.Parameters.AddWithValue("$assunto", assunto);
        comando.Parameters.AddWithValue("$anexos", qtdeAnexos);
        comando.ExecuteNonQuery();
    }

    public static List<EmailEnviado> ListarRecentes(int limite)
    {
        using var conexao = Database.AbrirConexao();
        using var comando = conexao.CreateCommand();
        comando.CommandText = """
            SELECT enviado_em, para, assunto, anexos
            FROM historico_email
            ORDER BY id DESC
            LIMIT $limite
            """;
        comando.Parameters.AddWithValue("$limite", limite);

        var registros = new List<EmailEnviado>();
        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            registros.Add(new EmailEnviado(
                DateTime.Parse(leitor.GetString(0), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind),
                leitor.GetString(1),
                leitor.GetString(2),
                leitor.GetInt32(3)));
        }
        return registros;
    }
}
