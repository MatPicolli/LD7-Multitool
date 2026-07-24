using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>
/// Mostra, lado a lado, os dados que cada fonte de CNPJ retornou, com o tempo
/// de resposta de cada uma e a concordância (semelhança) por campo. Deixa o
/// usuário preencher o cadastro com os dados consolidados (voto da maioria) ou
/// com uma fonte específica.
/// </summary>
public class ComparativoCnpjForm : Form
{
    private static readonly Color CorDivergencia = Color.FromArgb(255, 244, 244);

    private readonly List<RespostaFonte> _respostas;
    private readonly List<RespostaFonte> _comSucesso;
    private readonly DadosCnpj _consolidado;
    private readonly ComboBox _selecaoFonte;

    /// <summary>Dados escolhidos pelo usuário (null se cancelou).</summary>
    public DadosCnpj? DadosEscolhidos { get; private set; }

    public ComparativoCnpjForm(string cnpj, IReadOnlyList<RespostaFonte> respostas)
    {
        _respostas = respostas.ToList();
        _comSucesso = _respostas.Where(r => r.Dados is not null).ToList();
        var dadosSucesso = _comSucesso.Select(r => r.Dados!).ToList();
        _consolidado = ServicoCnpj.DadosConsolidados(dadosSucesso);
        var campos = ServicoCnpj.Consolidar(dadosSucesso);

        Text = $"Comparativo de CNPJ — {cnpj}";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(940, 560);
        ClientSize = new Size(1040, 620);

        // --- Resumo (topo) ---------------------------------------------------
        var comValor = campos.Where(c => c.Total > 0).ToList();
        var similaridade = comValor.Count == 0 ? 0 : comValor.Average(c => (double)c.Concordam / c.Total);
        var maisRapida = _comSucesso.OrderBy(r => r.TempoMs).FirstOrDefault();

        var resumo = new Label
        {
            Dock = DockStyle.Top,
            Height = 52,
            Padding = new Padding(16, 10, 16, 0),
            Text =
                $"Concordância média entre as fontes: {similaridade:P0}      " +
                $"{_comSucesso.Count} de {_respostas.Count} fontes responderam" +
                (maisRapida is null ? "" : $"      Mais rápida: {maisRapida.Fonte} ({maisRapida.TempoMs} ms)"),
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Estilo.CorTexto,
        };

        // --- Grade -----------------------------------------------------------
        var grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            Margin = new Padding(12),
        };
        Estilo.EstilizarGrade(grade);
        grade.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grade.ColumnHeadersHeight = 54;
        grade.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grade.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

        grade.Columns.Add(NovaColuna("campo", "Campo", 90));
        for (var i = 0; i < _respostas.Count; i++)
        {
            var r = _respostas[i];
            var titulo = r.Dados is not null
                ? $"{r.Fonte}\n{r.TempoMs} ms"
                : $"{r.Fonte}\n(erro)";
            var coluna = NovaColuna("fonte" + i, titulo, 100);
            coluna.ToolTipText = r.Erro is null ? $"Respondeu em {r.TempoMs} ms" : "Erro: " + r.Erro;
            grade.Columns.Add(coluna);
        }
        grade.Columns.Add(NovaColuna("consolidado", "Consolidado\n(maioria)", 105));
        grade.Columns.Add(NovaColuna("concordancia", "Concord.", 55));

        var colConsolidado = grade.Columns[grade.Columns.Count - 2]!;
        colConsolidado.DefaultCellStyle.Font = new Font(Estilo.FontePadrao, FontStyle.Bold);
        colConsolidado.DefaultCellStyle.BackColor = Color.FromArgb(244, 247, 255);

        for (var linha = 0; linha < ServicoCnpj.Campos.Length; linha++)
        {
            var (rotulo, seletor) = ServicoCnpj.Campos[linha];
            var valorConsolidado = campos[linha].Valor;

            var celulas = new List<string> { rotulo };
            foreach (var r in _respostas)
                celulas.Add(r.Dados is null ? "" : seletor(r.Dados));
            celulas.Add(valorConsolidado);
            celulas.Add(campos[linha].Total == 0 ? "—" : $"{campos[linha].Concordam}/{campos[linha].Total}");

            var indice = grade.Rows.Add(celulas.Cast<object>().ToArray());
            var linhaGrade = grade.Rows[indice];

            // Rótulo em cinza; realça em vermelho-claro as fontes que divergem da maioria.
            linhaGrade.Cells[0].Style.ForeColor = Estilo.CorTextoSuave;
            var normalConsolidado = ServicoCnpj.Normalizar(valorConsolidado);
            for (var i = 0; i < _respostas.Count; i++)
            {
                if (_respostas[i].Dados is null)
                    continue;
                var valorFonte = seletor(_respostas[i].Dados!);
                if (valorFonte.Trim().Length > 0 && ServicoCnpj.Normalizar(valorFonte) != normalConsolidado)
                {
                    var celula = linhaGrade.Cells[i + 1];
                    celula.Style.BackColor = CorDivergencia;
                    celula.Style.ForeColor = Estilo.CorPerigo;
                }
            }
        }

        var painelGrade = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
        painelGrade.Controls.Add(grade);

        // --- Barra inferior --------------------------------------------------
        _selecaoFonte = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Width = 260,
            Margin = new Padding(8, 4, 0, 0),
        };
        _selecaoFonte.Items.Add("Consolidado (recomendado)");
        foreach (var r in _comSucesso)
            _selecaoFonte.Items.Add($"Somente {r.Fonte}");
        _selecaoFonte.SelectedIndex = 0;

        var rotuloPreencher = new Label
        {
            Text = "Preencher com:",
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0),
        };

        var botaoUsar = Estilo.BotaoPrimario("Usar estes dados");
        botaoUsar.Enabled = _comSucesso.Count > 0;
        botaoUsar.Click += (_, _) => Confirmar();
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;

        var painelEsquerda = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            Padding = new Padding(4, 0, 0, 0),
        };
        painelEsquerda.Controls.Add(rotuloPreencher);
        painelEsquerda.Controls.Add(_selecaoFonte);

        var painelDireita = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            WrapContents = false,
        };
        painelDireita.Controls.Add(botaoUsar);
        painelDireita.Controls.Add(botaoCancelar);

        var painelBotoes = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(painelDireita);
        painelBotoes.Controls.Add(painelEsquerda);

        Controls.Add(painelGrade);
        Controls.Add(painelBotoes);
        Controls.Add(resumo);

        AcceptButton = botaoUsar;
        CancelButton = botaoCancelar;
    }

    private static DataGridViewTextBoxColumn NovaColuna(string nome, string titulo, int peso) => new()
    {
        Name = nome,
        HeaderText = titulo,
        FillWeight = peso,
        SortMode = DataGridViewColumnSortMode.NotSortable,
    };

    private void Confirmar()
    {
        DadosEscolhidos = _selecaoFonte.SelectedIndex <= 0
            ? _consolidado
            : _comSucesso[_selecaoFonte.SelectedIndex - 1].Dados;
        DialogResult = DialogResult.OK;
        Close();
    }
}
