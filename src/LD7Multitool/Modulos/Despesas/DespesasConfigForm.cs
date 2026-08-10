using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>Dados da conta de e-mail usada pela coleta (IMAP).</summary>
public sealed record ConfigImap(string Servidor, int Porta, bool UsarSsl, string Usuario, string Senha)
{
    public bool Configurado =>
        Servidor.Trim().Length > 0 && Usuario.Trim().Length > 0 && Senha.Length > 0;
}

/// <summary>
/// Configurações do módulo Despesas: pasta onde as segundas via são baixadas,
/// janela de busca e a conta de e-mail lida pela coleta automática.
/// </summary>
public class DespesasConfigForm : Form
{
    public const string ChavePastaDownloads = "despesas_pasta_downloads";
    public const string ChaveDias = "despesas_dias_busca";
    public const string ChaveImapServidor = "despesas_imap_servidor";
    public const string ChaveImapPorta = "despesas_imap_porta";
    public const string ChaveImapSsl = "despesas_imap_ssl";
    public const string ChaveImapUsuario = "despesas_imap_usuario";
    public const string ChaveImapSenha = "despesas_imap_senha";

    private const int DiasPadrao = 60;

    public static string PastaDownloads => ConfiguracaoRepository.Obter(ChavePastaDownloads) ?? "";

    /// <summary>Quantos dias para trás a coleta olha (arquivos e e-mails).</summary>
    public static int DiasBusca =>
        int.TryParse(ConfiguracaoRepository.Obter(ChaveDias), out var dias) && dias > 0 ? dias : DiasPadrao;

    public static ConfigImap LerConfigImap() => new(
        ConfiguracaoRepository.Obter(ChaveImapServidor) ?? "",
        int.TryParse(ConfiguracaoRepository.Obter(ChaveImapPorta), out var porta) ? porta : 993,
        (ConfiguracaoRepository.Obter(ChaveImapSsl) ?? "1") == "1",
        ConfiguracaoRepository.Obter(ChaveImapUsuario) ?? "",
        Segredo.Revelar(ConfiguracaoRepository.Obter(ChaveImapSenha) ?? ""));

    private readonly TextBox _campoPasta;
    private readonly NumericUpDown _campoDias;
    private readonly TextBox _campoServidor;
    private readonly NumericUpDown _campoPorta;
    private readonly CheckBox _campoSsl;
    private readonly TextBox _campoUsuario;
    private readonly TextBox _campoSenha;

    public DespesasConfigForm()
    {
        var config = LerConfigImap();

        Text = "Configurações — Despesas";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 470);

