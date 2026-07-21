using System.Net.Mail;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>Formulário de criação/edição de um cliente do Auto-Email.</summary>
public class CadastroEmailForm : Form
{
    private readonly TextBox _campoCodigo;
    private readonly TextBox _campoNome;
    private readonly ListBox _listaDestinatarios;
    private readonly TextBox _campoNovoDestinatario;

    public CadastroEmail Cadastro { get; }

    public CadastroEmailForm(CadastroEmail? cadastro = null)
    {
        Cadastro = cadastro ?? new CadastroEmail();

        Text = cadastro is null ? "Novo cliente" : "Editar cliente";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(520, 420);
        ClientSize = new Size(520, 420);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // código
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // nome
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // e-mails
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // botões

        _campoCodigo = new TextBox { Dock = DockStyle.Fill, Text = Cadastro.Codigo };
        _campoNome = new TextBox { Dock = DockStyle.Fill, Text = Cadastro.Nome };

        AdicionarLinha(tabela, 0, "Código:", _campoCodigo);
        AdicionarLinha(tabela, 1, "Nome:", _campoNome);

        // --- E-mails -------------------------------------------------------
        _listaDestinatarios = new ListBox { Dock = DockStyle.Fill };
        foreach (var destinatario in Cadastro.Destinatarios)
            _listaDestinatarios.Items.Add(destinatario);

        _campoNovoDestinatario = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 4, 0),
            PlaceholderText = "novo@email.com",
        };
        var botaoAdicionar = Estilo.BotaoPadrao("Adicionar");
        var botaoRemover = Estilo.BotaoPadrao("Remover");
        botaoAdicionar.Click += (_, _) => AdicionarDestinatario();
        botaoRemover.Click += (_, _) => RemoverDestinatario();
        _campoNovoDestinatario.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                AdicionarDestinatario();
                e.SuppressKeyPress = true;
            }
        };

        var painelEmails = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        painelEmails.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painelEmails.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        painelEmails.Controls.Add(_listaDestinatarios, 0, 0);

        // Tabela em vez de FlowLayoutPanel: garante tudo numa linha só
        // (o fluxo quebrava os botões para uma segunda linha cortada).
        var barraEmails = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
        };
        barraEmails.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barraEmails.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        barraEmails.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        barraEmails.Controls.Add(_campoNovoDestinatario, 0, 0);
        barraEmails.Controls.Add(botaoAdicionar, 1, 0);
        barraEmails.Controls.Add(botaoRemover, 2, 0);
        painelEmails.Controls.Add(barraEmails, 0, 1);

        AdicionarLinha(tabela, 2, "E-mail(s):", painelEmails);

        // --- Botões --------------------------------------------------------
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.Controls.Add(painelBotoes, 1, 3);

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
            Padding = new Padding(0, 8, 0, 0),
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
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

    private void RemoverDestinatario()
    {
        if (_listaDestinatarios.SelectedIndex >= 0)
            _listaDestinatarios.Items.RemoveAt(_listaDestinatarios.SelectedIndex);
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoCodigo.Text))
        {
            MessageBox.Show(this,
                "Informe o código do cliente (o mesmo usado nos nomes dos PDFs, ex.: 5551).",
                "Campo obrigatório", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_campoNome.Text))
        {
            MessageBox.Show(this, "Informe o nome do cliente.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_listaDestinatarios.Items.Count == 0)
        {
            MessageBox.Show(this, "Adicione pelo menos um e-mail.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Cadastro.Codigo = _campoCodigo.Text.Trim();
        Cadastro.Nome = _campoNome.Text.Trim();
        Cadastro.Destinatarios = _listaDestinatarios.Items.Cast<string>().ToList();

        DialogResult = DialogResult.OK;
        Close();
    }
}
