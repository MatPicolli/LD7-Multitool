namespace LD7Multitool.Core;

/// <summary>
/// Categorias (grupos) do menu lateral. Cada módulo diz a qual pertence em
/// <see cref="IModulo.Categoria"/> — use sempre estas constantes, para não
/// criar um grupo novo por erro de digitação.
/// </summary>
public static class Categorias
{
    /// <summary>Ferramentas da loja (e-mails, boletos, despesas, notas).</summary>
    public const string LojaoDa7 = "Lojão da 7";

    /// <summary>Cadastros da MILITARI (clientes/representantes).</summary>
    public const string Militari = "MILITARI";
}
