using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Executa a coleta automática dos itens e mostra, item a item, o que foi
/// encontrado. Roda em segundo plano para a janela não travar e pode ser
/// interrompida — um portal fora do ar não pode prender o programa.
/// </summary>
public class ColetaForm : Form
{
    private readonly List<Despesa> _despesas;
    private readonly DataGridView _grade;
    private readonly Label _situacao;
    private readonly Button _botaoFechar;
    private readonly CancellationTokenSource _cancelamento = new();

    /// <summary>Quantas contas novas entraram (a tela principal recarrega se houver alguma).</summary>
    public int TotalNovos { get; private set; }

    public ColetaForm(List<Despesa> despesas)
    {
        _despesas = despesas;

        Text = "Buscando as contas do mês";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(860, 480);
        ClientSize = new Size(860, 480);

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
        _grade.Columns.Add("despesa", "Item");
        _grade.Columns.Add("metodo", "Método");
        _grade.Columns.Add("detalhe", "Resultado");
        _grade.Columns["despesa"]!.FillWeight = 90;
        _grade.Columns["metodo"]!.FillWeight = 50;
        _grade.Columns["detalhe"]!.FillWeight = 120;

        _situacao = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Estilo.CorSuperficie,
            ForeColor = Estilo.CorTextoSuave,
            Text = $"Preparando a busca de {despesas.Count} item(ns)...",
        };

        _botaoFechar = Estilo.BotaoPadrao("Cancelar");
        _botaoFechar.Click += (_, _) => FecharOuCancelar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(_botaoFechar);

        Controls.Add(_grade);
        Controls.Add(painelBotoes);
        Controls.Add(_situacao);

        CancelButton = _botaoFechar;
        Shown += async (_, _) => await ExecutarAsync();
        FormClosing += (_, _) => _cancelamento.Cancel();
    }

    private async Task ExecutarAsync()
    {
        var progresso = new Progress<string>(mensagem => _situacao.Text = mensagem);
        List<ResultadoColeta> resultados;

        try
        {
            resultados = await Task.Run(
                () => ServicoColeta.ColetarAsync(_despesas, progresso, _cancelamento.Token),
                _cancelamento.Token);
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _situacao.Text = "Busca cancelada.";
                _botaoFechar.Text = "Fechar";
            }
            return;
        }

        // A janela pode ter sido fechada enquanto a busca rodava.
        if (IsDisposed)
            return;

        foreach (var resultado in resultados)
        {
            var indice = _grade.Rows.Add(resultado.Despesa, resultado.Metodo, resultado.Detalhe);
            var linha = _grade.Rows[indice];
            if (resultado.Erro)
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
            else if (resultado.Novos > 0)
                linha.DefaultCellStyle.ForeColor = Color.FromArgb(30, 130, 76);
            else
                linha.DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
        }

        TotalNovos = resultados.Sum(r => r.Novos);
        var comErro = resultados.Count(r => r.Erro);
        _situacao.Text =
            $"Busca concluída — {TotalNovos} conta(s) nova(s) em {resultados.Count} item(ns)" +
            (comErro > 0 ? $"; {comErro} com erro (linhas em vermelho)." : ".");
        _botaoFechar.Text = "Fechar";
    }

    private void FecharOuCancelar()
    {
        _cancelamento.Cancel();
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancelamento.Dispose();
        base.Dispose(disposing);
    }
}
