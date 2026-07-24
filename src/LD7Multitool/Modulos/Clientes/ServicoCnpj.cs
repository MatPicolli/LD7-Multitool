using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Conjunto de campos de uma empresa retornados por uma fonte de CNPJ.</summary>
public sealed record DadosCnpj
{
    public string RazaoSocial { get; init; } = "";
    public string NomeFantasia { get; init; } = "";
    public string Situacao { get; init; } = "";
    public string DataAbertura { get; init; } = "";
    public string NaturezaJuridica { get; init; } = "";
    public string CnaePrincipal { get; init; } = "";
    public string Porte { get; init; } = "";
    public string CapitalSocial { get; init; } = "";
    public string Logradouro { get; init; } = "";
    public string Numero { get; init; } = "";
    public string Complemento { get; init; } = "";
    public string Bairro { get; init; } = "";
    public string Cep { get; init; } = "";
    public string Municipio { get; init; } = "";
    public string Uf { get; init; } = "";
    public string Telefone { get; init; } = "";
    public string Email { get; init; } = "";
}

/// <summary>Resposta de uma fonte: os dados (ou o erro) e quanto tempo levou.</summary>
public sealed record RespostaFonte(string Fonte, DadosCnpj? Dados, string? Erro, long TempoMs);

/// <summary>Um campo já consolidado entre as fontes (valor da maioria + concordância).</summary>
public sealed record CampoConsolidado(string Rotulo, string Valor, int Concordam, int Total);

/// <summary>
/// Consulta os dados de uma empresa pelo CNPJ em <b>várias fontes públicas ao
/// mesmo tempo</b> (BrasilAPI, ReceitaWS, CNPJ.ws e Minha Receita), medindo o
/// tempo de cada uma e consolidando os campos pelo voto da maioria.
/// </summary>
public static class ServicoCnpj
{
    private static readonly HttpClient Http = CriarHttp();
    private static readonly NumberFormatInfo MoedaBr = new() { NumberGroupSeparator = ".", NumberDecimalSeparator = "," };

    /// <summary>Campos exibidos na comparação, na ordem, com o seletor de cada um.</summary>
    public static readonly (string Rotulo, Func<DadosCnpj, string> Valor)[] Campos =
    {
        ("Razão social", d => d.RazaoSocial),
        ("Nome fantasia", d => d.NomeFantasia),
        ("Situação", d => d.Situacao),
        ("Abertura", d => d.DataAbertura),
        ("Natureza jurídica", d => d.NaturezaJuridica),
        ("CNAE principal", d => d.CnaePrincipal),
        ("Porte", d => d.Porte),
        ("Capital social", d => d.CapitalSocial),
        ("Logradouro", d => d.Logradouro),
        ("Número", d => d.Numero),
        ("Complemento", d => d.Complemento),
        ("Bairro", d => d.Bairro),
        ("CEP", d => d.Cep),
        ("Município", d => d.Municipio),
        ("UF", d => d.Uf),
        ("Telefone", d => d.Telefone),
        ("E-mail", d => d.Email),
    };

