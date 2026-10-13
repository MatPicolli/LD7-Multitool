using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Lança uma venda (NF) e gera de uma vez as linhas de todas as parcelas —
/// o que na planilha era digitado/copiado linha a linha. Mostra a prévia antes
/// de gravar.
/// </summary>
public class NovaVendaForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly TextBox _campoCliente;
    private readonly TextBox _campoNotaFiscal;
    private readonly TextBox _campoPedido;
    private readonly NumericUpDown _campoValorNf;
    private readonly NumericUpDown _campoQuantidade;
    private readonly DateTimePicker _campoPrimeiroVencimento;
    private readonly ComboBox _campoIntervalo;
    private readonly TextBox _campoPrimeiroBoleto;
    private readonly NumericUpDown _campoPercentual;
    private readonly DataGridView _previa;
    private readonly Label _totalPrevia;

    /// <summary>As parcelas geradas (preenchido ao confirmar).</summary>
    public List<Comissao> Parcelas { get; private set; } = new();

    public NovaVendaForm()
    {
        Text = "Nova venda — gerar parcelas";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MinimumSize = new Size(920, 560);
        ClientSize = new Size(940, 560);

        // --- Campos (coluna da esquerda) -------------------------------------
        _campoCliente = new TextBox { Dock = DockStyle.Fill };
        ComissaoForm.ConfigurarSugestoesCliente(_campoCliente);
        _campoNotaFiscal = new TextBox { Dock = DockStyle.Fill };
        _campoPedido = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "número, SEM PEDIDO..." };
        _campoValorNf = ComissaoForm.CriarCampoValor(0m);
        _campoQuantidade = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1, Maximum = 120, Value = 1 };
        _campoPrimeiroVencimento = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short };
        _campoIntervalo = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
        };
        foreach (var (descricao, _) in GeradorParcelas.Intervalos)
            _campoIntervalo.Items.Add(descricao);
        _campoIntervalo.SelectedIndex = Array.FindIndex(GeradorParcelas.Intervalos, i => i.Dias == 30);
        _campoPrimeiroBoleto = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "ex.: 364-6 ou PIX" };
        _campoPercentual = ComissaoForm.CriarCampoPercentual(ComissoesConfigForm.PercentualPadrao);

        var campos = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0, 0, 16, 0) };
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        campos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var linha = 0;
        ComissaoForm.AdicionarLinha(campos, linha++, "Cliente *", _campoCliente);
        ComissaoForm.AdicionarLinha(campos, linha++, "Nota fiscal", _campoNotaFiscal);
        ComissaoForm.AdicionarLinha(campos, linha++, "Ref. pedido", _campoPedido);
        ComissaoForm.AdicionarLinha(campos, linha++, "Valor da NF (R$) *", _campoValorNf);
        ComissaoForm.AdicionarLinha(campos, linha++, "Nº de parcelas", _campoQuantidade);
        ComissaoForm.AdicionarLinha(campos, linha++, "1º vencimento", _campoPrimeiroVencimento);
        ComissaoForm.AdicionarLinha(campos, linha++, "Intervalo", _campoIntervalo);
        ComissaoForm.AdicionarLinha(campos, linha++, "Nº do 1º boleto", _campoPrimeiroBoleto);
        ComissaoForm.AdicionarLinha(campos, linha++, "Comissão (%)", _campoPercentual);
        campos.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        var ajuda = new Label
        {
            Text = "Boletos no formato 364-6 seguem em sequência (364-7, 364-8, 364-9, 365-0...). " +
                   "Outro texto, como PIX, se repete em todas as parcelas. " +
                   "O valor é dividido nos centavos e a última parcela leva a diferença.",
            Dock = DockStyle.Fill,
            ForeColor = Estilo.CorTextoSuave,
            Padding = new Padding(0, 8, 0, 0),
        };
        campos.Controls.Add(ajuda, 0, linha);
        campos.SetColumnSpan(ajuda, 2);
        linha++;
        campos.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        campos.RowCount = linha + 1;

        // --- Prévia (coluna da direita) --------------------------------------
        _previa = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };
        Estilo.EstilizarGrade(_previa);
        _previa.Columns.Add("parcela", "Parcela");
        _previa.Columns.Add("boleto", "Nº boleto");
        _previa.Columns.Add("vencimento", "Vencimento");
        _previa.Columns.Add("valor", "Valor parcela");
        _previa.Columns.Add("comissao", "Comissão");
        foreach (DataGridViewColumn coluna in _previa.Columns)
            coluna.SortMode = DataGridViewColumnSortMode.NotSortable;
        foreach (var nome in new[] { "valor", "comissao" })
        {
            _previa.Columns[nome]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            _previa.Columns[nome]!.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
        }

        var tituloPrevia = new Label
        {
            Text = "Prévia das parcelas",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Segoe UI Semibold", 10.5f),
            ForeColor = Estilo.CorPrimaria,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _totalPrevia = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTexto,
        };
        var painelPrevia = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        painelPrevia.Controls.Add(_previa);
        painelPrevia.Controls.Add(_totalPrevia);
        painelPrevia.Controls.Add(tituloPrevia);

        var conteudo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 12, 16, 0),
        };
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 430));
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        conteudo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        conteudo.Controls.Add(campos, 0, 0);
        conteudo.Controls.Add(painelPrevia, 1, 0);

        // --- Botões -----------------------------------------------------------
        var botaoSalvar = Estilo.BotaoPrimario("Salvar parcelas");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(conteudo);
        Controls.Add(painelBotoes);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;

        // Qualquer mudança refaz a prévia.
        _campoValorNf.ValueChanged += (_, _) => AtualizarPrevia();
        _campoQuantidade.ValueChanged += (_, _) => AtualizarPrevia();
        _campoPrimeiroVencimento.ValueChanged += (_, _) => AtualizarPrevia();
        _campoIntervalo.SelectedIndexChanged += (_, _) => AtualizarPrevia();
        _campoPrimeiroBoleto.TextChanged += (_, _) => AtualizarPrevia();
        _campoPercentual.ValueChanged += (_, _) => AtualizarPrevia();
        AtualizarPrevia();
    }

    private List<Comissao> GerarParcelas()
    {
        var modelo = new Comissao
        {
            Cliente = _campoCliente.Text.Trim(),
            NotaFiscal = _campoNotaFiscal.Text.Trim(),
            RefPedido = _campoPedido.Text.Trim(),
            ValorNf = _campoValorNf.Value,
            Percentual = _campoPercentual.Value,
        };
        var dias = GeradorParcelas.Intervalos[Math.Max(0, _campoIntervalo.SelectedIndex)].Dias;
        return GeradorParcelas.Gerar(modelo, (int)_campoQuantidade.Value,
            _campoPrimeiroVencimento.Value.Date, dias, _campoPrimeiroBoleto.Text);
    }

    private void AtualizarPrevia()
    {
        // Intervalo só faz sentido com mais de uma parcela.
        _campoIntervalo.Enabled = _campoQuantidade.Value > 1;

        var parcelas = GerarParcelas();
        _previa.Rows.Clear();
        foreach (var p in parcelas)
        {
            _previa.Rows.Add(
                p.Parcela,
                p.NumeroBoleto,
                p.Vencimento.ToString("dd/MM/yyyy", CulturaBr),
                p.ValorParcela.ToString("C2", CulturaBr),
                p.ValorComissao.ToString("C2", CulturaBr));
        }
        _previa.ClearSelection();
        _totalPrevia.Text =
            $"{parcelas.Count} parcela(s) — soma {parcelas.Sum(p => p.ValorParcela).ToString("C2", CulturaBr)}" +
            $" — comissão total {parcelas.Sum(p => p.ValorComissao).ToString("C2", CulturaBr)}";
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoCliente.Text))
        {
            MessageBox.Show(this, "Informe o cliente.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _campoCliente.Focus();
            return;
        }
        // Ler .Value confirma o que estiver digitado no campo (Enter direto do valor).
        if (_campoValorNf.Value <= 0m)
        {
            MessageBox.Show(this, "Informe o valor da NF.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _campoValorNf.Focus();
            return;
        }

        Parcelas = GerarParcelas();
        DialogResult = DialogResult.OK;
        Close();
    }
}
