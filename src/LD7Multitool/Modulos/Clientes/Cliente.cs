namespace LD7Multitool.Modulos.Clientes;

/// <summary>Ficha cadastral de um cliente.</summary>
public class Cliente
{
    public long Id { get; set; }

    /// <summary>Código de 4 dígitos gerado na criação; não é editável depois.</summary>
    public string Codigo { get; set; } = "";

    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Ie { get; set; } = "";

    public string Endereco { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Bairro { get; set; } = "";

    public string Email1 { get; set; } = "";
    public string Email2 { get; set; } = "";
    public string Telefone1 { get; set; } = "";
    public string Telefone2 { get; set; } = "";

    public string Contato { get; set; } = "";
    public string ContatoEmail { get; set; } = "";
    public string ContatoTelefone { get; set; } = "";

    public long? RepresentanteId { get; set; }

    /// <summary>Preenchido pelo repositório ao listar, para exibição (não persistido).</summary>
    public string RepresentanteNome { get; set; } = "";
}
