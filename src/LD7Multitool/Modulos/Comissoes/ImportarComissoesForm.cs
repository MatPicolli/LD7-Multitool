using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Prévia e confirmação da importação de uma aba da planilha de comissões.
/// Escolhe-se a aba no topo; cada linha mostra se será importada ou pulada.
/// </summary>
public class ImportarComissoesForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private const string SituacaoNova = "Nova";

    private sealed record Candidata(int Linha, Comissao? Comissao, string Situacao)
    {
        public bool Importavel => Comissao is not null && Situacao == SituacaoNova;
    }

    private readonly LeitorXlsx _leitor;
    private readonly decimal _percentualPadrao;
    private readonly HashSet<string> _chavesExistentes;
    private readonly ComboBox _campoAba;
    private readonly Label _resumo;
    private readonly DataGridView _grade;
    private readonly Button _botaoImportar;
    private List<Candidata> _candidatas = new();

    public int ImportadasCount { get; private set; }
    public int PuladasCount { get; private set; }

    public ImportarComissoesForm(LeitorXlsx leitor, string nomeArquivo, decimal percentualPadrao)
    {
        _leitor = leitor;
        _percentualPadrao = percentualPadrao;
        _chavesExistentes = ComissaoRepository.Listar().Select(c => c.ChaveImportacao).ToHashSet();

        Text = $"Importar comissões — {nomeArquivo}";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MinimumSize = new Size(900, 560);
        ClientSize = new Size(1040, 620);

        // --- Topo: escolha da aba + resumo ---------------------------------------
        _campoAba = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 12, 0),
        };
        foreach (var aba in _leitor.Abas)
            _campoAba.Items.Add(aba);

        _resumo = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Estilo.CorTexto };

        var topo = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(12, 10, 12, 6),
        };
        topo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        topo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        topo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        topo.Controls.Add(new Label
        {
            Text = "Aba:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, 0);
        topo.Controls.Add(_campoAba, 1, 0);
        topo.Controls.Add(_resumo, 2, 0);

        // --- Prévia ---------------------------------------------------------------
        _grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
        };
        Estilo.EstilizarGrade(_grade);
        _grade.Columns.Add("situacao", "Situação");
        _grade.Columns.Add("linha", "Linha");
        _grade.Columns.Add("cliente", "Cliente");
        _grade.Columns.Add("nf", "Nota fiscal");
        _grade.Columns.Add("boleto", "Nº boleto");
        _grade.Columns.Add("parcela", "Parcela");
        _grade.Columns.Add("vencimento", "Vencimento");
        _grade.Columns.Add("valor", "Valor parcela");
        _grade.Columns.Add("comissao", "Comissão");
        _grade.Columns.Add("status", "Status");
        _grade.Columns["situacao"]!.FillWeight = 120;
        _grade.Columns["linha"]!.FillWeight = 35;
        _grade.Columns["cliente"]!.FillWeight = 150;
        foreach (DataGridViewColumn coluna in _grade.Columns)
            coluna.SortMode = DataGridViewColumnSortMode.NotSortable;

        var painelGrade = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12) };
        painelGrade.Controls.Add(_grade);

        // --- Botões ------------------------------------------------------------
        _botaoImportar = Estilo.BotaoPrimario("Importar");
        _botaoImportar.Click += (_, _) => Importar();
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(_botaoImportar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(painelGrade);
        Controls.Add(painelBotoes);
        Controls.Add(topo);
        CancelButton = botaoCancelar;

        _campoAba.SelectedIndexChanged += (_, _) => CarregarAba();
        if (_campoAba.Items.Count > 0)
            _campoAba.SelectedIndex = 0;
        else
            MostrarErro("A planilha não tem abas.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _leitor.Dispose();
        base.Dispose(disposing);
    }

    private void CarregarAba()
    {
        _candidatas = new List<Candidata>();
        _grade.Rows.Clear();

        List<LinhaPlanilha>? linhas;
        string? erro;
        try
        {
            var aba = _leitor.LerAba((string)_campoAba.SelectedItem!);
            (linhas, erro) = PlanilhaComissoes.Ler(aba, _percentualPadrao);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Xml.XmlException)
        {
            (linhas, erro) = (null, "Não foi possível ler esta aba: " + ex.Message);
        }

        if (linhas is null)
        {
            MostrarErro(erro ?? "Aba sem dados.");
            return;
        }

        // Situação de cada linha: problema de leitura, já cadastrada, repetida
        // dentro da própria aba, ou nova.
        var chavesDestaAba = new HashSet<string>();
        foreach (var linha in linhas)
        {
            string situacao;
            if (linha.Comissao is null)
                situacao = (linha.Problema ?? "Inválida") + " — pulada";
            else if (_chavesExistentes.Contains(linha.Comissao.ChaveImportacao))
                situacao = "Já cadastrada — pulada";
            else if (!chavesDestaAba.Add(linha.Comissao.ChaveImportacao))
                situacao = "Repetida na aba — pulada";
            else
                situacao = SituacaoNova;
            _candidatas.Add(new Candidata(linha.NumeroLinha, linha.Comissao, situacao));
        }

        foreach (var candidata in _candidatas)
        {
            var c = candidata.Comissao;
            var indice = _grade.Rows.Add(
                candidata.Situacao,
                candidata.Linha,
                c?.Cliente ?? "",
                c?.NotaFiscal ?? "",
                c?.NumeroBoleto ?? "",
                c?.Parcela ?? "",
                c?.Vencimento.ToString("dd/MM/yyyy", CulturaBr) ?? "",
                c?.ValorParcela.ToString("C2", CulturaBr) ?? "",
                c?.ValorComissao.ToString("C2", CulturaBr) ?? "",
                c?.StatusDescricao ?? "");
            if (!candidata.Importavel)
                _grade.Rows[indice].DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
        }
        _grade.ClearSelection();

        var novas = _candidatas.Where(c => c.Importavel).Select(c => c.Comissao!).ToList();
        var pago = novas.Where(c => c.Paga).Sum(c => c.ValorComissao);
        var total = novas.Sum(c => c.ValorComissao);
        _resumo.ForeColor = Estilo.CorTexto;
        _resumo.Text = $"{_candidatas.Count} linha(s): {novas.Count} nova(s), {_candidatas.Count - novas.Count} pulada(s)" +
            $"   •   comissão {total.ToString("C2", CulturaBr)} (paga {pago.ToString("C2", CulturaBr)}, " +
            $"falta {(total - pago).ToString("C2", CulturaBr)})";
        _botaoImportar.Text = $"Importar {novas.Count} parcela(s)";
        _botaoImportar.Enabled = novas.Count > 0;
    }

    private void MostrarErro(string mensagem)
    {
        _resumo.ForeColor = Estilo.CorPerigo;
        _resumo.Text = mensagem;
        _botaoImportar.Text = "Importar";
        _botaoImportar.Enabled = false;
    }

    private void Importar()
    {
        var novas = _candidatas.Where(c => c.Importavel).Select(c => c.Comissao!).ToList();
        ComissaoRepository.InserirVarias(novas);
        ImportadasCount = novas.Count;
        PuladasCount = _candidatas.Count - novas.Count;
        DialogResult = DialogResult.OK;
        Close();
    }
}
