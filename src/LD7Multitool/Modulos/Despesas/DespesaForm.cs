using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Cadastro de um item de despesa: identificação, acesso ao portal e a forma de
/// coleta automática. As abas separam o que todo mundo preenche (identificação
/// e acesso) do que só interessa a quem vai automatizar.
/// </summary>
public class DespesaForm : Form
{
    private const int AlturaLinha = 30;

    private readonly TextBox _campoNome;
    private readonly TextBox _campoFornecedor;
    private readonly ComboBox _campoForma;
    private readonly NumericUpDown _campoDiaVencimento;
    private readonly CheckBox _campoAtivo;

    private readonly TextBox _campoUrl;
    private readonly TextBox _campoIdentificador;
    private readonly TextBox _campoDocumento;
    private readonly TextBox _campoLogin;
    private readonly TextBox _campoSenha;
    private readonly CheckBox _campoMostrarSenha;
    private readonly TextBox _campoObservacoes;

    private readonly ComboBox _campoMetodo;
    private readonly TextBox _campoPadraoArquivo;
    private readonly TextBox _campoEmailRemetente;
    private readonly TextBox _campoEmailAssunto;
    private readonly TextBox _campoConfigHttp;
    private readonly Label _ajudaMetodo;

    public Despesa Despesa { get; }

    public DespesaForm(Despesa? despesa = null)
    {
        var novo = despesa is null;
        Despesa = despesa ?? new Despesa { Ordem = DespesaRepository.ProximaOrdem() };

        Text = novo ? "Nova despesa" : "Editar despesa";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 620);
        ClientSize = new Size(760, 620);
        KeyPreview = true;

        // --- Identificação ---------------------------------------------------
        _campoNome = new TextBox { Text = Despesa.Nome };
        _campoFornecedor = new TextBox { Text = Despesa.Fornecedor };

        _campoForma = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        foreach (FormaObtencao forma in Enum.GetValues<FormaObtencao>())
            _campoForma.Items.Add(forma.Descricao());
        _campoForma.SelectedIndex = (int)Despesa.Forma;

        _campoDiaVencimento = new NumericUpDown { Minimum = 0, Maximum = 31, Value = Despesa.DiaVencimento };
        _campoAtivo = new CheckBox { Text = "Ativo", AutoSize = true, Checked = Despesa.Ativo, Dock = DockStyle.Right };

        // --- Acesso ----------------------------------------------------------
        _campoUrl = new TextBox { Text = Despesa.UrlPortal };
        _campoIdentificador = new TextBox
        {
            Text = Despesa.Identificador,
            PlaceholderText = "unidade consumidora / matrícula / código da conta",
        };
        _campoDocumento = new TextBox { Text = Despesa.Documento, PlaceholderText = "CPF ou CNPJ usado no acesso" };
        _campoLogin = new TextBox { Text = Despesa.Login };
        _campoSenha = new TextBox { Text = Despesa.Senha, UseSystemPasswordChar = true };
        _campoMostrarSenha = new CheckBox
        {
            Text = "Mostrar",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _campoMostrarSenha.CheckedChanged += (_, _) =>
            _campoSenha.UseSystemPasswordChar = !_campoMostrarSenha.Checked;

        _campoObservacoes = new TextBox
        {
            Text = Despesa.Observacoes,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
        };

        // --- Coleta automática ------------------------------------------------
        _campoMetodo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        foreach (MetodoColeta metodo in Enum.GetValues<MetodoColeta>())
            _campoMetodo.Items.Add(metodo.Descricao());
        _campoMetodo.SelectedIndex = (int)Despesa.Metodo;
        _campoMetodo.SelectedIndexChanged += (_, _) => AtualizarCamposDeColeta();

        _campoPadraoArquivo = new TextBox
        {
            Text = Despesa.PadraoArquivo,
            PlaceholderText = "ex.: celesc*haras*.pdf",
        };
        _campoEmailRemetente = new TextBox
        {
            Text = Despesa.EmailRemetente,
            PlaceholderText = "trecho do remetente, ex.: generation",
        };
        _campoEmailAssunto = new TextBox
        {
            Text = Despesa.EmailAssunto,
            PlaceholderText = "trecho do assunto, ex.: fatura",
        };
        _campoConfigHttp = new TextBox
        {
            Text = Despesa.ConfigHttp,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 9f),
        };
        _ajudaMetodo = new Label { Dock = DockStyle.Fill, ForeColor = Estilo.CorTextoSuave };

