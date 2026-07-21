using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Configurações do Auto-Email: servidor SMTP (com teste de conexão) e as
/// pastas onde ficam os PDFs de notas fiscais e boletos.
/// </summary>
public class AutoEmailConfigForm : Form
{
    private readonly TextBox _campoServidor;
    private readonly NumericUpDown _campoPorta;
    private readonly CheckBox _campoSsl;
    private readonly TextBox _campoUsuario;
    private readonly TextBox _campoSenha;
    private readonly TextBox _campoRemetente;
    private readonly TextBox _campoPastaNfe;
    private readonly TextBox _campoPastaBoletos;
    private readonly Button _botaoTestar;

    public AutoEmailConfigForm()
    {
        Text = "Configurações — Auto-Email";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 500);

        var config = ConfigSmtp.Carregar();

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 11,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _campoServidor = new TextBox { Dock = DockStyle.Fill, Text = config.Servidor };
        _campoPorta = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            Minimum = 1,
            Maximum = 65535,
            Value = Math.Clamp(config.Porta, 1, 65535),
        };
        _campoSsl = new CheckBox { Text = "Usar SSL/TLS", Checked = config.UsarSsl, Dock = DockStyle.Fill };
        _campoUsuario = new TextBox { Dock = DockStyle.Fill, Text = config.Usuario };
        _campoSenha = new TextBox { Dock = DockStyle.Fill, Text = config.Senha, UseSystemPasswordChar = true };
        _campoRemetente = new TextBox { Dock = DockStyle.Fill, Text = config.Remetente };
        _campoPastaNfe = new TextBox { Dock = DockStyle.Fill, Text = config.PastaNfe };
        _campoPastaBoletos = new TextBox { Dock = DockStyle.Fill, Text = config.PastaBoletos };

        AdicionarTituloSecao(tabela, 0, "Servidor de e-mail (SMTP)");
        AdicionarLinha(tabela, 1, "Servidor:", _campoServidor);
        AdicionarLinha(tabela, 2, "Porta:", _campoPorta);
        AdicionarLinha(tabela, 3, "", _campoSsl);
        AdicionarLinha(tabela, 4, "Usuário:", _campoUsuario);
        AdicionarLinha(tabela, 5, "Senha:", _campoSenha);
        AdicionarLinha(tabela, 6, "E-mail remetente:", _campoRemetente);
        AdicionarTituloSecao(tabela, 7, "Pastas dos arquivos");
        AdicionarLinha(tabela, 8, "Notas Fiscais:", CriarSeletorPasta(_campoPastaNfe));
        AdicionarLinha(tabela, 9, "Boletos:", CriarSeletorPasta(_campoPastaBoletos));

        _botaoTestar = Estilo.BotaoPadrao("Testar conexão");
        _botaoTestar.Click += async (_, _) => await TestarAsync();

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(_botaoTestar, 0, 10);
        tabela.Controls.Add(painelBotoes, 1, 10);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private static void AdicionarTituloSecao(TableLayoutPanel tabela, int linha, string texto)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        var rotulo = new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Estilo.CorPrimaria,
        };
        tabela.Controls.Add(rotulo, 0, linha);
        tabela.SetColumnSpan(rotulo, 2);
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private Control CriarSeletorPasta(TextBox campo)
    {
        var botaoProcurar = Estilo.BotaoPadrao("...");
        botaoProcurar.AutoSize = false;
        botaoProcurar.MinimumSize = Size.Empty;
        botaoProcurar.Size = new Size(36, 29);
        botaoProcurar.Margin = new Padding(4, 0, 0, 0);
        botaoProcurar.Padding = new Padding(0);
        botaoProcurar.Click += (_, _) =>
        {
            using var dialogo = new FolderBrowserDialog
            {
                Description = "Selecione a pasta",
                UseDescriptionForTitle = true,
                SelectedPath = Directory.Exists(campo.Text) ? campo.Text : "",
            };
            if (dialogo.ShowDialog(this) == DialogResult.OK)
                campo.Text = dialogo.SelectedPath;
        };

        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        painel.Controls.Add(campo, 0, 0);
        painel.Controls.Add(botaoProcurar, 1, 0);
        return painel;
    }

    private ConfigSmtp MontarConfig() => new()
    {
        Servidor = _campoServidor.Text.Trim(),
        Porta = (int)_campoPorta.Value,
        UsarSsl = _campoSsl.Checked,
        Usuario = _campoUsuario.Text.Trim(),
        Senha = _campoSenha.Text,
        Remetente = _campoRemetente.Text.Trim(),
        PastaNfe = _campoPastaNfe.Text.Trim(),
        PastaBoletos = _campoPastaBoletos.Text.Trim(),
    };

    private async Task TestarAsync()
    {
        var config = MontarConfig();
        if (!config.Configurada)
        {
            MessageBox.Show(this, "Preencha pelo menos o servidor e o e-mail remetente.",
                "Configuração incompleta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _botaoTestar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await EnvioEmailService.TestarConexaoAsync(config);
            MessageBox.Show(this,
                $"Conexão funcionando! Um e-mail de teste foi enviado para {config.Remetente}.",
                "Teste bem-sucedido", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha no teste de conexão:\n\n" + ex.Message,
                "Teste falhou", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _botaoTestar.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Salvar()
    {
        var config = MontarConfig();

        if (config.PastaNfe.Length > 0 && !Directory.Exists(config.PastaNfe))
        {
            MessageBox.Show(this, "A pasta de Notas Fiscais não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (config.PastaBoletos.Length > 0 && !Directory.Exists(config.PastaBoletos))
        {
            MessageBox.Show(this, "A pasta de Boletos não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        config.Gravar();
        DialogResult = DialogResult.OK;
        Close();
    }
}
