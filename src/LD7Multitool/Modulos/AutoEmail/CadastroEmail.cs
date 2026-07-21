namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Um cadastro de envio: quem recebe (um ou mais e-mails), quais arquivos
/// vão anexados e o assunto/corpo da mensagem.
/// </summary>
public class CadastroEmail
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public string Assunto { get; set; } = "";
    public string Corpo { get; set; } = "";
    public List<string> Destinatarios { get; set; } = new();
    public List<string> Arquivos { get; set; } = new();
}