    private static HttpClient CriarHttp()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        // Vários serviços/CDNs recusam requisições sem User-Agent.
        http.DefaultRequestHeaders.UserAgent.ParseAdd("LD7Multitool/1.0");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return http;
    }

    /// <summary>Consulta todas as fontes em paralelo e devolve a resposta de cada uma.</summary>
    public static async Task<IReadOnlyList<RespostaFonte>> ConsultarAsync(string cnpj)
    {
        var digitos = SomenteDigitos(cnpj);
        if (digitos.Length != 14)
            return new[] { new RespostaFonte("(validação)", null, "CNPJ deve ter 14 dígitos.", 0) };

        var tarefas = new[]
        {
            Medir("BrasilAPI", () => BrasilApi(digitos)),
            Medir("ReceitaWS", () => ReceitaWs(digitos)),
            Medir("CNPJ.ws", () => CnpjWs(digitos)),
            Medir("Minha Receita", () => MinhaReceita(digitos)),
        };
        return await Task.WhenAll(tarefas);
    }

    private static async Task<RespostaFonte> Medir(string fonte, Func<Task<DadosCnpj?>> buscar)
    {
        var cronometro = Stopwatch.StartNew();
        try
        {
            var dados = await buscar();
            cronometro.Stop();
            return dados is null
                ? new RespostaFonte(fonte, null, "CNPJ não encontrado", cronometro.ElapsedMilliseconds)
                : new RespostaFonte(fonte, dados, null, cronometro.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            cronometro.Stop();
            return new RespostaFonte(fonte, null, ex.Message, cronometro.ElapsedMilliseconds);
        }
    }

    // --- Consolidação -------------------------------------------------------

    /// <summary>Para cada campo, o valor mais frequente entre as fontes e a concordância.</summary>
    public static List<CampoConsolidado> Consolidar(IReadOnlyList<DadosCnpj> fontes)
    {
        var resultado = new List<CampoConsolidado>();
        foreach (var (rotulo, seletor) in Campos)
        {
            var (valor, concordam, total) = Maioria(fontes.Select(seletor));
            resultado.Add(new CampoConsolidado(rotulo, valor, concordam, total));
        }
        return resultado;
    }

    /// <summary>Monta um <see cref="DadosCnpj"/> com o valor da maioria em cada campo.</summary>
    public static DadosCnpj DadosConsolidados(IReadOnlyList<DadosCnpj> fontes) => new()
    {
        RazaoSocial = Maioria(fontes.Select(d => d.RazaoSocial)).Valor,
        NomeFantasia = Maioria(fontes.Select(d => d.NomeFantasia)).Valor,
        Situacao = Maioria(fontes.Select(d => d.Situacao)).Valor,
        DataAbertura = Maioria(fontes.Select(d => d.DataAbertura)).Valor,
        NaturezaJuridica = Maioria(fontes.Select(d => d.NaturezaJuridica)).Valor,
        CnaePrincipal = Maioria(fontes.Select(d => d.CnaePrincipal)).Valor,
        Porte = Maioria(fontes.Select(d => d.Porte)).Valor,
        CapitalSocial = Maioria(fontes.Select(d => d.CapitalSocial)).Valor,
        Logradouro = Maioria(fontes.Select(d => d.Logradouro)).Valor,
        Numero = Maioria(fontes.Select(d => d.Numero)).Valor,
        Complemento = Maioria(fontes.Select(d => d.Complemento)).Valor,
        Bairro = Maioria(fontes.Select(d => d.Bairro)).Valor,
        Cep = Maioria(fontes.Select(d => d.Cep)).Valor,
        Municipio = Maioria(fontes.Select(d => d.Municipio)).Valor,
        Uf = Maioria(fontes.Select(d => d.Uf)).Valor,
        Telefone = Maioria(fontes.Select(d => d.Telefone)).Valor,
        Email = Maioria(fontes.Select(d => d.Email)).Valor,
    };

    private static (string Valor, int Concordam, int Total) Maioria(IEnumerable<string> valores)
    {
        var lista = valores.Select(v => (v ?? "").Trim()).Where(v => v.Length > 0).ToList();
        if (lista.Count == 0)
            return ("", 0, 0);
        var grupo = lista.GroupBy(Normalizar).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First();
        return (grupo.First(), grupo.Count(), lista.Count);
    }

    /// <summary>Normaliza um valor para comparar semelhança (maiúsculas, sem acento, espaços colapsados).</summary>
    public static string Normalizar(string texto)
    {
        var semAcento = new string(texto.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray());
        var construtor = new StringBuilder(semAcento.Length);
        var espacoPendente = false;
        foreach (var c in semAcento.Trim())
        {
            if (char.IsWhiteSpace(c)) { espacoPendente = true; continue; }
            if (espacoPendente && construtor.Length > 0) construtor.Append(' ');
            espacoPendente = false;
            construtor.Append(char.ToUpperInvariant(c));
        }
        return construtor.ToString();
    }

    // --- Fontes -------------------------------------------------------------

    private static async Task<DadosCnpj?> BrasilApi(string digitos)
    {
        using var resposta = await Http.GetAsync($"https://brasilapi.com.br/api/cnpj/v1/{digitos}");
        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;
        resposta.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return LerFormatoReceitaFederal(doc.RootElement);
    }

    private static async Task<DadosCnpj?> MinhaReceita(string digitos)
    {
        using var resposta = await Http.GetAsync($"https://minhareceita.org/{digitos}");
        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;
        resposta.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        return LerFormatoReceitaFederal(doc.RootElement);
    }

    /// <summary>Formato compartilhado por BrasilAPI e Minha Receita (dump da Receita Federal).</summary>
    private static DadosCnpj LerFormatoReceitaFederal(JsonElement r) => new()
    {
        RazaoSocial = Str(r, "razao_social"),
        NomeFantasia = Str(r, "nome_fantasia"),
        Situacao = Str(r, "descricao_situacao_cadastral"),
        DataAbertura = FormatarData(Str(r, "data_inicio_atividade")),
        NaturezaJuridica = Str(r, "natureza_juridica"),
        CnaePrincipal = Cnae(Str(r, "cnae_fiscal"), Str(r, "cnae_fiscal_descricao")),
        Porte = Str(r, "porte") is var p && p.Length > 0 ? p : Str(r, "descricao_porte"),
        CapitalSocial = FormatarMoeda(Str(r, "capital_social")),
        Logradouro = Str(r, "logradouro"),
        Numero = Str(r, "numero"),
        Complemento = Str(r, "complemento"),
        Bairro = Str(r, "bairro"),
        Cep = SomenteDigitos(Str(r, "cep")),
        Municipio = Str(r, "municipio"),
        Uf = Str(r, "uf"),
        Telefone = FormatarTelefone(Str(r, "ddd_telefone_1")),
        Email = Str(r, "email"),
    };

    private static async Task<DadosCnpj?> ReceitaWs(string digitos)
    {
        using var resposta = await Http.GetAsync($"https://receitaws.com.br/v1/cnpj/{digitos}");
        resposta.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var r = doc.RootElement;

        if (Str(r, "status").Equals("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            var mensagem = Str(r, "message");
            if (mensagem.Contains("não encontrado", StringComparison.OrdinalIgnoreCase) ||
                mensagem.Contains("rejeitado", StringComparison.OrdinalIgnoreCase))
                return null;
            throw new InvalidOperationException(mensagem.Length > 0 ? mensagem : "erro na ReceitaWS");
        }

        var cnae = "";
        if (r.TryGetProperty("atividade_principal", out var atividades) &&
            atividades.ValueKind == JsonValueKind.Array && atividades.GetArrayLength() > 0)
            cnae = Cnae(Str(atividades[0], "code"), Str(atividades[0], "text"));

        return new DadosCnpj
        {
            RazaoSocial = Str(r, "nome"),
            NomeFantasia = Str(r, "fantasia"),
            Situacao = Str(r, "situacao"),
            DataAbertura = Str(r, "abertura"),
            NaturezaJuridica = Str(r, "natureza_juridica"),
            CnaePrincipal = cnae,
            Porte = Str(r, "porte"),
            CapitalSocial = FormatarMoeda(Str(r, "capital_social")),
            Logradouro = Str(r, "logradouro"),
            Numero = Str(r, "numero"),
            Complemento = Str(r, "complemento"),
            Bairro = Str(r, "bairro"),
            Cep = SomenteDigitos(Str(r, "cep")),
            Municipio = Str(r, "municipio"),
            Uf = Str(r, "uf"),
            Telefone = FormatarTelefone(Str(r, "telefone")),
            Email = Str(r, "email"),
        };
    }

    private static async Task<DadosCnpj?> CnpjWs(string digitos)
    {
        using var resposta = await Http.GetAsync($"https://publica.cnpj.ws/cnpj/{digitos}");
        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;
        resposta.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var r = doc.RootElement;

        var temEstab = Obj(r, "estabelecimento", out var estab);
        var cnae = "";
        if (temEstab && Obj(estab, "atividade_principal", out var ap))
            cnae = Cnae(Str(ap, "id"), Str(ap, "descricao"));

        return new DadosCnpj
        {
            RazaoSocial = Str(r, "razao_social"),
            NomeFantasia = temEstab ? Str(estab, "nome_fantasia") : "",
            Situacao = temEstab ? Str(estab, "situacao_cadastral") : "",
            DataAbertura = temEstab ? FormatarData(Str(estab, "data_inicio_atividade")) : "",
            NaturezaJuridica = Obj(r, "natureza_juridica", out var nj) ? Str(nj, "descricao") : "",
            CnaePrincipal = cnae,
            Porte = Obj(r, "porte", out var porte) ? Str(porte, "descricao") : "",
            CapitalSocial = FormatarMoeda(Str(r, "capital_social")),
            Logradouro = temEstab ? Str(estab, "logradouro") : "",
            Numero = temEstab ? Str(estab, "numero") : "",
            Complemento = temEstab ? Str(estab, "complemento") : "",
            Bairro = temEstab ? Str(estab, "bairro") : "",
            Cep = temEstab ? SomenteDigitos(Str(estab, "cep")) : "",
            Municipio = temEstab && Obj(estab, "cidade", out var cidade) ? Str(cidade, "nome") : "",
            Uf = temEstab && Obj(estab, "estado", out var estado) ? Str(estado, "sigla") : "",
            Telefone = temEstab ? FormatarTelefone(Str(estab, "ddd1") + Str(estab, "telefone1")) : "",
            Email = temEstab ? Str(estab, "email") : "",
        };
    }

    // --- Utilidades ---------------------------------------------------------

    private static string Str(JsonElement elemento, string propriedade)
    {
        if (!elemento.TryGetProperty(propriedade, out var valor) || valor.ValueKind == JsonValueKind.Null)
            return "";
        return valor.ValueKind == JsonValueKind.String ? valor.GetString() ?? "" : valor.ToString();
    }

    private static bool Obj(JsonElement elemento, string propriedade, out JsonElement filho)
    {
        if (elemento.TryGetProperty(propriedade, out filho) && filho.ValueKind == JsonValueKind.Object)
            return true;
        filho = default;
        return false;
    }

    private static string Cnae(string codigo, string descricao)
    {
        codigo = codigo.Trim();
        descricao = descricao.Trim();
        if (codigo.Length == 0) return descricao;
        if (descricao.Length == 0) return codigo;
        return $"{codigo} - {descricao}";
    }

    private static string FormatarData(string valor)
    {
        var texto = valor.Length >= 10 ? valor[..10] : valor;
        return DateTime.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var data)
            ? data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : valor;
    }

    private static string FormatarMoeda(string valor)
    {
        if (valor.Trim().Length == 0)
            return "";
        return decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var montante)
            ? "R$ " + montante.ToString("#,##0.00", MoedaBr)
            : valor;
    }

    private static string FormatarTelefone(string telefone)
    {
        var primeiro = telefone.Split('/')[0];
        var d = new string(primeiro.Where(char.IsDigit).ToArray());
        return d.Length switch
        {
            10 => $"({d[..2]}) {d.Substring(2, 4)}-{d.Substring(6, 4)}",
            11 => $"({d[..2]}) {d.Substring(2, 5)}-{d.Substring(7, 4)}",
            _ => primeiro.Trim(),
        };
    }

    private static string SomenteDigitos(string texto) =>
        new(texto.Where(char.IsDigit).ToArray());
}
