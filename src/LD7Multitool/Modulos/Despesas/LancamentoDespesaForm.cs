using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Lançamento de uma conta (o boleto/fatura de um mês) de um item de despesa.
///
/// O botão "Ler de um PDF..." reaproveita o leitor de boletos do módulo Boletos:
/// valor e vencimento saem da linha digitável, sem digitação.
/// </summary>
public class LancamentoDespesaForm : Form
{
    private const int AlturaLinha = 32;

    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly TextBox _campoCompetencia;
    private readonly DateTimePicker _campoVencimento;
    private readonly TextBox _campoValor;
    private readonly TextBox _campoLinhaDigitavel;
    private readonly ComboBox _campoSituacao;
    private readonly TextBox _campoArquivo;

    public LancamentoDespesa Lancamento { get; }

    public LancamentoDespesaForm(Despesa despesa, LancamentoDespesa? lancamento = null)
    {
        var novo = lancamento is null;
        Lancamento = lancamento ?? new LancamentoDespesa
        {
            DespesaId = despesa.Id,
            Vencimento = ProximoVencimento(despesa),
        };
        if (Lancamento.Competencia.Length == 0)
            Lancamento.Competencia = ServicoColeta.CompetenciaDe(Lancamento.Vencimento);

        Text = (novo ? "Lançar conta — " : "Editar conta — ") + despesa.Nome;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 330);

        _campoCompetencia = new TextBox { Text = Lancamento.CompetenciaFormatada, PlaceholderText = "MM/aaaa" };
        _campoVencimento = new DateTimePicker { Format = DateTimePickerFormat.Short, Value = Lancamento.Vencimento };
        _campoValor = new TextBox { Text = Lancamento.Valor.ToString("0.00", CulturaBr) };
        _campoLinhaDigitavel = new TextBox { Text = Lancamento.LinhaDigitavel };

        _campoSituacao = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        foreach (SituacaoDespesa situacao in Enum.GetValues<SituacaoDespesa>())
            _campoSituacao.Items.Add(situacao.Descricao());
        _campoSituacao.SelectedIndex = (int)Lancamento.Situacao;

        _campoArquivo = new TextBox { Text = Lancamento.CaminhoArquivo, ReadOnly = true, BackColor = Estilo.CorFundo };

        var botaoLerPdf = Estilo.BotaoPadrao("Ler de um PDF...");
        botaoLerPdf.Click += (_, _) => LerDePdf();

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(20, 14, 20, 8),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var linhas = new (string Rotulo, Control Campo)[]
        {
            ("Competência", _campoCompetencia),
            ("Vencimento", _campoVencimento),
            ("Valor (R$)", _campoValor),
            ("Linha digitável", _campoLinhaDigitavel),
            ("Situação", _campoSituacao),
            ("Arquivo (PDF)", ComBotao(_campoArquivo, botaoLerPdf)),
        };
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
                MinimumSize = new Size(120, 0),
            }, 0, i);

            linhas[i].Campo.Dock = DockStyle.Fill;
            linhas[i].Campo.Margin = new Padding(0, 3, 0, 3);
            tabela.Controls.Add(linhas[i].Campo, 1, i);
        }
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tabela.RowCount = linhas.Length + 1;

        var botaoSalvar = Estilo.BotaoPrimario("Gravar");
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

        Controls.Add(tabela);
        Controls.Add(painelBotoes);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    /// <summary>Vencimento sugerido: o dia cadastrado no item, no mês corrente ou no próximo.</summary>
    private static DateTime ProximoVencimento(Despesa despesa)
    {
        if (despesa.DiaVencimento is < 1 or > 31)
            return DateTime.Today;

        var hoje = DateTime.Today;
        var dia = Math.Min(despesa.DiaVencimento, DateTime.DaysInMonth(hoje.Year, hoje.Month));
        var data = new DateTime(hoje.Year, hoje.Month, dia);
        if (data >= hoje)
            return data;

        var proximo = hoje.AddMonths(1);
        return new DateTime(proximo.Year, proximo.Month,
            Math.Min(despesa.DiaVencimento, DateTime.DaysInMonth(proximo.Year, proximo.Month)));
    }

    private void LerDePdf()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Escolha o PDF do boleto",
            Filter = "Arquivos PDF (*.pdf)|*.pdf",
            InitialDirectory = Directory.Exists(DespesasConfigForm.PastaDownloads)
                ? DespesasConfigForm.PastaDownloads
                : "",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        // Reaproveita o leitor do módulo Boletos: um PDF pode ter várias
        // parcelas, mas aqui interessa a primeira (uma conta por lançamento).
        var boleto = Boletos.LeitorBoletoPdf.Ler(dialogo.FileName).FirstOrDefault();
        _campoArquivo.Text = dialogo.FileName;

        if (boleto is null || boleto.Valor == 0)
        {
            MessageBox.Show(this,
                "Não foi possível ler a linha digitável deste PDF (pode ser um documento " +
                "escaneado ou uma fatura só informativa).\nO arquivo foi vinculado — preencha " +
                "valor e vencimento à mão.",
                "Leitura incompleta", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _campoValor.Text = boleto.Valor.ToString("0.00", CulturaBr);
        _campoVencimento.Value = boleto.Validade;
        _campoCompetencia.Text = DateTime.ParseExact(
            ServicoColeta.CompetenciaDe(boleto.Validade), "yyyy-MM",
            CultureInfo.InvariantCulture).ToString("MM/yyyy");
    }

    private void Salvar()
    {
        if (!decimal.TryParse(_campoValor.Text, NumberStyles.Currency, CulturaBr, out var valor) || valor < 0)
        {
            MessageBox.Show(this, "Informe um valor válido (ex.: 189,90).", "Valor inválido",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _campoValor.Focus();
            return;
        }

        Lancamento.Vencimento = _campoVencimento.Value.Date;
        Lancamento.Valor = valor;
        Lancamento.LinhaDigitavel = Febraban.SomenteDigitos(_campoLinhaDigitavel.Text);
        Lancamento.Situacao = (SituacaoDespesa)_campoSituacao.SelectedIndex;
        Lancamento.CaminhoArquivo = _campoArquivo.Text.Trim();
        Lancamento.Competencia = CompetenciaDigitada() ?? ServicoColeta.CompetenciaDe(Lancamento.Vencimento);

        DialogResult = DialogResult.OK;
        Close();
    }

    /// <summary>Lê a competência digitada como MM/aaaa; null se estiver vazia ou inválida.</summary>
    private string? CompetenciaDigitada()
    {
        var texto = _campoCompetencia.Text.Trim();
        return DateTime.TryParseExact(texto, new[] { "MM/yyyy", "M/yyyy", "yyyy-MM" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var mes)
            ? mes.ToString("yyyy-MM")
            : null;
    }

    private static Control ComBotao(Control campo, Button botao)
    {
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        campo.Dock = DockStyle.Fill;
        campo.Margin = new Padding(0, 2, 0, 2);
        // Ver DespesasConfigForm: AutoSize e Dock.Fill juntos brigam pelo tamanho.
        botao.AutoSize = false;
        botao.Dock = DockStyle.Fill;
        botao.Margin = new Padding(6, 0, 0, 0);
        painel.Controls.Add(campo, 0, 0);
        painel.Controls.Add(botao, 1, 0);
        return painel;
    }
}
