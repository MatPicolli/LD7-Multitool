using System.Globalization;
using System.Net.Mail;
using LD7Multitool.Core;
using LD7Multitool.Modulos.AutoEmail;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Envia o PDF de um boleto por e-mail. Os destinatários podem ser escolhidos
/// entre os e-mails já cadastrados (módulo Auto-Email) ou digitados à mão.
/// </summary>
public class EnviarBoletoForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly Boleto _boleto;
    private readonly ConfigSmtp _config;

    private readonly ComboBox _cadastrados;
    private readonly TextBox _campoManual;
    private readonly ListBox _destinatarios;
    private readonly TextBox _campoAssunto;
    private readonly TextBox _campoCorpo;
    private readonly Button _botaoEnviar;

    public EnviarBoletoForm(Boleto boleto)
    {
        _boleto = boleto;
        _config = ConfigSmtp.Carregar();

        Text = "Enviar boleto por e-mail";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 560);
        ClientSize = new Size(620, 560);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // boleto
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // anexo
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // cadastrado
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // manual
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // lista
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // assunto
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // mensagem

        AdicionarLinha(tabela, 0, "Boleto:", new Label
        {
            Text = $"{_boleto.Nome} — {_boleto.Valor.ToString("C2", CulturaBr)}, " +
                   $"venc. {_boleto.Validade:dd/MM/yyyy}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.75f),
        });

        var temPdf = _boleto.CaminhoArquivo.Length > 0 && File.Exists(_boleto.CaminhoArquivo);
        AdicionarLinha(tabela, 1, "Anexo:", new Label
        {
            Text = temPdf
                ? Path.GetFileName(_boleto.CaminhoArquivo)
                : "(sem PDF vinculado — o e-mail irá sem anexo)",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = temPdf ? Estilo.CorTexto : Estilo.CorPerigo,
            AutoEllipsis = true,
        });

        // --- Escolher entre cadastrados ------------------------------------
        _cadastrados = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
        };
        CarregarCadastrados();
        var botaoAddCadastrado = Estilo.BotaoPadrao("Adicionar");
        botaoAddCadastrado.Click += (_, _) => AdicionarCadastrado();
        AdicionarLinha(tabela, 2, "Cadastrado:", ComporCampoBotao(_cadastrados, botaoAddCadastrado));

        // --- E-mail manual -------------------------------------------------
        _campoManual = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "outro@email.com" };
        _campoManual.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { AdicionarManual(); e.SuppressKeyPress = true; }
        };
        var botaoAddManual = Estilo.BotaoPadrao("Adicionar");
        botaoAddManual.Click += (_, _) => AdicionarManual();
        AdicionarLinha(tabela, 3, "Manual:", ComporCampoBotao(_campoManual, botaoAddManual));

        // --- Lista de destinatários ----------------------------------------
        _destinatarios = new ListBox { Dock = DockStyle.Fill };
        var botaoRemover = Estilo.BotaoPadrao("Remover selecionado");
        botaoRemover.Click += (_, _) =>
        {
            if (_destinatarios.SelectedIndex >= 0)
                _destinatarios.Items.RemoveAt(_destinatarios.SelectedIndex);
        };
        var painelLista = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        painelLista.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painelLista.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        painelLista.Controls.Add(_destinatarios, 0, 0);
        var barraRemover = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        barraRemover.Controls.Add(botaoRemover);
        painelLista.Controls.Add(barraRemover, 0, 1);
        AdicionarLinha(tabela, 4, "Enviar para:", painelLista);

        // --- Assunto e mensagem --------------------------------------------
        _campoAssunto = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = $"Boleto — {_boleto.Nome} (venc. {_boleto.Validade:dd/MM/yyyy})",
        };
        AdicionarLinha(tabela, 5, "Assunto:", _campoAssunto);

        _campoCorpo = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = "Olá,\r\n\r\nSegue em anexo o boleto.\r\n\r\nAtenciosamente.",
        };
        AdicionarLinha(tabela, 6, "Mensagem:", _campoCorpo);

        // --- Botões --------------------------------------------------------
        _botaoEnviar = Estilo.BotaoPrimario("Enviar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        _botaoEnviar.Click += async (_, _) => await EnviarAsync();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(_botaoEnviar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(tabela);
        Controls.Add(painelBotoes);
        CancelButton = botaoCancelar;
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 6, 0, 0),
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private static Control ComporCampoBotao(Control campo, Button botao)
    {
        campo.Dock = DockStyle.Fill;
        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        painel.Controls.Add(campo, 0, 0);
        painel.Controls.Add(botao, 1, 0);
        return painel;
    }

    private void CarregarCadastrados()
    {
        // Cada e-mail cadastrado vira um item "email — nome (código)".
        foreach (var cadastro in CadastroEmailRepository.Listar())
        {
            foreach (var email in cadastro.Destinatarios)
                _cadastrados.Items.Add(new ItemEmail(email, $"{email} — {cadastro.Nome} ({cadastro.Codigo})"));
        }
        if (_cadastrados.Items.Count > 0)
            _cadastrados.SelectedIndex = 0;
    }

    private void AdicionarCadastrado()
    {
        if (_cadastrados.SelectedItem is ItemEmail item)
            AdicionarDestinatario(item.Email);
    }

    private void AdicionarManual()
    {
        var email = _campoManual.Text.Trim();
        if (email.Length == 0)
            return;
        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            MessageBox.Show(this, $"\"{email}\" não é um e-mail válido.", "E-mail inválido",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        AdicionarDestinatario(email);
        _campoManual.Clear();
        _campoManual.Focus();
    }

    private void AdicionarDestinatario(string email)
    {
        if (!_destinatarios.Items.Contains(email))
            _destinatarios.Items.Add(email);
    }

    private async Task EnviarAsync()
    {
        var destinatarios = _destinatarios.Items.Cast<string>().ToList();
        if (destinatarios.Count == 0)
        {
            MessageBox.Show(this, "Adicione pelo menos um destinatário.", "Sem destinatários",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var anexos = new List<string>();
        if (_boleto.CaminhoArquivo.Length > 0 && File.Exists(_boleto.CaminhoArquivo))
            anexos.Add(_boleto.CaminhoArquivo);

        _botaoEnviar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await EnvioEmailService.EnviarAsync(
                _config, destinatarios, _campoAssunto.Text, _campoCorpo.Text, anexos);
            HistoricoEmailRepository.Registrar(destinatarios, _campoAssunto.Text, anexos.Count);
            MessageBox.Show(this, $"Boleto enviado para {destinatarios.Count} destinatário(s)!",
                "Envio concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha ao enviar o e-mail:\n\n" + ex.Message, "Erro no envio",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _botaoEnviar.Enabled = true;
            UseWaitCursor = false;
        }
    }

    /// <summary>Item do combo de cadastrados: guarda o e-mail e exibe o rótulo.</summary>
    private sealed record ItemEmail(string Email, string Rotulo)
    {
        public override string ToString() => Rotulo;
    }
}
