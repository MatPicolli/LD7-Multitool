namespace LD7Multitool.Modulos.Clientes;

public enum TipoCliente
{
    Fisica = 0,
    Juridica = 1,
}

/// <summary>Ficha cadastral de um cliente (pessoa física ou jurídica).</summary>
public class Cliente
{
    public long Id { get; set; }

    /// <summary>Código de 4 dígitos gerado na criação; não é editável depois.</summary>
    public string Codigo { get; set; } = "";

    public TipoCliente Tipo { get; set; } = TipoCliente.Juridica;
    public bool Ativo { get; set; } = true;

    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";

    /// <summary>Só preenchido quando Tipo = Física.</summary>
    public string Cpf { get; set; } = "";
    /// <summary>Só preenchido quando Tipo = Jurídica.</summary>
    public string Cnpj { get; set; } = "";
    public string Ie { get; set; } = "";
    public string InscricaoMunicipal { get; set; } = "";

    // Campos de pessoa física — sem sentido para Jurídica.
    public string Rg { get; set; } = "";
    public string EstadoCivil { get; set; } = "";
    public string Sexo { get; set; } = "";
    public DateTime? DataNascimento { get; set; }
    public string Nacionalidade { get; set; } = "";
    public string Naturalidade { get; set; } = "";

    public string Endereco { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Complemento { get; set; } = "";
    public string Cep { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Bairro { get; set; } = "";

    public string Email1 { get; set; } = "";
    public string Email2 { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string Celular { get; set; } = "";
    public string Site { get; set; } = "";

    public string Contato { get; set; } = "";
    public string ContatoEmail { get; set; } = "";
    public string ContatoTelefone { get; set; } = "";

    public long? RepresentanteId { get; set; }

    /// <summary>Preenchido pelo repositório ao listar, para exibição (não persistido).</summary>
    public string RepresentanteNome { get; set; } = "";

    /// <summary>CPF ou CNPJ, o que estiver preenchido conforme o tipo — para exibição.</summary>
    public string DocumentoExibicao => Tipo == TipoCliente.Fisica ? Cpf : Cnpj;
}
