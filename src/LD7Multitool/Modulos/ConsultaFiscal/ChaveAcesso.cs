namespace LD7Multitool.Modulos.ConsultaFiscal;

public enum ModeloDocumento
{
    NFe,   // modelo 55
    NFCe,  // modelo 65
    CTe,   // modelo 57 ou 67
    Desconhecido,
}

/// <summary>
/// Chave de acesso de um documento fiscal eletrônico (44 dígitos).
/// Layout: cUF(2) AAMM(4) CNPJ(14) mod(2) série(3) nNF(9) tpEmis(1) cNF(8) cDV(1).
/// </summary>
public sealed class ChaveAcesso
{
    public string Digitos { get; }

    private ChaveAcesso(string digitos) => Digitos = digitos;

    public string CodigoUf => Digitos.Substring(0, 2);
    public string CodigoModelo => Digitos.Substring(20, 2);

    public ModeloDocumento Modelo => CodigoModelo switch
    {
        "55" => ModeloDocumento.NFe,
        "65" => ModeloDocumento.NFCe,
        "57" or "67" => ModeloDocumento.CTe,
        _ => ModeloDocumento.Desconhecido,
    };

    public string TipoDescricao => Modelo switch
    {
        ModeloDocumento.NFe => "NF-e",
        ModeloDocumento.NFCe => "NFC-e",
        ModeloDocumento.CTe => "CT-e",
        _ => "Documento",
    };

    public string UfSigla => UfsPorCodigo.GetValueOrDefault(CodigoUf, "??");

    /// <summary>Chave em grupos de 4 dígitos, como no rodapé do DANFE.</summary>
    public string Formatada =>
        string.Join(" ", Enumerable.Range(0, 11).Select(i => Digitos.Substring(i * 4, 4)));

    /// <summary>
    /// Extrai uma chave de 44 dígitos de uma entrada qualquer (bipada ou digitada),
    /// ignorando prefixos/espaços não numéricos.
    /// </summary>
    public static bool TryParse(string entrada, out ChaveAcesso chave)
    {
        chave = null!;
        if (string.IsNullOrWhiteSpace(entrada))
            return false;

        var digitos = new string(entrada.Where(char.IsDigit).ToArray());
        if (digitos.Length < 44)
            return false;
        if (digitos.Length > 44)
            digitos = digitos[..44]; // ex.: leitor que acrescenta algo depois

        chave = new ChaveAcesso(digitos);
        return chave.Modelo != ModeloDocumento.Desconhecido;
    }

    private static readonly Dictionary<string, string> UfsPorCodigo = new()
    {
        ["11"] = "RO", ["12"] = "AC", ["13"] = "AM", ["14"] = "RR", ["15"] = "PA",
        ["16"] = "AP", ["17"] = "TO", ["21"] = "MA", ["22"] = "PI", ["23"] = "CE",
        ["24"] = "RN", ["25"] = "PB", ["26"] = "PE", ["27"] = "AL", ["28"] = "SE",
        ["29"] = "BA", ["31"] = "MG", ["32"] = "ES", ["33"] = "RJ", ["35"] = "SP",
        ["41"] = "PR", ["42"] = "SC", ["43"] = "RS", ["50"] = "MS", ["51"] = "MT",
        ["52"] = "GO", ["53"] = "DF",
    };
}
