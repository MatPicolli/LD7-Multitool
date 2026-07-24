using System.Net.Http;
using System.Text.Json;

namespace LD7Multitool.Modulos.Clientes;

public sealed record EnderecoCep(string Logradouro, string Bairro, string Cidade, string Uf, string Cep);

/// <summary>Consulta de endereço por CEP usando a API pública ViaCEP.</summary>
public static class ServicoCep
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    /// <summary>Retorna o endereço, ou null se o CEP for inválido/não encontrado.</summary>
    public static async Task<EnderecoCep?> BuscarAsync(string cep)
    {
        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length != 8)
            return null;

        try
        {
            using var resposta = await Http.GetAsync($"https://viacep.com.br/ws/{digitos}/json/");
            if (!resposta.IsSuccessStatusCode)
                return null;

            using var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = json.RootElement;
            if (raiz.TryGetProperty("erro", out _))
                return null;

            return new EnderecoCep(
                Logradouro: Texto(raiz, "logradouro"),
                Bairro: Texto(raiz, "bairro"),
                Cidade: Texto(raiz, "localidade"),
                Uf: Texto(raiz, "uf"),
                Cep: Texto(raiz, "cep"));
        }
        catch
        {
            return null;
        }
    }

    private static string Texto(JsonElement raiz, string propriedade) =>
        raiz.TryGetProperty(propriedade, out var valor) ? valor.GetString() ?? "" : "";
}
