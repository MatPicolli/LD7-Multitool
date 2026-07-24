using System.Net.Http;
using System.Text.Json;

namespace LD7Multitool.Modulos.Clientes;

public sealed record DadosCnpj(
    string RazaoSocial, string NomeFantasia,
    string Logradouro, string Numero, string Complemento, string Bairro,
    string Cep, string Municipio, string Uf,
    string Telefone, string Email);

/// <summary>Consulta os dados de uma empresa pelo CNPJ usando a API pública BrasilAPI.</summary>
public static class ServicoCnpj
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>Retorna os dados da empresa, ou null se o CNPJ for inválido/não encontrado.</summary>
    public static async Task<DadosCnpj?> BuscarAsync(string cnpj)
    {
        var digitos = new string(cnpj.Where(char.IsDigit).ToArray());
        if (digitos.Length != 14)
            return null;

        try
        {
            using var resposta = await Http.GetAsync($"https://brasilapi.com.br/api/cnpj/v1/{digitos}");
            if (!resposta.IsSuccessStatusCode)
                return null;

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;

            return new DadosCnpj(
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
                Email: Texto(raiz, "email"));
        }
        catch
        {
            return null;
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
        var d = new string(telefone.Where(char.IsDigit).ToArray());
        return d.Length switch
        {
            10 => $"({d[..2]}) {d.Substring(2, 4)}-{d.Substring(6, 4)}",
            11 => $"({d[..2]}) {d.Substring(2, 5)}-{d.Substring(7, 4)}",
            _ => telefone,
        };
    }
}
