using System.Text;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Formatação (máscara) de CPF e CNPJ a partir do texto digitado.</summary>
public static class Mascaras
{
    /// <summary>Ex.: "123.456.789-00" (parcial enquanto digita).</summary>
    public static string Cpf(string texto) => Formatar(texto, 11, new[] { (3, '.'), (6, '.'), (9, '-') });

    /// <summary>Ex.: "12.345.678/0001-90" (parcial enquanto digita).</summary>
    public static string Cnpj(string texto) => Formatar(texto, 14, new[] { (2, '.'), (5, '.'), (8, '/'), (12, '-') });

    private static string Formatar(string texto, int maxDigitos, (int Posicao, char Separador)[] separadores)
    {
        var digitos = new string(texto.Where(char.IsDigit).ToArray());
        if (digitos.Length > maxDigitos)
            digitos = digitos[..maxDigitos];

        var construtor = new StringBuilder(digitos.Length + separadores.Length);
        for (var i = 0; i < digitos.Length; i++)
        {
            foreach (var (posicao, separador) in separadores)
            {
                if (i == posicao)
                    construtor.Append(separador);
            }
            construtor.Append(digitos[i]);
        }
        return construtor.ToString();
    }
}
