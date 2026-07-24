using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace LD7Multitool.Modulos.Clientes;

public sealed record DadosCnpj(
    string RazaoSocial, string NomeFantasia,
    string Logradouro, string Numero, string Complemento, string Bairro,
    string Cep, string Municipio, string Uf,
    string Telefone, string Email);

/// <summary>Resultado da consulta: ou os dados, ou uma mensagem de erro legível.</summary>
public sealed record ResultadoCnpj(DadosCnpj? Dados, string? Erro)
{
    public bool Ok => Dados is not null;
    public static ResultadoCnpj ComErro(string erro) => new(null, erro);
    public static ResultadoCnpj ComDados(DadosCnpj dados) => new(dados, null);
}

/// <summary>
/// Consulta os dados de uma empresa pelo CNPJ. Tenta a BrasilAPI e, se falhar,
/// a ReceitaWS como reserva. Retorna a mensagem de erro real quando não dá certo.
/// </summary>
public static class ServicoCnpj
{
    private static readonly HttpClient Http = CriarHttp();

    private static HttpClient CriarHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        // Vários serviços/CDNs recusam requisições sem User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("LD7Multitool/1.0");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return http;
    }

    public static async Task<ResultadoCnpj> BuscarAsync(string cnpj)
    {
        var digitos = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digitos.Length != 14)
            return ResultadoCnpj.ComErro("Informe um CNPJ com 14 dígitos.");

        var (dadosBrasil, erroBrasil) = await TentarBrasilApi(digitos);
        if (dadosBrasil is not null)
            return ResultadoCnpj.ComDados(dadosBrasil);

        var (dadosReceita, erroReceita) = await TentarReceitaWs(digitos);
        if (dadosReceita is not null)
            return ResultadoCnpj.ComDados(dadosReceita);

        return ResultadoCnpj.ComErro($"{erroBrasil} (reserva: {erroReceita})");
    }

    private static async Task<(DadosCnpj?, string)> TentarBrasilApi(string digitos)
    {
        try
        {
            using var resposta = await Http.GetAsync($"https://brasilapi.com.br/api/cnpj/v1/{digitos}");
            if (resposta.StatusCode == HttpStatusCode.NotFound)
                return (null, "CNPJ não encontrado na BrasilAPI");
            if (!resposta.IsSuccessStatusCode)
                return (null, $"BrasilAPI HTTP {(int)resposta.StatusCode}");

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            return (new DadosCnpj(
                RazaoSocial: Texto(raiz, "razao_social"),
                NomeFantasia: Texto(raiz, "nome_fantasia"),
                Logradouro: Texto(raiz, "logradouro"),
                Numero: Texto(raiz, "numero"),
                Complemento: Texto(raiz, "complemento"),
                Bairro: Texto(raiz, "bairro"),
                Cep: Texto(raiz, "cep"),
                Municipio: Texto(raiz, "municipio"),
                Uf: Texto(raiz, "uf"),
                Telefone: FormatarTelefone(Texto(raiz, "ddd_telefone_1")),
                Email: Texto(raiz, "email")), "");
        }
        catch (Exception ex)
        {
            return (null, "BrasilAPI: " + ex.Message);
        }
    }

    private static async Task<(DadosCnpj?, string)> TentarReceitaWs(string digitos)
    {
        try
        {
            using var resposta = await Http.GetAsync($"https://receitaws.com.br/v1/cnpj/{digitos}");
            if (!resposta.IsSuccessStatusCode)
                return (null, $"ReceitaWS HTTP {(int)resposta.StatusCode}");

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            if (Texto(raiz, "status").Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                return (null, "ReceitaWS: " + Texto(raiz, "message"));

            return (new DadosCnpj(
                RazaoSocial: Texto(raiz, "nome"),
                NomeFantasia: Texto(raiz, "fantasia"),
                Logradouro: Texto(raiz, "logradouro"),
                Numero: Texto(raiz, "numero"),
                Complemento: Texto(raiz, "complemento"),
                Bairro: Texto(raiz, "bairro"),
                Cep: Texto(raiz, "cep"),
                Municipio: Texto(raiz, "municipio"),
                Uf: Texto(raiz, "uf"),
                Telefone: FormatarTelefone(Texto(raiz, "telefone")),
                Email: Texto(raiz, "email")), "");
        }
        catch (Exception ex)
        {
            return (null, "ReceitaWS: " + ex.Message);
        }
    }

    private static string Texto(JsonElement raiz, string propriedade)
    {
        if (!raiz.TryGetProperty(propriedade, out var valor) || valor.ValueKind == JsonValueKind.Null)
            return "";
        return valor.ValueKind == JsonValueKind.String ? valor.GetString() ?? "" : valor.ToString();
    }

    private static string FormatarTelefone(string telefone)
    {
        // ReceitaWS às vezes traz "(11) 1234-5678 / (11) 91234-5678" — pega o 1º.
        var primeiro = telefone.Split('/')[0];
        var d = new string(primeiro.Where(char.IsDigit).ToArray());
        return d.Length switch
        {
            10 => $"({d[..2]}) {d.Substring(2, 4)}-{d.Substring(6, 4)}",
            11 => $"({d[..2]}) {d.Substring(2, 5)}-{d.Substring(7, 4)}",
            _ => primeiro.Trim(),
        };
    }
}
