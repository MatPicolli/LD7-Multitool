namespace LD7Multitool.Modulos.Clientes;

/// <summary>Representante (vendedor/parceiro) que pode estar vinculado a um cliente.</summary>
public class Representante
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefone1 { get; set; } = "";
    public string Telefone2 { get; set; } = "";

    public override string ToString() => Nome;
}
