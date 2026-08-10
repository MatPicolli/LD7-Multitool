using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Coleta direto do portal, por HTTP, seguindo uma "receita" em JSON gravada no
/// próprio item (<see cref="Despesa.ConfigHttp"/>).
///
/// A receita existe porque cada portal (Semasa, Emasa, Celesc, PGMEI...) tem um
/// formulário diferente: em vez de embutir no programa um raspador por site —
/// que quebra na primeira mudança de layout — o endereço, os campos do
/// formulário e as expressões de extração ficam configuráveis, e ajustar um
/// portal vira editar um texto, sem recompilar.
///
/// Formato (todos os campos são opcionais, menos <c>url</c>):
/// <code>
/// {
///   "passos": [
///     { "metodo": "POST", "url": "https://portal/login",
///       "campos": { "usuario": "{login}", "senha": "{senha}" } },
///     { "metodo": "GET",  "url": "https://portal/segundavia?uc={identificador}" }
///   ],
///   "cabecalhos":       { "Referer": "https://portal/" },
///   "regexLinha":       "(\\d{5}\\.\\d{5} \\d{5}\\.\\d{6} \\d{5}\\.\\d{6} \\d \\d{14})",
///   "regexValor":       "Valor[^0-9]*([\\d.,]+)",
///   "regexVencimento":  "Vencimento[^0-9]*(\\d{2}/\\d{2}/\\d{4})"
/// }
/// </code>
/// Marcadores substituídos nos campos e na URL: <c>{login}</c>, <c>{senha}</c>,
/// <c>{identificador}</c>, <c>{documento}</c>, <c>{ano}</c>, <c>{mes}</c>.
///
/// Só o corpo da <b>última</b> resposta é analisado. Se a receita não trouxer
/// expressão nenhuma, o coletor procura sozinho por linhas digitáveis no HTML —
/// o que já resolve os portais que mostram a linha na tela.
/// </summary>
public class ColetorHttp : IColetorDespesa
{
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(45);

    public MetodoColeta Metodo => MetodoColeta.Http;

    public string? MotivoIndisponivel(Despesa despesa) =>
        despesa.ConfigHttp.Trim().Length == 0
            ? "Preencha a receita de consulta (JSON) na aba \"Coleta automática\" do item."
            : null;

    public async Task<IReadOnlyList<LancamentoDespesa>> ColetarAsync(
        Despesa despesa, CancellationToken cancelamento)
    {
        JsonDocument receita;
        try
        {
            receita = JsonDocument.Parse(despesa.ConfigHttp);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Receita de consulta inválida (JSON): " + ex.Message);
        }

        using (receita)
        {
            var raiz = receita.RootElement;
            var passos = raiz.TryGetProperty("passos", out var lista) && lista.ValueKind == JsonValueKind.Array
                ? lista.EnumerateArray().ToList()
                : new List<JsonElement> { raiz };
            if (passos.Count == 0)
                throw new InvalidOperationException("A receita não tem nenhum passo.");

            // CookieContainer próprio: portais que pedem login precisam manter a
            // sessão entre o passo do login e o da segunda via.
            using var manipulador = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AllowAutoRedirect = true,
            };
            using var http = new HttpClient(manipulador) { Timeout = Espera };
            // Vários portais recusam requisição sem User-Agent de navegador.
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) LD7Multitool/1.0");
            AplicarCabecalhos(http, raiz);

            var corpo = "";
            foreach (var passo in passos)
            {
                cancelamento.ThrowIfCancellationRequested();
                corpo = await ExecutarPassoAsync(http, passo, despesa, cancelamento);
            }

