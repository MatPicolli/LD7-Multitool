using System.Security.Cryptography;
using System.Text;

namespace LD7Multitool.Core;

/// <summary>
/// Proteção dos segredos gravados no dados.db (senhas dos portais de despesa).
///
/// Usa a DPAPI do Windows no escopo do <b>usuário atual</b>: a senha só volta a
/// ser legível na mesma conta do Windows que a gravou. Isso é de propósito —
/// como o dados.db é portátil (basta copiar a pasta), gravar as senhas em texto
/// puro significaria distribuir as senhas junto. O preço é que, ao levar o
/// banco para outra máquina/usuário, as senhas precisam ser digitadas de novo:
/// <see cref="Revelar"/> devolve string vazia em vez de estourar.
/// </summary>
public static class Segredo
{
    // Marca o texto como protegido — permite reconhecer (e reaproveitar) um
    // valor que ainda tenha sido gravado em texto puro por versão anterior.
    private const string Prefixo = "dpapi:";

    /// <summary>Cifra um segredo para gravação. Texto vazio continua vazio.</summary>
    public static string Proteger(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "";
        try
        {
            var cifrado = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(texto), null, DataProtectionScope.CurrentUser);
            return Prefixo + Convert.ToBase64String(cifrado);
        }
        catch (CryptographicException)
        {
            // Sem DPAPI disponível preferimos não gravar a senha a gravá-la aberta.
            return "";
        }
    }

    /// <summary>Decifra um segredo gravado. Devolve "" se não for possível.</summary>
    public static string Revelar(string armazenado)
    {
        if (string.IsNullOrEmpty(armazenado))
            return "";
        if (!armazenado.StartsWith(Prefixo, StringComparison.Ordinal))
            return armazenado; // gravado antes desta proteção existir
        try
        {
            var cifrado = Convert.FromBase64String(armazenado[Prefixo.Length..]);
            var aberto = ProtectedData.Unprotect(cifrado, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(aberto);
        }
        catch (Exception e) when (e is CryptographicException or FormatException)
        {
            // Banco copiado de outra máquina/usuário: a senha precisa ser redigitada.
            return "";
        }
    }
}
