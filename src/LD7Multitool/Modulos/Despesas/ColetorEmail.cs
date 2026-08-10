using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Coleta pela caixa de entrada (IMAP).
///
/// É o coletor que cobre a maior parte do relatório de despesas: Generation,
/// Embratel, Claro internet, FGTS, previdência e as segundas vias da Vivo
/// chegam por e-mail. Cada item filtra as mensagens por trecho do remetente
/// e/ou do assunto, os anexos em PDF são salvos na pasta de downloads e lidos
/// pela linha digitável.
///
/// A conta de e-mail (servidor, usuário e senha) é única para o módulo e fica
/// no ⚙; a senha é gravada cifrada (ver <c>Core/Segredo.cs</c>). Em contas
/// Google/Microsoft com verificação em duas etapas é preciso usar uma
/// <b>senha de aplicativo</b> — a senha normal é recusada pelo IMAP.
/// </summary>
public class ColetorEmail : IColetorDespesa
{
    // Teto de mensagens lidas por item numa coleta: protege contra uma caixa de
    // entrada enorme com um filtro largo demais.
    private const int MaximoMensagens = 30;

    public MetodoColeta Metodo => MetodoColeta.Email;

    public string? MotivoIndisponivel(Despesa despesa)
    {
        var config = DespesasConfigForm.LerConfigImap();
        if (!config.Configurado)
            return "Configure a conta de e-mail (IMAP) no botão ⚙ do módulo.";
        if (despesa.EmailRemetente.Trim().Length == 0 && despesa.EmailAssunto.Trim().Length == 0)
            return "Preencha o remetente e/ou o assunto do e-mail no item.";
        if (DespesasConfigForm.PastaDownloads.Length == 0)
            return "Configure a pasta de downloads no ⚙ (é onde os anexos são salvos).";
        return null;
    }

    public async Task<IReadOnlyList<LancamentoDespesa>> ColetarAsync(
        Despesa despesa, CancellationToken cancelamento)
    {
        var config = DespesasConfigForm.LerConfigImap();
        var pastaAnexos = PastaDoItem(despesa);

        using var cliente = new ImapClient();
        await cliente.ConnectAsync(
            config.Servidor, config.Porta,
            config.UsarSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable,
            cancelamento);
        await cliente.AuthenticateAsync(config.Usuario, config.Senha, cancelamento);

        var caixa = cliente.Inbox;
        await caixa.OpenAsync(FolderAccess.ReadOnly, cancelamento);

        var uids = await caixa.SearchAsync(MontarBusca(despesa), cancelamento);
        var lancamentos = new List<LancamentoDespesa>();

        // Do mais recente para trás — o que interessa é a última conta.
        foreach (var uid in uids.Reverse().Take(MaximoMensagens))
        {
            cancelamento.ThrowIfCancellationRequested();
            var mensagem = await caixa.GetMessageAsync(uid, cancelamento);
            lancamentos.AddRange(await LerMensagemAsync(mensagem, uid, pastaAnexos, cancelamento));
        }

        await cliente.DisconnectAsync(true, cancelamento);

        // Devolve em ordem de vencimento para o serviço gravar do mais antigo ao mais novo.
        return lancamentos.OrderBy(l => l.Vencimento).ToList();
    }

    /// <summary>Filtro IMAP: período + trecho do remetente + trecho do assunto.</summary>
    private static SearchQuery MontarBusca(Despesa despesa)
    {
        var busca = SearchQuery.DeliveredAfter(DateTime.Today.AddDays(-DespesasConfigForm.DiasBusca));

        var remetente = despesa.EmailRemetente.Trim();
        if (remetente.Length > 0)
            busca = busca.And(SearchQuery.FromContains(remetente));

        var assunto = despesa.EmailAssunto.Trim();
        if (assunto.Length > 0)
            busca = busca.And(SearchQuery.SubjectContains(assunto));

        return busca;
    }

    private static async Task<List<LancamentoDespesa>> LerMensagemAsync(
        MimeMessage mensagem, UniqueId uid, string pastaAnexos, CancellationToken cancelamento)
    {
        var lancamentos = new List<LancamentoDespesa>();
        var recebido = mensagem.Date.LocalDateTime;

        foreach (var anexo in mensagem.Attachments.OfType<MimePart>())
        {
            var nome = anexo.FileName ?? "";
            if (!nome.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                continue;

            var caminho = await SalvarAnexoAsync(anexo, pastaAnexos, uid, nome, cancelamento);
            var paginas = LerPaginas(caminho);

            var achou = false;
            foreach (var pagina in paginas)
            {
                if (!Febraban.TentarLer(pagina, out var linha, out var valor, out var vencimento))
                    continue;

                achou = true;
                lancamentos.Add(new LancamentoDespesa
                {
                    Vencimento = vencimento ?? recebido.Date,
                    Valor = valor,
                    LinhaDigitavel = linha,
                    CaminhoArquivo = caminho,
                    Origem = OrigemLancamento.Email,
                    ChaveOrigem = "linha:" + linha,
                });
            }

            if (!achou)
            {
                lancamentos.Add(new LancamentoDespesa
                {
                    Vencimento = recebido.Date,
                    CaminhoArquivo = caminho,
                    Origem = OrigemLancamento.Email,
                    ChaveOrigem = $"email:{uid.Id}:{nome.ToLowerInvariant()}",
                });
            }
        }

        return lancamentos;
    }

    private static async Task<string> SalvarAnexoAsync(
        MimePart anexo, string pasta, UniqueId uid, string nome, CancellationToken cancelamento)
    {
        Directory.CreateDirectory(pasta);
        // Prefixo com o UID da mensagem: dois meses com o mesmo nome de anexo
        // ("boleto.pdf") não se sobrescrevem.
        var seguro = string.Concat(nome.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var caminho = Path.Combine(pasta, $"{uid.Id}-{seguro}");

        if (!File.Exists(caminho))
        {
            await using var fluxo = File.Create(caminho);
            await anexo.Content.DecodeToAsync(fluxo, cancelamento);
        }
        return caminho;
    }

    private static List<string> LerPaginas(string caminho)
    {
        try
        {
            using var documento = PdfDocument.Open(caminho);
            return documento.GetPages().Select(ContentOrderTextExtractor.GetText).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>Subpasta dos anexos do item, dentro da pasta de downloads.</summary>
    private static string PastaDoItem(Despesa despesa)
    {
        var nome = string.Concat(despesa.Nome.Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)).Trim();
        return Path.Combine(DespesasConfigForm.PastaDownloads, "Despesas", nome);
    }
}
