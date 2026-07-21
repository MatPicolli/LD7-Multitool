using System.Security.Cryptography;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Configuração do servidor SMTP usado para os envios.
/// A senha é protegida com DPAPI (vinculada ao usuário do Windows),
/// nunca gravada em texto puro no banco.
/// </summary>
public class ConfigSmtp
{
    public string Servidor { get; set; } = "";
    public int Porta { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = "";
    public string Senha { get; set; } = "";
    public string Remetente { get; set; } = "";

    /// <summary>Pasta onde ficam os PDFs das notas fiscais (DANFE Cliente-0000 (...).pdf).</summary>
    public string PastaNfe { get; set; } = "";

    /// <summary>Pasta onde ficam os PDFs dos boletos (BOLETO Cliente-0000 (...).pdf).</summary>
    public string PastaBoletos { get; set; } = "";

    public bool Configurada =>
        !string.IsNullOrWhiteSpace(Servidor) && !string.IsNullOrWhiteSpace(Remetente);

    public static ConfigSmtp Carregar()
    {
        return new ConfigSmtp
        {
            Servidor = ConfiguracaoRepository.Obter("smtp_servidor") ?? "",
            Porta = int.TryParse(ConfiguracaoRepository.Obter("smtp_porta"), out var porta) ? porta : 587,
            UsarSsl = ConfiguracaoRepository.Obter("smtp_ssl") != "0",
            Usuario = ConfiguracaoRepository.Obter("smtp_usuario") ?? "",
            Senha = DesprotegerSenha(ConfiguracaoRepository.Obter("smtp_senha")),
            Remetente = ConfiguracaoRepository.Obter("smtp_remetente") ?? "",
            PastaNfe = ConfiguracaoRepository.Obter("autoemail_pasta_nfe") ?? "",
            PastaBoletos = ConfiguracaoRepository.Obter("autoemail_pasta_boletos") ?? "",
        };
    }

    public void Gravar()
    {
        ConfiguracaoRepository.Definir("smtp_servidor", Servidor);
        ConfiguracaoRepository.Definir("smtp_porta", Porta.ToString());
        ConfiguracaoRepository.Definir("smtp_ssl", UsarSsl ? "1" : "0");
        ConfiguracaoRepository.Definir("smtp_usuario", Usuario);
        ConfiguracaoRepository.Definir("smtp_senha", ProtegerSenha(Senha));
        ConfiguracaoRepository.Definir("smtp_remetente", Remetente);
        ConfiguracaoRepository.Definir("autoemail_pasta_nfe", PastaNfe);
        ConfiguracaoRepository.Definir("autoemail_pasta_boletos", PastaBoletos);
    }

    private static string ProtegerSenha(string senha)
    {
        if (string.IsNullOrEmpty(senha))
            return "";
        var protegida = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(senha), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protegida);
    }

    private static string DesprotegerSenha(string? senhaProtegida)
    {
        if (string.IsNullOrEmpty(senhaProtegida))
            return "";
        try
        {
            var bytes = ProtectedData.Unprotect(
                Convert.FromBase64String(senhaProtegida), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            // Senha gravada por outro usuário/máquina — precisa ser redefinida.
            return "";
        }
        catch (FormatException)
        {
            return "";
        }
    }
}
