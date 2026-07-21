using System.Net;
using System.Net.Mail;

namespace LD7Multitool.Modulos.AutoEmail;

public static class EnvioEmailService
{
    /// <summary>
    /// Envia o e-mail de um cadastro para todos os destinatários,
    /// anexando os arquivos configurados.
    /// Lança exceção com mensagem amigável se algo impedir o envio.
    /// </summary>
    public static async Task EnviarAsync(ConfigSmtp config, CadastroEmail cadastro)
    {
        if (!config.Configurada)
            throw new InvalidOperationException(
                "Configure o servidor SMTP antes de enviar (botão \"Configurar SMTP\").");

        if (cadastro.Destinatarios.Count == 0)
            throw new InvalidOperationException("O cadastro não tem nenhum destinatário.");

        var arquivosFaltando = cadastro.Arquivos.Where(a => !File.Exists(a)).ToList();
        if (arquivosFaltando.Count > 0)
            throw new InvalidOperationException(
                "Arquivo(s) não encontrado(s):\n" + string.Join("\n", arquivosFaltando));

        using var mensagem = new MailMessage
        {
            From = new MailAddress(config.Remetente),
            Subject = cadastro.Assunto,
            Body = cadastro.Corpo,
        };
        foreach (var destinatario in cadastro.Destinatarios)
            mensagem.To.Add(destinatario);
        foreach (var arquivo in cadastro.Arquivos)
            mensagem.Attachments.Add(new Attachment(arquivo));

        using var cliente = new SmtpClient(config.Servidor, config.Porta)
        {
            EnableSsl = config.UsarSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };
        if (!string.IsNullOrWhiteSpace(config.Usuario))
            cliente.Credentials = new NetworkCredential(config.Usuario, config.Senha);

        await cliente.SendMailAsync(mensagem);
    }
}