        _campoPasta = new TextBox { Text = PastaDownloads };
        _campoDias = new NumericUpDown { Minimum = 7, Maximum = 730, Value = DiasBusca };
        _campoServidor = new TextBox { Text = config.Servidor, PlaceholderText = "imap.gmail.com" };
        _campoPorta = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = config.Porta };
        // AutoSize fica desligado porque o campo é colocado com Dock.Fill numa
        // TableLayoutPanel — as duas coisas juntas dão tamanho imprevisível.
        _campoSsl = new CheckBox
        {
            Text = "Usar SSL/TLS",
            Checked = config.UsarSsl,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _campoUsuario = new TextBox { Text = config.Usuario, PlaceholderText = "conta@exemplo.com" };
        _campoSenha = new TextBox { Text = config.Senha, UseSystemPasswordChar = true };

        var botaoProcurar = Estilo.BotaoPadrao("Procurar...");
        botaoProcurar.Click += (_, _) => Procurar();

        var linhaPasta = ComBotao(_campoPasta, botaoProcurar);
        var linhaPorta = LadoALado((_campoPorta, 1), (_campoSsl, 2));

        var grupoArquivos = Grupo("Downloads das segundas vias", new (string, Control)[]
        {
            ("Pasta", linhaPasta),
            ("Buscar últimos (dias)", _campoDias),
        });

        var grupoEmail = Grupo("Conta de e-mail lida pela coleta (IMAP)", new (string, Control)[]
        {
            ("Servidor", _campoServidor),
            ("Porta / segurança", linhaPorta),
            ("Usuário", _campoUsuario),
            ("Senha", _campoSenha),
        });

        var aviso = new Label
        {
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(16, 8, 16, 8),
            ForeColor = Estilo.CorTextoSuave,
            Text =
                "A pasta de downloads é onde o programa procura os PDFs baixados dos portais e " +
                "onde salva os anexos vindos por e-mail.\n\n" +
                "Em contas Gmail/Outlook com verificação em duas etapas, o IMAP exige uma SENHA " +
                "DE APLICATIVO (a senha normal é recusada). A senha é gravada cifrada e só é " +
                "legível nesta conta do Windows — ao copiar o dados.db para outra máquina, " +
                "digite-a novamente.",
        };

        var conteudo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(16, 8, 16, 8),
        };
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        conteudo.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        conteudo.Controls.Add(grupoArquivos, 0, 0);
        conteudo.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
        conteudo.Controls.Add(new Panel(), 0, 1);
        conteudo.RowStyles.Add(new RowStyle(SizeType.Absolute, 168));
        conteudo.Controls.Add(grupoEmail, 0, 2);
        conteudo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        conteudo.RowCount = 4;

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(16, 8, 16, 8),
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        // Fill primeiro, depois os docks de borda (convenção do projeto).
        Controls.Add(conteudo);
        Controls.Add(painelBotoes);
        Controls.Add(aviso);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private void Procurar()
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Selecione a pasta onde os boletos são baixados",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_campoPasta.Text) ? _campoPasta.Text : "",
        };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            _campoPasta.Text = dialogo.SelectedPath;
    }

    private void Salvar()
    {
        var pasta = _campoPasta.Text.Trim();
        if (pasta.Length > 0 && !Directory.Exists(pasta))
        {
            MessageBox.Show(this, "A pasta informada não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ConfiguracaoRepository.Definir(ChavePastaDownloads, pasta);
        ConfiguracaoRepository.Definir(ChaveDias, ((int)_campoDias.Value).ToString());
        ConfiguracaoRepository.Definir(ChaveImapServidor, _campoServidor.Text.Trim());
        ConfiguracaoRepository.Definir(ChaveImapPorta, ((int)_campoPorta.Value).ToString());
        ConfiguracaoRepository.Definir(ChaveImapSsl, _campoSsl.Checked ? "1" : "0");
        ConfiguracaoRepository.Definir(ChaveImapUsuario, _campoUsuario.Text.Trim());
        ConfiguracaoRepository.Definir(ChaveImapSenha, Segredo.Proteger(_campoSenha.Text));

        DialogResult = DialogResult.OK;
        Close();
    }

    // --- Montagem do layout (mesmas convenções das outras telas) -------------

    private static GroupBox Grupo(string titulo, (string Rotulo, Control Campo)[] linhas)
    {
        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < linhas.Length; i++)
        {
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            tabela.Controls.Add(new Label
            {
                Text = linhas[i].Rotulo,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Estilo.CorTextoSuave,
                Margin = new Padding(0, 0, 12, 0),
                MinimumSize = new Size(140, 0),
            }, 0, i);

            var campo = linhas[i].Campo;
            campo.Dock = DockStyle.Fill;
            campo.Margin = new Padding(0, 3, 0, 3);
            tabela.Controls.Add(campo, 1, i);
        }
        tabela.RowCount = linhas.Length;

        var grupo = new GroupBox
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ForeColor = Estilo.CorPrimaria,
            Font = new Font("Segoe UI Semibold", 9.75f),
            Padding = new Padding(12, 6, 12, 10),
        };
        grupo.Controls.Add(tabela);
        return grupo;
    }

    private static Control ComBotao(Control campo, Button botao)
    {
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        campo.Dock = DockStyle.Fill;
        campo.Margin = new Padding(0, 2, 0, 2);
        // Os botões do Estilo vêm com AutoSize; dentro de uma célula com Dock.Fill
        // quem manda é a célula — deixar os dois ligados dá tamanho imprevisível.
        botao.AutoSize = false;
        botao.Dock = DockStyle.Fill;
        botao.Margin = new Padding(6, 0, 0, 0);
        painel.Controls.Add(campo, 0, 0);
        painel.Controls.Add(botao, 1, 0);
        return painel;
    }

    private static Control LadoALado(params (Control Campo, int Peso)[] itens)
    {
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = itens.Length, RowCount = 1, Margin = new Padding(0) };
        for (var i = 0; i < itens.Length; i++)
        {
            painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, itens[i].Peso));
            itens[i].Campo.Dock = DockStyle.Fill;
            itens[i].Campo.Margin = new Padding(i == 0 ? 0 : 10, 3, 0, 3);
            painel.Controls.Add(itens[i].Campo, i, 0);
        }
        return painel;
    }
}
