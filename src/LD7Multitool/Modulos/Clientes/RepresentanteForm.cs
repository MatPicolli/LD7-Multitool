using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Formulário de criação/edição de um representante.</summary>
public class RepresentanteForm : Form
{
    private readonly TextBox _campoNome;
    private readonly TextBox _campoEmail;
    private readonly TextBox _campoTelefone1;
    private readonly TextBox _campoTelefone2;

    public Representante Representante { get; }

    public RepresentanteForm(Representante? representante = null)
    {
        Representante = representante ?? new Representante();

        Text = representante is null ? "Novo representante" : "Editar representante";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 260);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _campoNome = new TextBox { Dock = DockStyle.Fill, Text = Representante.Nome };
        _campoEmail = new TextBox { Dock = DockStyle.Fill, Text = Representante.Email };
        _campoTelefone1 = new TextBox { Dock = DockStyle.Fill, Text = Representante.Telefone1 };
        _campoTelefone2 = new TextBox { Dock = DockStyle.Fill, Text = Representante.Telefone2 };

        AdicionarLinha(tabela, 0, "Nome:", _campoNome);
        AdicionarLinha(tabela, 1, "E-mail:", _campoEmail);
        AdicionarLinha(tabela, 2, "Telefone 1:", _campoTelefone1);
        AdicionarLinha(tabela, 3, "Telefone 2:", _campoTelefone2);

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(painelBotoes, 1, 4);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoNome.Text))
        {
            MessageBox.Show(this, "Informe o nome do representante.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Representante.Nome = _campoNome.Text.Trim();
        Representante.Email = _campoEmail.Text.Trim();
        Representante.Telefone1 = _campoTelefone1.Text.Trim();
        Representante.Telefone2 = _campoTelefone2.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}
