using LD7Multitool.Core;

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
    private readonly TextBox _campoArquivo;

    public Boleto Boleto { get; }

    public BoletoForm(Boleto? boleto = null)
    {
        Boleto = boleto ?? new Boleto();

        Text = boleto is null ? "Novo boleto" : "Editar boleto";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 400);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            Padding = new Padding(16),
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
            FlatStyle = FlatStyle.Flat,
        };
        foreach (EstadoBoleto estado in Enum.GetValues<EstadoBoleto>())
            _campoEstado.Items.Add(estado.Descricao());
        _campoEstado.SelectedIndex = (int)Boleto.Estado;

        // Arquivo PDF vinculado: caixa somente leitura + botão de escolha.
        _campoArquivo = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = Boleto.CaminhoArquivo,
        };
        var botaoEscolherArquivo = Estilo.BotaoPadrao("...");
        botaoEscolherArquivo.AutoSize = false;
        botaoEscolherArquivo.MinimumSize = Size.Empty;
        botaoEscolherArquivo.Size = new Size(36, 29);
        botaoEscolherArquivo.Margin = new Padding(4, 0, 0, 0);
        botaoEscolherArquivo.Padding = new Padding(0);
        botaoEscolherArquivo.Click += (_, _) => EscolherArquivo();

        var painelArquivo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
        };
        painelArquivo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painelArquivo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        painelArquivo.Controls.Add(_campoArquivo, 0, 0);
        painelArquivo.Controls.Add(botaoEscolherArquivo, 1, 0);

        AdicionarLinha(tabela, 0, "Nome:", _campoNome);
        AdicionarLinha(tabela, 1, "Valor (R$):", _campoValor);
        AdicionarLinha(tabela, 2, "Vencimento:", _campoValidade);
        AdicionarLinha(tabela, 3, "Nosso número:", _campoNossoNumero);
        AdicionarLinha(tabela, 4, "NF-e referente:", _campoNfeReferente);
        AdicionarLinha(tabela, 5, "Estado:", _campoEstado);
        AdicionarLinha(tabela, 6, "Arquivo PDF:", painelArquivo);

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
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(painelBotoes, 1, 7);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private void EscolherArquivo()
    {
        var pastaPadrao = BoletosConfigForm.PastaPdfs;
        using var dialogo = new OpenFileDialog
        {
            Title = "Selecionar o PDF do boleto",
            Filter = "Arquivos PDF (*.pdf)|*.pdf|Todos os arquivos (*.*)|*.*",
            InitialDirectory = Directory.Exists(pastaPadrao) ? pastaPadrao : "",
        };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            _campoArquivo.Text = dialogo.FileName;
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
        Boleto.CaminhoArquivo = _campoArquivo.Text.Trim();

        DialogResult = DialogResult.OK;
        Close();
    }
}
