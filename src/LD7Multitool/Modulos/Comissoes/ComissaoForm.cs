using System.Globalization;
using LD7Multitool.Core;
using LD7Multitool.Modulos.Clientes;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>Edição de uma linha (parcela) da tabela de comissões.</summary>
public class ComissaoForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly TextBox _campoCliente;
    private readonly TextBox _campoNotaFiscal;
    private readonly TextBox _campoBoleto;
    private readonly TextBox _campoPedido;
    private readonly NumericUpDown _campoValorNf;
    private readonly DateTimePicker _campoVencimento;
    private readonly TextBox _campoParcela;
    private readonly NumericUpDown _campoValorParcela;
    private readonly NumericUpDown _campoPercentual;
    private readonly Label _rotuloComissao;
    private readonly ComboBox _campoStatus;
    private readonly DateTimePicker _campoDataPagamento;

    public Comissao Comissao { get; }

    public ComissaoForm(Comissao comissao)
    {
        Comissao = comissao;

        Text = "Editar parcela";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 548);

        _campoCliente = new TextBox { Dock = DockStyle.Fill, Text = comissao.Cliente };
        ConfigurarSugestoesCliente(_campoCliente);
        _campoNotaFiscal = new TextBox { Dock = DockStyle.Fill, Text = comissao.NotaFiscal };
        _campoBoleto = new TextBox { Dock = DockStyle.Fill, Text = comissao.NumeroBoleto };
        _campoPedido = new TextBox { Dock = DockStyle.Fill, Text = comissao.RefPedido };
        _campoValorNf = CriarCampoValor(comissao.ValorNf);
        _campoVencimento = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Value = comissao.Vencimento,
        };
        _campoParcela = new TextBox { Dock = DockStyle.Fill, Text = comissao.Parcela, PlaceholderText = "ex.: 01/13 ou UNICA" };
        _campoValorParcela = CriarCampoValor(comissao.ValorParcela);
        _campoPercentual = CriarCampoPercentual(comissao.Percentual);
        _rotuloComissao = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = Estilo.FonteCabecalhoGrade };

        _campoStatus = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
        };
        _campoStatus.Items.AddRange(new object[] { "Não pago", "Pago" });
        _campoStatus.SelectedIndex = comissao.Paga ? 1 : 0;

        // A planilha tem linhas "Pago" sem data, então a data é opcional
        // (caixinha) e só vale quando o status é Pago.
        _campoDataPagamento = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Value = comissao.DataPagamento ?? DateTime.Today,
            Checked = comissao.DataPagamento is not null,
            Enabled = comissao.Paga,
        };
        _campoStatus.SelectedIndexChanged += (_, _) =>
        {
            var paga = _campoStatus.SelectedIndex == 1;
            _campoDataPagamento.Enabled = paga;
            if (paga && !_campoDataPagamento.Checked)
            {
                _campoDataPagamento.Value = DateTime.Today;
                _campoDataPagamento.Checked = true;
            }
            else if (!paga)
            {
                _campoDataPagamento.Checked = false;
            }
        };

        _campoValorParcela.ValueChanged += (_, _) => AtualizarComissao();
        _campoPercentual.ValueChanged += (_, _) => AtualizarComissao();
        AtualizarComissao();

        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16) };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var linha = 0;
        AdicionarLinha(tabela, linha++, "Cliente:", _campoCliente);
        AdicionarLinha(tabela, linha++, "Nota fiscal:", _campoNotaFiscal);
        AdicionarLinha(tabela, linha++, "Nº boleto:", _campoBoleto);
        AdicionarLinha(tabela, linha++, "Ref. pedido:", _campoPedido);
        AdicionarLinha(tabela, linha++, "Valor da NF (R$):", _campoValorNf);
        AdicionarLinha(tabela, linha++, "Vencimento:", _campoVencimento);
        AdicionarLinha(tabela, linha++, "Parcela:", _campoParcela);
        AdicionarLinha(tabela, linha++, "Valor da parcela (R$):", _campoValorParcela);
        AdicionarLinha(tabela, linha++, "Comissão (%):", _campoPercentual);
        AdicionarLinha(tabela, linha++, "Comissão (R$):", _rotuloComissao);
        AdicionarLinha(tabela, linha++, "Status da comissão:", _campoStatus);
        AdicionarLinha(tabela, linha++, "Pag. comissão:", _campoDataPagamento);

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();
        var painelBotoes = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(painelBotoes, 1, linha);
        tabela.RowCount = linha + 1;

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private void AtualizarComissao() =>
        _rotuloComissao.Text = (_campoValorParcela.Value * _campoPercentual.Value / 100m).ToString("C2", CulturaBr);

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoCliente.Text))
        {
            MessageBox.Show(this, "Informe o cliente.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Comissao.Cliente = _campoCliente.Text.Trim();
        Comissao.NotaFiscal = _campoNotaFiscal.Text.Trim();
        Comissao.NumeroBoleto = _campoBoleto.Text.Trim();
        Comissao.RefPedido = _campoPedido.Text.Trim();
        Comissao.ValorNf = _campoValorNf.Value;
        Comissao.Vencimento = _campoVencimento.Value.Date;
        Comissao.Parcela = _campoParcela.Text.Trim();
        Comissao.ValorParcela = _campoValorParcela.Value;
        Comissao.Percentual = _campoPercentual.Value;
        Comissao.Paga = _campoStatus.SelectedIndex == 1;
        Comissao.DataPagamento = Comissao.Paga && _campoDataPagamento.Checked ? _campoDataPagamento.Value.Date : null;

        DialogResult = DialogResult.OK;
        Close();
    }

    // --- Peças reaproveitadas pelo formulário de nova venda -----------------

    internal static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        campo.Margin = new Padding(0, 4, 0, 4);
        tabela.Controls.Add(campo, 1, linha);
    }

    internal static NumericUpDown CriarCampoValor(decimal valor) => new()
    {
        Dock = DockStyle.Fill,
        DecimalPlaces = 2,
        ThousandsSeparator = true,
        Minimum = 0m,
        Maximum = 1_000_000_000m,
        Value = Math.Clamp(valor, 0m, 1_000_000_000m),
    };

    internal static NumericUpDown CriarCampoPercentual(decimal valor) => new()
    {
        Dock = DockStyle.Fill,
        DecimalPlaces = 2,
        Increment = 0.5m,
        Minimum = 0m,
        Maximum = 100m,
        Value = Math.Clamp(valor, 0m, 100m),
    };

    /// <summary>
    /// Autocompletar do cliente: nomes já usados nas comissões + razão social e
    /// nome fantasia do cadastro de Clientes (também MILITARI).
    /// </summary>
    internal static void ConfigurarSugestoesCliente(TextBox campo)
    {
        var nomes = new HashSet<string>(ComissaoRepository.ListarClientes(), StringComparer.OrdinalIgnoreCase);
        foreach (var cliente in ClienteRepository.Listar())
        {
            if (cliente.RazaoSocial.Trim().Length > 0) nomes.Add(cliente.RazaoSocial.Trim());
            if (cliente.NomeFantasia.Trim().Length > 0) nomes.Add(cliente.NomeFantasia.Trim());
        }
        var sugestoes = new AutoCompleteStringCollection();
        sugestoes.AddRange(nomes.ToArray());
        campo.AutoCompleteCustomSource = sugestoes;
        campo.AutoCompleteSource = AutoCompleteSource.CustomSource;
        campo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
    }
}
