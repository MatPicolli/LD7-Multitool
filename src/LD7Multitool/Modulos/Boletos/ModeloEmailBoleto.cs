using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Modelo (template) editável do assunto e corpo do e-mail de envio de boleto.
/// Placeholders suportados: {nome}, {valor}, {vencimento}.
/// </summary>
public static class ModeloEmailBoleto
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    public const string ChaveAssunto = "boletos_assunto_modelo";
    public const string ChaveCorpo = "boletos_corpo_modelo";

    public const string AssuntoPadrao = "Boleto — {nome} (venc. {vencimento})";
    public const string CorpoPadrao =
        "Olá,\r\n\r\nSegue em anexo o boleto.\r\n\r\nAtenciosamente.";

    public const string Ajuda = "Placeholders: {nome}, {valor}, {vencimento}";

    public static string Assunto => ValorOuPadrao(ChaveAssunto, AssuntoPadrao);
    public static string Corpo => ValorOuPadrao(ChaveCorpo, CorpoPadrao);

    public static string AplicarAssunto(Boleto boleto) => Substituir(Assunto, boleto);
    public static string AplicarCorpo(Boleto boleto) => Substituir(Corpo, boleto);

    private static string Substituir(string modelo, Boleto boleto) =>
        modelo.Replace("{nome}", boleto.Nome)
              .Replace("{valor}", boleto.Valor.ToString("C2", CulturaBr))
              .Replace("{vencimento}", boleto.Validade.ToString("dd/MM/yyyy"));

    private static string ValorOuPadrao(string chave, string padrao)
    {
        var valor = ConfiguracaoRepository.Obter(chave);
        return string.IsNullOrEmpty(valor) ? padrao : valor;
    }
}
