namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Um cliente cadastrado para envio: código (usado para localizar os PDFs
/// de NF-e/boleto nas pastas configuradas), nome e um ou mais e-mails.
/// </summary>
public class CadastroEmail
{
    public long Id { get; set; }

    /// <summary>Código do cliente (ex.: 5551) — bate com o "Cliente-5551" dos PDFs.</summary>
    public string Codigo { get; set; } = "";

    public string Nome { get; set; } = "";

    public List<string> Destinatarios { get; set; } = new();
}