        // --- Abas -------------------------------------------------------------
        var abas = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
        abas.TabPages.Add(Aba("Identificação e acesso", MontarAbaCadastro()));
        abas.TabPages.Add(Aba("Coleta automática", MontarAbaColeta()));

        var cabecalho = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Estilo.CorFundo,
            Padding = new Padding(20, 12, 20, 0),
        };
        cabecalho.Controls.Add(new Label
        {
            Text = novo ? "Inserindo" : "Editando",
            AutoSize = true,
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI Semibold", 12f),
        });
        cabecalho.Controls.Add(_campoAtivo);

        var botaoSalvar = Estilo.BotaoPrimario("Gravar (F8)");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        var conteudo = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 8) };
        conteudo.Controls.Add(abas);

        Controls.Add(conteudo);
        Controls.Add(painelBotoes);
        Controls.Add(cabecalho);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F8)
                Salvar();
        };

        AtualizarCamposDeColeta();
    }

    private Control MontarAbaCadastro()
    {
        var grupoItem = Grupo("Item", new (string, Control)[]
        {
            ("Nome *", _campoNome),
            ("Fornecedor", _campoFornecedor),
            ("Como obter", _campoForma),
            ("Dia do vencimento", LadoALado((_campoDiaVencimento, 1), (Dica("0 = varia todo mês"), 3))),
        });

        var grupoAcesso = Grupo("Acesso ao portal", new (string, Control)[]
        {
            ("Portal (endereço)", _campoUrl),
            ("Identificador", _campoIdentificador),
            ("CPF / CNPJ", _campoDocumento),
            ("Login", _campoLogin),
            ("Senha", LadoALado((_campoSenha, 3), (_campoMostrarSenha, 1))),
        });

        var grupoObservacoes = new GroupBox
        {
            Text = "Observações",
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ForeColor = Estilo.CorPrimaria,
            Font = new Font("Segoe UI Semibold", 9.75f),
            Padding = new Padding(12, 6, 12, 10),
        };
        _campoObservacoes.Dock = DockStyle.Fill;
        _campoObservacoes.Font = Estilo.FontePadrao;
        grupoObservacoes.Controls.Add(_campoObservacoes);

        return Empilhar(
            (grupoItem, AlturaGrupo(4)),
            (grupoAcesso, AlturaGrupo(5)),
            (grupoObservacoes, 0));
    }

    private Control MontarAbaColeta()
    {
        var grupoMetodo = Grupo("Como o programa busca esta conta", new (string, Control)[]
        {
            ("Método", _campoMetodo),
            ("Máscara do arquivo", _campoPadraoArquivo),
            ("E-mail — remetente", _campoEmailRemetente),
            ("E-mail — assunto", _campoEmailAssunto),
        });

        var grupoReceita = new GroupBox
        {
            Text = "Receita de consulta ao portal (JSON) — só para o método \"Portal (HTTP)\"",
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ForeColor = Estilo.CorPrimaria,
            Font = new Font("Segoe UI Semibold", 9.75f),
            Padding = new Padding(12, 6, 12, 10),
        };
        _campoConfigHttp.Dock = DockStyle.Fill;
        grupoReceita.Controls.Add(_campoConfigHttp);

        var painelAjuda = new Panel { Dock = DockStyle.Top, Height = 76, Padding = new Padding(4, 4, 4, 8) };
        painelAjuda.Controls.Add(_ajudaMetodo);

        var pilha = Empilhar(
            (grupoMetodo, AlturaGrupo(4)),
            (grupoReceita, 0));

        var raiz = new Panel { Dock = DockStyle.Fill };
        raiz.Controls.Add(pilha);
        raiz.Controls.Add(painelAjuda);
        return raiz;
    }

    /// <summary>Habilita só os campos do método escolhido e explica o que ele faz.</summary>
    private void AtualizarCamposDeColeta()
    {
        var metodo = (MetodoColeta)_campoMetodo.SelectedIndex;

        _campoPadraoArquivo.Enabled = metodo == MetodoColeta.Pasta;
        _campoEmailRemetente.Enabled = metodo == MetodoColeta.Email;
        _campoEmailAssunto.Enabled = metodo == MetodoColeta.Email;
        _campoConfigHttp.Enabled = metodo == MetodoColeta.Http;

        _ajudaMetodo.Text = metodo switch
        {
            MetodoColeta.Nenhum =>
                "Sem busca automática: a conta é lançada à mão (ou pelo botão \"Lançar conta\", " +
                "que já lê valor e vencimento de um PDF de boleto).",
            MetodoColeta.Pasta =>
                "O programa procura na pasta de downloads (⚙) os PDFs que casam com a máscara e lê " +
                "valor e vencimento pela linha digitável. É o caminho mais seguro para portais com " +
                "login complicado: você baixa a segunda via como sempre e o resto é automático.",
            MetodoColeta.Email =>
                "O programa lê a caixa de entrada configurada no ⚙, filtra pelo remetente/assunto, " +
                "salva os anexos em PDF e lança as contas encontradas.",
            MetodoColeta.Http =>
                "O programa consulta o portal seguindo a receita JSON abaixo. Serve para portais " +
                "simples (formulário sem captcha nem token). Sites com login em duas etapas, " +
                "captcha ou aplicativo não funcionam por aqui — nesses, use \"Pasta de downloads\".",
            _ => "",
        };
    }

    private void Salvar()
    {
        if (_campoNome.Text.Trim().Length == 0)
        {
            MessageBox.Show(this, "Informe o nome do item.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _campoNome.Focus();
            return;
        }

        Despesa.Nome = _campoNome.Text.Trim();
        Despesa.Fornecedor = _campoFornecedor.Text.Trim();
        Despesa.Forma = (FormaObtencao)_campoForma.SelectedIndex;
        Despesa.DiaVencimento = (int)_campoDiaVencimento.Value;
        Despesa.Ativo = _campoAtivo.Checked;

        Despesa.UrlPortal = _campoUrl.Text.Trim();
        Despesa.Identificador = _campoIdentificador.Text.Trim();
        // Como no resto do programa, o documento fica gravado só com dígitos.
        Despesa.Documento = Febraban.SomenteDigitos(_campoDocumento.Text);
        Despesa.Login = _campoLogin.Text.Trim();
        Despesa.Senha = _campoSenha.Text;
        Despesa.Observacoes = _campoObservacoes.Text.Trim();

        Despesa.Metodo = (MetodoColeta)_campoMetodo.SelectedIndex;
        Despesa.PadraoArquivo = _campoPadraoArquivo.Text.Trim();
        Despesa.EmailRemetente = _campoEmailRemetente.Text.Trim();
        Despesa.EmailAssunto = _campoEmailAssunto.Text.Trim();
        Despesa.ConfigHttp = _campoConfigHttp.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }

    // --- Montagem do layout --------------------------------------------------

    private static TabPage Aba(string titulo, Control conteudo)
    {
        var aba = new TabPage(titulo) { BackColor = Estilo.CorFundo, Padding = new Padding(12, 10, 12, 10) };
        conteudo.Dock = DockStyle.Fill;
        aba.Controls.Add(conteudo);
        return aba;
    }

    /// <summary>Altura fixa de uma caixa com N linhas — evita corte de campo.</summary>
    private static int AlturaGrupo(int linhas) => linhas * AlturaLinha + 48;

    /// <summary>Empilha caixas com altura fixa; altura 0 significa "ocupa o resto".</summary>
    private static TableLayoutPanel Empilhar(params (Control Caixa, int Altura)[] caixas)
    {
        var pilha = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        pilha.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var linha = 0;
        foreach (var (caixa, altura) in caixas)
        {
            pilha.RowStyles.Add(altura > 0
                ? new RowStyle(SizeType.Absolute, altura)
                : new RowStyle(SizeType.Percent, 100));
            caixa.Margin = new Padding(0, 0, 0, 12);
            pilha.Controls.Add(caixa, 0, linha++);
        }
        pilha.RowCount = linha;
        return pilha;
    }

    private static GroupBox Grupo(string titulo, (string Rotulo, Control Campo)[] linhas)
    {
        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < linhas.Length; i++)
        {
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, AlturaLinha));
            tabela.Controls.Add(new Label
            {
                Text = linhas[i].Rotulo,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = Estilo.CorTextoSuave,
                Margin = new Padding(0, 0, 12, 0),
                MinimumSize = new Size(150, 0),
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

    private static Control LadoALado(params (Control Campo, int Peso)[] itens)
    {
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = itens.Length, RowCount = 1, Margin = new Padding(0) };
        for (var i = 0; i < itens.Length; i++)
        {
            painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, itens[i].Peso));
            itens[i].Campo.Dock = DockStyle.Fill;
            itens[i].Campo.Margin = new Padding(i == 0 ? 0 : 10, 0, 0, 0);
            painel.Controls.Add(itens[i].Campo, i, 0);
        }
        return painel;
    }

    private static Label Dica(string texto) => new()
    {
        Text = texto,
        ForeColor = Estilo.CorTextoSuave,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoSize = false,
    };
}
