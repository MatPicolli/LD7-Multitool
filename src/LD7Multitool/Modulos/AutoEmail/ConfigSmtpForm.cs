namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>Formulário de configuração do servidor SMTP.</summary>
public class ConfigSmtpForm : Form
{
    private readonly TextBox _campoServidor;
    private readonly NumericUpDown _campoPorta;
    private readonly CheckBox _campoSsl;
    private readonly TextBox _campoUsuario;
    private readonly TextBox _campoSenha;
    private readonly TextBox _campoRemetente;

    public ConfigSmtpForm()
    {
        Text = "Configurar SMTP";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 300);

        var config = ConfigSmtp.Carregar();

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
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

        AdicionarLinha(tabela, 0, "Servidor (ex: smtp.gmail.com):", _campoServidor);
        AdicionarLinha(tabela, 1, "Porta:", _campoPorta);
        AdicionarLinha(tabela, 2, "", _campoSsl);
        AdicionarLinha(tabela, 3, "Usuário:", _campoUsuario);
        AdicionarLinha(tabela, 4, "Senha:", _campoSenha);
        AdicionarLinha(tabela, 5, "E-mail remetente:", _campoRemetente);

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        var botaoSalvar = new Button { Text = "Salvar", Width = 100 };
        var botaoCancelar = new Button { Text = "Cancelar", Width = 100, DialogResult = DialogResult.Cancel };
        botaoSalvar.Click += (_, _) => Salvar();
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.Controls.Add(painelBotoes, 1, 6);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoServidor.Text) ||
            string.IsNullOrWhiteSpace(_campoRemetente.Text))
        {
            MessageBox.Show(this, "Informe pelo menos o servidor e o e-mail remetente.",
                "Campos obrigatórios", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var config = new ConfigSmtp
        {
            Servidor = _campoServidor.Text.Trim(),
            Porta = (int)_campoPorta.Value,
            UsarSsl = _campoSsl.Checked,
            Usuario = _campoUsuario.Text.Trim(),
            Senha = _campoSenha.Text,
            Remetente = _campoRemetente.Text.Trim(),
        };
        config.Gravar();

        DialogResult = DialogResult.OK;
        Close();
    }
}