            return Extrair(corpo, raiz);
        }
    }

    private static void AplicarCabecalhos(HttpClient http, JsonElement raiz)
    {
        if (!raiz.TryGetProperty("cabecalhos", out var cabecalhos) ||
            cabecalhos.ValueKind != JsonValueKind.Object)
            return;

        foreach (var cabecalho in cabecalhos.EnumerateObject())
            http.DefaultRequestHeaders.TryAddWithoutValidation(cabecalho.Name, cabecalho.Value.ToString());
    }

    private static async Task<string> ExecutarPassoAsync(
        HttpClient http, JsonElement passo, Despesa despesa, CancellationToken cancelamento)
    {
        var url = Substituir(Texto(passo, "url"), despesa);
        if (url.Length == 0)
            throw new InvalidOperationException("Um dos passos da receita está sem \"url\".");

        var metodo = Texto(passo, "metodo").ToUpperInvariant();
        if (metodo.Length == 0)
            metodo = passo.TryGetProperty("campos", out _) ? "POST" : "GET";

        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), url);
        if (passo.TryGetProperty("campos", out var campos) && campos.ValueKind == JsonValueKind.Object)
        {
            var formulario = campos.EnumerateObject()
                .Select(c => new KeyValuePair<string, string>(c.Name, Substituir(c.Value.ToString(), despesa)));
            requisicao.Content = new FormUrlEncodedContent(formulario);
        }

        using var resposta = await http.SendAsync(requisicao, cancelamento);
        resposta.EnsureSuccessStatusCode();
        return await resposta.Content.ReadAsStringAsync(cancelamento);
    }

    /// <summary>Troca os marcadores da receita pelos dados cadastrados no item.</summary>
    private static string Substituir(string texto, Despesa despesa) => texto
        .Replace("{login}", despesa.Login)
        .Replace("{senha}", despesa.Senha)
        .Replace("{identificador}", despesa.Identificador)
        .Replace("{documento}", Febraban.SomenteDigitos(despesa.Documento))
        .Replace("{ano}", DateTime.Today.ToString("yyyy"))
        .Replace("{mes}", DateTime.Today.ToString("MM"));

    /// <summary>Monta os lançamentos a partir do corpo da última resposta.</summary>
    private static List<LancamentoDespesa> Extrair(string corpo, JsonElement raiz)
    {
        var lancamentos = new List<LancamentoDespesa>();
        var texto = SemMarcacao(corpo);

        // 1) Caminho preferido: linha digitável (traz valor e vencimento juntos).
        var regexLinha = Texto(raiz, "regexLinha") is { Length: > 0 } padrao
            ? new Regex(padrao, RegexOptions.IgnoreCase)
            : Febraban.LinhaDigitavelRegex;

        foreach (Match casamento in regexLinha.Matches(texto))
        {
            if (!Febraban.TentarLer(casamento.Value, out var linha, out var valor, out var vencimento))
                continue;
            if (lancamentos.Any(l => l.LinhaDigitavel == linha))
                continue;

            lancamentos.Add(new LancamentoDespesa
            {
                Vencimento = vencimento ?? DateTime.Today,
                Valor = valor,
                LinhaDigitavel = linha,
                Origem = OrigemLancamento.Portal,
                ChaveOrigem = "linha:" + linha,
            });
        }

        if (lancamentos.Count > 0)
            return lancamentos;

        // 2) Sem linha digitável: valor e vencimento pelas expressões da receita.
        var valorTexto = Capturar(texto, Texto(raiz, "regexValor"));
        var vencimentoTexto = Capturar(texto, Texto(raiz, "regexVencimento"));
        if (valorTexto.Length == 0 && vencimentoTexto.Length == 0)
            return lancamentos;

        var data = DateTime.TryParseExact(vencimentoTexto, new[] { "dd/MM/yyyy", "yyyy-MM-dd" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var lida) ? lida : DateTime.Today;
        decimal.TryParse(valorTexto, NumberStyles.Currency,
            CultureInfo.GetCultureInfo("pt-BR"), out var montante);

        lancamentos.Add(new LancamentoDespesa
        {
            Vencimento = data,
            Valor = montante,
            Origem = OrigemLancamento.Portal,
            // Sem linha digitável, a identidade do boleto é a data + o valor.
            ChaveOrigem = $"portal:{data:yyyy-MM-dd}:{montante.ToString(CultureInfo.InvariantCulture)}",
        });
        return lancamentos;
    }

    private static string Capturar(string texto, string padrao)
    {
        if (padrao.Length == 0)
            return "";
        var casamento = new Regex(padrao, RegexOptions.IgnoreCase).Match(texto);
        if (!casamento.Success)
            return "";
        return (casamento.Groups.Count > 1 ? casamento.Groups[1].Value : casamento.Value).Trim();
    }

    /// <summary>Tira as tags do HTML — as expressões trabalham sobre o texto visível.</summary>
    private static string SemMarcacao(string html) =>
        WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", " "));

    private static string Texto(JsonElement elemento, string propriedade) =>
        elemento.TryGetProperty(propriedade, out var valor) && valor.ValueKind == JsonValueKind.String
            ? valor.GetString() ?? ""
            : "";
}
