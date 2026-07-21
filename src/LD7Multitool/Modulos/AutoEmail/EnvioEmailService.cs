using System.Net;
using System.Net.Mail;

namespace LD7Multitool.Modulos.AutoEmail;

public static class EnvioEmailService
{
    /// <summary>
    /// Envia um e-mail com anexos para os destinatários informados.
    /// Lança exceção com mensagem amigável se algo impedir o envio.
    /// </summary>
    public static async Task EnviarAsync(
        ConfigSmtp config,
        IReadOnlyCollection<string> destinatarios,
        string assunto,
        string corpo,
        IReadOnlyCollection<string> arquivos)
    {
        if (!config.Configurada)
            throw new InvalidOperationException(
                "Configure o servidor SMTP antes de enviar (botão de engrenagem ⚙).");

        if (destinatarios.Count == 0)
            throw new InvalidOperationException("O cadastro não tem nenhum destinatário.");

        var arquivosFaltando = arquivos.Where(a => !File.Exists(a)).ToList();
        if (arquivosFaltando.Count > 0)
            throw new InvalidOperationException(
                "Arquivo(s) não encontrado(s):\n" + string.Join("\n", arquivosFaltando));

        using var mensagem = new MailMessage
        {
            From = new MailAddress(config.Remetente),
            Subject = assunto,
            Body = corpo,
        };
        foreach (var destinatario in destinatarios)
            mensagem.To.Add(destinatario);
        foreach (var arquivo in arquivos)
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

    /// <summary>
    /// Testa a configuração enviando um e-mail de teste para o próprio
    /// remetente — valida servidor, porta, SSL e credenciais de uma vez.
    /// </summary>
    public static Task TestarConexaoAsync(ConfigSmtp config) =>
        EnviarAsync(
            config,
            new[] { config.Remetente },
            "Teste de conexão — LD7 Multitool",
            "Se você recebeu este e-mail, a configuração SMTP do LD7 Multitool está funcionando.",
            Array.Empty<string>());
}
