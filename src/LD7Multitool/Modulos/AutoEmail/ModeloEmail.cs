using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Modelo (template) editável do assunto e corpo dos e-mails do Auto-Email.
/// Placeholders suportados: {cliente}, {codigo}, {tipo}.
/// </summary>
public static class ModeloEmail
{
    public const string ChaveAssunto = "autoemail_assunto_modelo";
    public const string ChaveCorpo = "autoemail_corpo_modelo";

    public const string AssuntoPadrao = "{tipo} — {cliente}";
    public const string CorpoPadrao =
        "Olá,\r\n\r\nSegue(m) em anexo o(s) documento(s).\r\n\r\nAtenciosamente.";

    public const string Ajuda = "Placeholders: {cliente}, {codigo}, {tipo}";

    public static string Assunto => ValorOuPadrao(ChaveAssunto, AssuntoPadrao);
    public static string Corpo => ValorOuPadrao(ChaveCorpo, CorpoPadrao);

    public static string AplicarAssunto(string cliente, string codigo, string tipo) =>
        Substituir(Assunto, cliente, codigo, tipo);

    public static string AplicarCorpo(string cliente, string codigo, string tipo) =>
        Substituir(Corpo, cliente, codigo, tipo);

    private static string Substituir(string modelo, string cliente, string codigo, string tipo) =>
        modelo.Replace("{cliente}", cliente)
              .Replace("{codigo}", codigo)
              .Replace("{tipo}", tipo);

    private static string ValorOuPadrao(string chave, string padrao)
    {
        var valor = ConfiguracaoRepository.Obter(chave);
        return string.IsNullOrEmpty(valor) ? padrao : valor;
    }
}
