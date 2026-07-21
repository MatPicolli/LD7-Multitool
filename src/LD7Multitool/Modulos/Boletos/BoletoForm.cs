namespace LD7Multitool.Modulos.Boletos;

/// <summary>Formulário de criação/edição de um boleto.</summary>
public class BoletoForm : Form
{
    private readonly TextBox _campoNome;
    private readonly NumericUpDown _campoValor;
    private readonly DateTimePicker _campoValidade;
    private readonly TextBox _campoNossoNumero;
    private readonly TextBox _campoNfeReferente;
    private readonly ComboBox _campoEstado;

    public Boleto Boleto { get; }

    public BoletoForm(Boleto? boleto = null)
    {
        Boleto = boleto ?? new Boleto();

        Text = boleto is null ? "Novo boleto" : "Editar boleto";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 320);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(12),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _campoNome = new TextBox { Dock = DockStyle.Fill, Text = Boleto.Nome };

        _campoValor = new NumericUpDown
        {
            Dock = DockStyle.Fill,
            DecimalPlaces = 2,
            ThousandsSeparator = true,
            Maximum = 1_000_000_000m,
            Minimum = 0m,
            Value = Math.Clamp(Boleto.Valor, 0m, 1_000_000_000m),
        };

        _campoValidade = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Value = Boleto.Validade < DateTimePicker.MinimumDateTime ? DateTime.Today : Boleto.Validade,
        };

        _campoNossoNumero = new TextBox { Dock = DockStyle.Fill, Text = Boleto.NossoNumero };
        _campoNfeReferente = new TextBox { Dock = DockStyle.Fill, Text = Boleto.NfeReferente };

        _campoEstado = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        foreach (EstadoBoleto estado in Enum.GetValues<EstadoBoleto>())
            _campoEstado.Items.Add(estado.Descricao());
        _campoEstado.SelectedIndex = (int)Boleto.Estado;

        AdicionarLinha(tabela, 0, "Nome:", _campoNome);
        AdicionarLinha(tabela, 1, "Valor (R$):", _campoValor);
        AdicionarLinha(tabela, 2, "Validade:", _campoValidade);
        AdicionarLinha(tabela, 3, "Nosso número:", _campoNossoNumero);
        AdicionarLinha(tabela, 4, "NF-e referente:", _campoNfeReferente);
        AdicionarLinha(tabela, 5, "Estado:", _campoEstado);

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        var botaoSalvar = new Button { Text = "Salvar", Width = 100, DialogResult = DialogResult.None };
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
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
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
        if (string.IsNullOrWhiteSpace(_campoNome.Text))
        {
            MessageBox.Show(this, "Informe o nome do boleto.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Boleto.Nome = _campoNome.Text.Trim();
        Boleto.Valor = _campoValor.Value;
        Boleto.Validade = _campoValidade.Value.Date;
        Boleto.NossoNumero = _campoNossoNumero.Text.Trim();
        Boleto.NfeReferente = _campoNfeReferente.Text.Trim();
        Boleto.Estado = (EstadoBoleto)_campoEstado.SelectedIndex;

        DialogResult = DialogResult.OK;
        Close();
    }
}
