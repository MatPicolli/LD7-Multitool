using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Extração automática (melhor esforço) dos dados de um boleto em PDF.
///
/// Valor e vencimento vêm da linha digitável, que segue o padrão FEBRABAN e
/// por isso funciona com boleto de qualquer banco: nos últimos 14 dígitos,
/// os 4 primeiros são o "fator de vencimento" e os 10 restantes o valor em
/// centavos. Pagador, nosso número e número do documento (NF-e) usam
/// heurísticas de layout — se algum não for encontrado, o campo fica em
/// branco para o usuário completar.
/// </summary>
public static class LeitorBoletoPdf
{
    // Linha digitável da ficha de compensação (com ou sem pontos/espaços).
    private static readonly Regex LinhaDigitavelRegex = new(
        @"\d{5}\.?\d{5}\s*\d{5}\.?\d{6}\s*\d{5}\.?\d{6}\s*\d\s*(\d{14})");

    // Linha "nome do pagador + CNPJ/CPF" (o CNPJ do beneficiário costuma
    // aparecer sozinho na linha, então não é capturado aqui).
    private static readonly Regex PagadorRegex = new(
        @"^\s*(?<nome>[^\r\n]{4,70}?)\s+(\d{2}\.\d{3}\.\d{3}/\d{4}-\d{2}|\d{3}\.\d{3}\.\d{3}-\d{2})\s*$",
        RegexOptions.Multiline);

    // Linha da ficha de compensação: nº documento, espécie (DM etc.),
    // aceite (S/N), data de processamento e NOSSO NÚMERO no fim.
    // Atenção: o "nosso número" é o identificador único de cada boleto
    // (ex.: 1334-0) — não confundir com o código cooperativa/beneficiário
    // (ex.: 3069/3673030), que é igual em todos os boletos do emissor.
    private static readonly Regex FichaRegex = new(
        @"^\s*(?<doc>[\w./-]{1,15})\s+[A-Z]{1,3}\s+[SN]\s+\d{2}/\d{2}/\d{4}\s+(?<nossoNumero>[\d./-]{2,20})\s*$",
        RegexOptions.Multiline);

    // Fallback do nosso número: linha contendo apenas "número-dígito"
    // (ex.: 1334-0). Não colide com CEP (5+3 dígitos) nem com datas.
    private static readonly Regex NossoNumeroSoltoRegex = new(
        @"^\s*(\d{1,12}-\d)\s*$", RegexOptions.Multiline);

    // O fator de vencimento conta dias a partir de 07/10/1997; a numeração
    // reiniciou em 1000 no dia 22/02/2025 (circular FEBRABAN).
    private static readonly DateTime BaseFatorAntiga = new(1997, 10, 7);
    private static readonly DateTime InicioNovoCiclo = new(2025, 2, 22);

    /// <summary>
    /// Lê o PDF e monta um boleto com o que conseguir extrair.
    /// Nunca lança exceção: um PDF ilegível vira um boleto só com o nome
    /// do arquivo, para o usuário preencher manualmente.
    /// </summary>
    public static Boleto Ler(string caminho)
    {
        var boleto = new Boleto
        {
            Nome = Path.GetFileNameWithoutExtension(caminho),
            Validade = DateTime.Today,
            Estado = EstadoBoleto.Aberto,
            CaminhoArquivo = caminho,
        };

        string texto;
        try
        {
            using var documento = PdfDocument.Open(caminho);
            texto = string.Join("\n",
                documento.GetPages().Select(p => ContentOrderTextExtractor.GetText(p)));
        }
        catch
        {
            return boleto;
        }

        var linhaDigitavel = LinhaDigitavelRegex.Match(texto);
        if (linhaDigitavel.Success)
        {
            var campoLivre = linhaDigitavel.Groups[1].Value;
            var fator = int.Parse(campoLivre[..4]);
            var centavos = long.Parse(campoLivre[4..]);

            if (centavos > 0)
                boleto.Valor = centavos / 100m;

            if (fator >= 1000)
            {
                var vencimento = BaseFatorAntiga.AddDays(fator);
                if (vencimento < InicioNovoCiclo)
                    vencimento = InicioNovoCiclo.AddDays(fator - 1000);
                boleto.Validade = vencimento;
            }
        }

        var pagador = PagadorRegex.Match(texto);
        if (pagador.Success)
            boleto.Nome = pagador.Groups["nome"].Value.Trim();

        var ficha = FichaRegex.Match(texto);
        if (ficha.Success)
        {
            boleto.NfeReferente = ficha.Groups["doc"].Value;
            boleto.NossoNumero = ficha.Groups["nossoNumero"].Value;
        }
        else
        {
            var solto = NossoNumeroSoltoRegex.Match(texto);
            if (solto.Success)
                boleto.NossoNumero = solto.Groups[1].Value;
        }

        return boleto;
    }
}
