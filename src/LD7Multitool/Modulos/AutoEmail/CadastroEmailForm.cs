using System.Net.Mail;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>Formulário de criação/edição de um cadastro de envio.</summary>
public class CadastroEmailForm : Form
{
    private readonly TextBox _campoNome;
    private readonly TextBox _campoAssunto;
    private readonly TextBox _campoCorpo;
    private readonly ListBox _listaDestinatarios;
    private readonly TextBox _campoNovoDestinatario;
    private readonly ListBox _listaArquivos;

    public CadastroEmail Cadastro { get; }

    public CadastroEmailForm(CadastroEmail? cadastro = null)
    {
        Cadastro = cadastro ?? new CadastroEmail();

        Text = cadastro is null ? "Novo cadastro de e-mail" : "Editar cadastro de e-mail";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(640, 560);
        ClientSize = new Size(640, 560);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(12),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // nome
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // assunto
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 30));    // corpo
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 35));    // destinatários
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 35));    // arquivos
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // botões

        _campoNome = new TextBox { Dock = DockStyle.Fill, Text = Cadastro.Nome };
        _campoAssunto = new TextBox { Dock = DockStyle.Fill, Text = Cadastro.Assunto };
        _campoCorpo = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = Cadastro.Corpo,
        };

        AdicionarLinha(tabela, 0, "Nome:", _campoNome);
        AdicionarLinha(tabela, 1, "Assunto:", _campoAssunto);
        AdicionarLinha(tabela, 2, "Mensagem:", _campoCorpo);

        // --- Destinatários ---------------------------------------------------
        _listaDestinatarios = new ListBox { Dock = DockStyle.Fill };
        foreach (var destinatario in Cadastro.Destinatarios)
            _listaDestinatarios.Items.Add(destinatario);

        _campoNovoDestinatario = new TextBox { Width = 220 };
        var botaoAdicionarDestinatario = new Button { Text = "Adicionar", Width = 90 };
        var botaoRemoverDestinatario = new Button { Text = "Remover", Width = 90 };
        botaoAdicionarDestinatario.Click += (_, _) => AdicionarDestinatario();
        botaoRemoverDestinatario.Click += (_, _) => RemoverSelecionado(_listaDestinatarios);
        _campoNovoDestinatario.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AdicionarDestinatario();
                e.SuppressKeyPress = true;
            }
        };

        var painelDestinatarios = CriarPainelLista(_listaDestinatarios,
            _campoNovoDestinatario, botaoAdicionarDestinatario, botaoRemoverDestinatario);
        AdicionarLinha(tabela, 3, "Destinatários:", painelDestinatarios);

        // --- Arquivos --------------------------------------------------------
        _listaArquivos = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
        };
        foreach (var arquivo in Cadastro.Arquivos)
            _listaArquivos.Items.Add(arquivo);

        var botaoAdicionarArquivo = new Button { Text = "Adicionar...", Width = 90 };
        var botaoRemoverArquivo = new Button { Text = "Remover", Width = 90 };
        botaoAdicionarArquivo.Click += (_, _) => AdicionarArquivos();
        botaoRemoverArquivo.Click += (_, _) => RemoverSelecionado(_listaArquivos);

        var painelArquivos = CriarPainelLista(_listaArquivos,
            null, botaoAdicionarArquivo, botaoRemoverArquivo);
        AdicionarLinha(tabela, 4, "Arquivos:", painelArquivos);

        // --- Botões ----------------------------------------------------------
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
        tabela.Controls.Add(painelBotoes, 1, 5);

        Controls.Add(tabela);
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
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private static Control CriarPainelLista(ListBox lista, TextBox? campoTexto, params Button[] botoes)
    {
        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
        };
        painel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        painel.Controls.Add(lista, 0, 0);

        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        if (campoTexto is not null)
            barra.Controls.Add(campoTexto);
        foreach (var botao in botoes)
            barra.Controls.Add(botao);
        painel.Controls.Add(barra, 0, 1);

        return painel;
    }

    private void AdicionarDestinatario()
    {
        var email = _campoNovoDestinatario.Text.Trim();
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

        if (!_listaDestinatarios.Items.Contains(email))
            _listaDestinatarios.Items.Add(email);
        _campoNovoDestinatario.Clear();
        _campoNovoDestinatario.Focus();
    }

    private void AdicionarArquivos()
    {
        using var dialogo = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Selecionar arquivos para anexar",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;
        foreach (var arquivo in dialogo.FileNames)
        {
            if (!_listaArquivos.Items.Contains(arquivo))
                _listaArquivos.Items.Add(arquivo);
        }
    }

    private static void RemoverSelecionado(ListBox lista)
    {
        if (lista.SelectedIndex >= 0)
            lista.Items.RemoveAt(lista.SelectedIndex);
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoNome.Text))
        {
            MessageBox.Show(this, "Informe o nome do cadastro.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_listaDestinatarios.Items.Count == 0)
        {
            MessageBox.Show(this, "Adicione pelo menos um destinatário.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Cadastro.Nome = _campoNome.Text.Trim();
        Cadastro.Assunto = _campoAssunto.Text.Trim();
        Cadastro.Corpo = _campoCorpo.Text;
        Cadastro.Destinatarios = _listaDestinatarios.Items.Cast<string>().ToList();
        Cadastro.Arquivos = _listaArquivos.Items.Cast<string>().ToList();

        DialogResult = DialogResult.OK;
        Close();
    }
}
