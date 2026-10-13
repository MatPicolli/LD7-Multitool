using System.ComponentModel;
using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Tela de comissões MILITARI — a aba "2026 (NOVA MILITAR)" da planilha
/// "Comissão": uma linha por parcela, comissão de 10%, status de pagamento e os
/// totais (comissão total, paga, falta pagar).
/// </summary>
public class ComissoesControl : UserControl
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private const string FiltroTodos = "Todos";
    private const string FiltroNaoPago = "Não pago";
    private const string FiltroPago = "Pago";
    private const string FiltroVencidas = "Vencidas não pagas";
    private const string TodosOsAnos = "Todos os anos";

    private readonly DataGridView _grade;
    private readonly TextBox _campoPesquisa;
    private readonly ComboBox _filtroAno;
    private readonly ComboBox _filtroStatus;
    private readonly Label _valorTotal;
    private readonly Label _valorPago;
    private readonly Label _valorFalta;
    private readonly Label _valorVencidas;
    private readonly Label _resumo;
    private List<Comissao> _todos = new();
    private List<Comissao> _visiveis = new();

    // "Nº" da planilha: posição da parcela na ordem de vencimento de TODAS as
    // linhas — não muda ao filtrar.
    private Dictionary<long, int> _numeroPorId = new();

    public ComissoesControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior --------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoNova = Estilo.BotaoPrimario("+ Nova venda");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoPago = Estilo.BotaoPadrao("Marcar como pago");
        var botaoNaoPago = Estilo.BotaoPadrao("Marcar como não pago");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        var botaoConfig = Estilo.BotaoIcone("⚙", "Configurações (importar planilha, % padrão)");

        botaoNova.Click += (_, _) => NovaVenda();
        botaoEditar.Click += (_, _) => Editar();
        botaoPago.Click += (_, _) => MarcarPago();
        botaoNaoPago.Click += (_, _) => MarcarNaoPago();
        botaoExcluir.Click += (_, _) => Excluir();
        botaoConfig.Click += (_, _) => AbrirConfiguracoes();

        var fluxoAcoes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0) };
        fluxoAcoes.Controls.Add(botaoNova);
        fluxoAcoes.Controls.Add(botaoEditar);
        fluxoAcoes.Controls.Add(botaoPago);
        fluxoAcoes.Controls.Add(botaoNaoPago);
        fluxoAcoes.Controls.Add(botaoExcluir);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfig.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfig);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Barra de filtro (pesquisa + ano + status) ------------------------
        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Pesquisar por cliente, NF, boleto, pedido, valor ou data...",
            Margin = new Padding(0),
        };
        _campoPesquisa.TextChanged += (_, _) => AtualizarGrade();

        _filtroAno = CriarCombo();
        _filtroAno.Items.Add(TodosOsAnos);
        _filtroAno.SelectedIndex = 0;
        _filtroAno.SelectedIndexChanged += (_, _) => AtualizarGrade();

        _filtroStatus = CriarCombo();
        _filtroStatus.Items.AddRange(new object[] { FiltroTodos, FiltroNaoPago, FiltroPago, FiltroVencidas });
        _filtroStatus.SelectedIndex = 0;
        _filtroStatus.SelectedIndexChanged += (_, _) => AtualizarGrade();

        var barraFiltro = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 6, 12, 6),
        };
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        barraFiltro.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        barraFiltro.Controls.Add(CriarRotuloFiltro("Pesquisar:", ContentAlignment.MiddleLeft), 0, 0);
        barraFiltro.Controls.Add(_campoPesquisa, 1, 0);
        barraFiltro.Controls.Add(CriarRotuloFiltro("Ano:", ContentAlignment.MiddleRight), 2, 0);
        barraFiltro.Controls.Add(_filtroAno, 3, 0);
        barraFiltro.Controls.Add(CriarRotuloFiltro("Status:", ContentAlignment.MiddleRight), 4, 0);
        barraFiltro.Controls.Add(_filtroStatus, 5, 0);

        // --- Cartões com os totais (Total / Total pago / Falta pagar) ---------
        var painelCartoes = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 78,
            ColumnCount = 4,
            RowCount = 1,
            Padding = new Padding(12, 10, 12, 6),
            BackColor = Estilo.CorFundo,
        };
        for (var i = 0; i < 4; i++)
            painelCartoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        painelCartoes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _valorTotal = CriarCartao(painelCartoes, 0, "Comissão total", Estilo.CorTexto);
        _valorPago = CriarCartao(painelCartoes, 1, "Pago", Estilo.CorSucesso);
        _valorFalta = CriarCartao(painelCartoes, 2, "Falta pagar", Estilo.CorPrimaria);
        _valorVencidas = CriarCartao(painelCartoes, 3, "Vencidas não pagas", Estilo.CorPerigo);

        // --- Grade -----------------------------------------------------------
        _grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            // Várias linhas de uma vez: marcar um lote como pago e ver a soma.
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
        };
        Estilo.EstilizarGrade(_grade);
        AdicionarColuna("numero", "Nº", 32, alinharDireita: true);
        AdicionarColuna("nf", "Nota fiscal", 62);
        AdicionarColuna("boleto", "Nº boleto", 55);
        AdicionarColuna("cliente", "Cliente", 150);
        AdicionarColuna("pedido", "Ref. pedido", 62);
        AdicionarColuna("valorNf", "Valor NF", 72, alinharDireita: true);
        AdicionarColuna("vencimento", "Vencimento", 68);
        AdicionarColuna("parcela", "Parcela", 50);
        AdicionarColuna("valorParcela", "Valor parcela", 72, alinharDireita: true);
        AdicionarColuna("comissao", "Comissão", 66, alinharDireita: true);
        AdicionarColuna("status", "Status", 56);
        AdicionarColuna("pagamento", "Pag. comissão", 70);

        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Editar();
        };
        _grade.SelectionChanged += (_, _) => AtualizarResumo();
        _grade.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete) Excluir();
        };

        // Ordenação pelo valor real (data, número), não pelo texto exibido.
        _grade.SortCompare += (_, e) =>
        {
            if (_grade.Rows[e.RowIndex1].Tag is not Comissao a || _grade.Rows[e.RowIndex2].Tag is not Comissao b)
                return;
            e.SortResult = e.Column.Name switch
            {
                "numero" => Numero(a).CompareTo(Numero(b)),
                "nf" => CompararNumeroOuTexto(a.NotaFiscal, b.NotaFiscal),
                "pedido" => CompararNumeroOuTexto(a.RefPedido, b.RefPedido),
                "valorNf" => a.ValorNf.CompareTo(b.ValorNf),
                "vencimento" => a.Vencimento.CompareTo(b.Vencimento),
                "valorParcela" => a.ValorParcela.CompareTo(b.ValorParcela),
                "comissao" => a.ValorComissao.CompareTo(b.ValorComissao),
                "pagamento" => Nullable.Compare(a.DataPagamento, b.DataPagamento),
                _ => string.Compare(Convert.ToString(e.CellValue1), Convert.ToString(e.CellValue2),
                    StringComparison.CurrentCultureIgnoreCase),
            };
            // Empate: mantém a ordem da planilha.
            if (e.SortResult == 0)
                e.SortResult = Numero(a).CompareTo(Numero(b));
            e.Handled = true;
        };

        var menuContexto = new ContextMenuStrip();
        menuContexto.Items.Add("Editar", null, (_, _) => Editar());
        menuContexto.Items.Add("Marcar como pago...", null, (_, _) => MarcarPago());
        menuContexto.Items.Add("Marcar como não pago", null, (_, _) => MarcarNaoPago());
        menuContexto.Items.Add(new ToolStripSeparator());
        menuContexto.Items.Add("Excluir", null, (_, _) => Excluir());
        _grade.ContextMenuStrip = menuContexto;
        _grade.CellMouseDown += (_, e) =>
        {
            // Clique-direito fora da seleção seleciona só aquela linha; dentro
            // dela, mantém o lote selecionado (para agir em todas).
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && !_grade.Rows[e.RowIndex].Selected)
            {
                _grade.ClearSelection();
                _grade.Rows[e.RowIndex].Selected = true;
            }
        };

        _resumo = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Estilo.CorSuperficie,
            ForeColor = Estilo.CorTextoSuave,
        };

        // Fill primeiro; depois rodapé e as faixas do topo (a última adicionada
        // fica mais acima): barra de ações > filtro > cartões > grade.
        Controls.Add(_grade);
        Controls.Add(_resumo);
        Controls.Add(painelCartoes);
        Controls.Add(barraFiltro);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    // --- Montagem da tela ---------------------------------------------------

    private static ComboBox CriarCombo() => new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
        FlatStyle = FlatStyle.Flat,
        Margin = new Padding(0),
    };

    private static Label CriarRotuloFiltro(string texto, ContentAlignment alinhamento) => new()
    {
        Text = texto,
        Dock = DockStyle.Fill,
        TextAlign = alinhamento,
        ForeColor = Estilo.CorTextoSuave,
        Padding = alinhamento == ContentAlignment.MiddleRight ? new Padding(0, 0, 8, 0) : Padding.Empty,
    };

    /// <summary>Cartão branco com título pequeno e o valor em destaque; devolve o rótulo do valor.</summary>
    private static Label CriarCartao(TableLayoutPanel painel, int coluna, string titulo, Color corValor)
    {
        var cartao = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Estilo.CorSuperficie,
            Margin = new Padding(coluna == 0 ? 0 : 8, 0, 0, 0),
            Padding = new Padding(12, 6, 12, 4),
        };
        cartao.Paint += (_, e) =>
        {
            using var caneta = new Pen(Estilo.CorBorda);
            e.Graphics.DrawRectangle(caneta, 0, 0, cartao.Width - 1, cartao.Height - 1);
        };
        cartao.Resize += (_, _) => cartao.Invalidate();

        var valor = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = corValor,
            Font = Estilo.FonteTitulo,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        };
        var rotulo = new Label
        {
            Text = titulo,
            Dock = DockStyle.Top,
            Height = 20,
            ForeColor = Estilo.CorTextoSuave,
            TextAlign = ContentAlignment.BottomLeft,
        };
        cartao.Controls.Add(valor);
        cartao.Controls.Add(rotulo);
        painel.Controls.Add(cartao, coluna, 0);
        return valor;
    }

    private void AdicionarColuna(string nome, string titulo, int peso, bool alinharDireita = false)
    {
        var indice = _grade.Columns.Add(nome, titulo);
        var coluna = _grade.Columns[indice];
        coluna.FillWeight = peso;
        coluna.SortMode = DataGridViewColumnSortMode.Automatic;
        if (alinharDireita)
        {
            coluna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            coluna.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
            coluna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
            coluna.HeaderCell.Style.Padding = new Padding(0, 0, 10, 0);
        }
    }

    // --- Dados ----------------------------------------------------------------

    private List<Comissao> Selecionadas => _grade.SelectedRows
        .Cast<DataGridViewRow>()
        .Select(r => r.Tag)
        .OfType<Comissao>()
        .OrderBy(Numero)
        .ToList();

    private int Numero(Comissao comissao) =>
        _numeroPorId.TryGetValue(comissao.Id, out var numero) ? numero : 0;

    /// <summary>Recarrega do banco, reaplica os filtros e, opcionalmente, reseleciona linhas.</summary>
    private void Recarregar(IEnumerable<long>? selecionar = null)
    {
        _todos = ComissaoRepository.Listar();
        _numeroPorId = _todos.Select((c, i) => (c.Id, i + 1)).ToDictionary(p => p.Id, p => p.Item2);
        AtualizarAnos();
        AtualizarGrade();

        if (selecionar is null)
            return;
        var ids = selecionar.ToHashSet();
        _grade.ClearSelection();
        foreach (DataGridViewRow linha in _grade.Rows)
        {
            if (linha.Tag is Comissao c && ids.Contains(c.Id))
                linha.Selected = true;
        }
        var primeira = _grade.SelectedRows.Cast<DataGridViewRow>().MinBy(r => r.Index);
        if (primeira is null)
            return;
        try
        {
            _grade.FirstDisplayedScrollingRowIndex = primeira.Index;
        }
        catch (InvalidOperationException)
        {
            // Grade sem área visível (ex.: janela minimizada) — só não rola.
        }
    }

    /// <summary>Opções do filtro de ano conforme os vencimentos existentes (mantém a escolha).</summary>
    private void AtualizarAnos()
    {
        var escolhido = _filtroAno.SelectedItem as string;
        var anos = _todos.Select(c => c.Vencimento.Year).Distinct().OrderByDescending(a => a)
            .Select(a => a.ToString(CultureInfo.InvariantCulture)).ToList();

        _filtroAno.BeginUpdate();
        _filtroAno.Items.Clear();
        _filtroAno.Items.Add(TodosOsAnos);
        foreach (var ano in anos)
            _filtroAno.Items.Add(ano);
        var indice = escolhido is null ? 0 : _filtroAno.Items.IndexOf(escolhido);
        _filtroAno.SelectedIndex = Math.Max(0, indice);
        _filtroAno.EndUpdate();
    }

    private bool PassaFiltros(Comissao c)
    {
        if (_filtroAno.SelectedItem is string ano && ano != TodosOsAnos &&
            c.Vencimento.Year.ToString(CultureInfo.InvariantCulture) != ano)
        {
            return false;
        }
        return (_filtroStatus.SelectedItem as string) switch
        {
            FiltroPago => c.Paga,
            FiltroNaoPago => !c.Paga,
            FiltroVencidas => c.VencidaNaoPaga,
            _ => true,
        };
    }

    private void AtualizarGrade()
    {
        var termo = Comissao.Normalizar(_campoPesquisa.Text);
        _visiveis = _todos
            .Where(PassaFiltros)
            .Where(c => termo.Length == 0 || TextoPesquisavel(c).Contains(termo))
            .ToList();

        _grade.SuspendLayout();
        _grade.Rows.Clear();
        foreach (var c in _visiveis)
        {
            var indice = _grade.Rows.Add(
                Numero(c),
                c.NotaFiscal,
                c.NumeroBoleto,
                c.Cliente,
                c.RefPedido,
                Moeda(c.ValorNf),
                c.Vencimento.ToString("dd/MM/yyyy", CulturaBr),
                c.Parcela,
                Moeda(c.ValorParcela),
                Moeda(c.ValorComissao),
                c.StatusDescricao,
                c.DataPagamento?.ToString("dd/MM/yyyy", CulturaBr) ?? "");

            var linha = _grade.Rows[indice];
            linha.Tag = c;
            if (c.Paga)
                linha.DefaultCellStyle.ForeColor = Estilo.CorSucesso;
            else if (c.VencidaNaoPaga)
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
        }
        _grade.ResumeLayout();

        // Repovoar a grade desfaz a ordenação escolhida no cabeçalho; reaplica.
        if (_grade.SortedColumn is { } colunaOrdenada)
        {
            _grade.Sort(colunaOrdenada, _grade.SortOrder == SortOrder.Descending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending);
        }
        _grade.ClearSelection();

        // Totais como na planilha (=SUM, =SUMIF "Pago"/"Não Pago"), só que
        // sobre o que está filtrado — filtrar um cliente mostra os totais dele.
        var total = _visiveis.Sum(c => c.ValorComissao);
        var pago = _visiveis.Where(c => c.Paga).Sum(c => c.ValorComissao);
        _valorTotal.Text = Moeda(total);
        _valorPago.Text = Moeda(pago);
        _valorFalta.Text = Moeda(total - pago);
        _valorVencidas.Text = Moeda(_visiveis.Where(c => c.VencidaNaoPaga).Sum(c => c.ValorComissao));

        AtualizarResumo();
    }

    /// <summary>Rodapé: quantas linhas e, com 2+ selecionadas, a soma da comissão delas.</summary>
    private void AtualizarResumo()
    {
        var texto = $"{_visiveis.Count} parcela(s)";
        var selecionadas = Selecionadas;
        if (selecionadas.Count >= 2)
        {
            texto += $"   |   Selecionadas: {selecionadas.Count} — comissão " +
                Moeda(selecionadas.Sum(c => c.ValorComissao));
        }
        _resumo.Text = texto + "   |   Ctrl/Shift + clique seleciona várias; clique-direito para mais ações";
    }

    private static string TextoPesquisavel(Comissao c) => Comissao.Normalizar(string.Join(' ',
        c.NotaFiscal,
        c.NumeroBoleto,
        c.Cliente,
        c.RefPedido,
        c.Parcela,
        c.StatusDescricao,
        c.ValorNf.ToString("0.00", CulturaBr),
        c.ValorParcela.ToString("0.00", CulturaBr),
        c.ValorParcela.ToString("0.00", CultureInfo.InvariantCulture),
        c.Vencimento.ToString("dd/MM/yyyy", CulturaBr),
        c.DataPagamento?.ToString("dd/MM/yyyy", CulturaBr) ?? ""));

    private static int CompararNumeroOuTexto(string a, string b) =>
        long.TryParse(a, out var na) && long.TryParse(b, out var nb)
            ? na.CompareTo(nb)
            : string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);

    private static string Moeda(decimal valor) => valor.ToString("C2", CulturaBr);

    // --- Ações ------------------------------------------------------------------

    private void NovaVenda()
    {
        using var formulario = new NovaVendaForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        ComissaoRepository.InserirVarias(formulario.Parcelas);
        Recarregar(formulario.Parcelas.Select(p => p.Id));
    }

    private void Editar()
    {
        if (Selecionadas.FirstOrDefault() is not { } comissao)
            return;
        using var formulario = new ComissaoForm(comissao.Clonar());
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        ComissaoRepository.Atualizar(formulario.Comissao);
        Recarregar(new[] { comissao.Id });
    }

    private void MarcarPago()
    {
        var selecionadas = Selecionadas;
        if (selecionadas.Count == 0)
            return;
        using var formulario = new DataPagamentoForm(selecionadas);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        var ids = selecionadas.Select(c => c.Id).ToList();
        ComissaoRepository.DefinirPagamento(ids, true, formulario.Data);
        Recarregar(ids);
    }

    private void MarcarNaoPago()
    {
        var selecionadas = Selecionadas;
        if (selecionadas.Count == 0)
            return;
        var comData = selecionadas.Count(c => c.DataPagamento is not null);
        if (comData > 0)
        {
            var resposta = MessageBox.Show(this,
                $"Marcar {selecionadas.Count} parcela(s) como não paga(s)?\n" +
                $"A data de pagamento da comissão de {comData} delas será apagada.",
                "Marcar como não pago", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resposta != DialogResult.Yes)
                return;
        }
        var ids = selecionadas.Select(c => c.Id).ToList();
        ComissaoRepository.DefinirPagamento(ids, false, null);
        Recarregar(ids);
    }

    private void Excluir()
    {
        var selecionadas = Selecionadas;
        if (selecionadas.Count == 0)
            return;
        var descricao = selecionadas.Count == 1
            ? $"a parcela {selecionadas[0].Parcela} de \"{selecionadas[0].Cliente}\" " +
              $"(vencimento {selecionadas[0].Vencimento:dd/MM/yyyy})"
            : $"{selecionadas.Count} parcelas";
        var resposta = MessageBox.Show(this, $"Excluir {descricao}?", "Confirmar exclusão",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        ComissaoRepository.Excluir(selecionadas.Select(c => c.Id));
        Recarregar();
    }

    private void AbrirConfiguracoes()
    {
        using var formulario = new ComissoesConfigForm();
        formulario.ShowDialog(this);
        if (formulario.HouveImportacao)
            Recarregar();
    }
}
