using System.Diagnostics;
using System.Globalization;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Fechamento mensal da comissão para o vendedor — a tabela auxiliar à direita
/// da aba da planilha: escolhe o período, marca as parcelas a pagar, vê a SOMA,
/// gera o PDF/copia a tabela e registra o pagamento (preenche "pag. comissão").
/// </summary>
public class FechamentoComissaoForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly DateTimePicker _campoDe;
    private readonly DateTimePicker _campoAte;
    private readonly CheckBox _campoAnteriores;
    private readonly DataGridView _grade;
    private readonly Label _soma;
    private readonly Button _botaoRegistrar;
    private readonly Button _botaoPdf;
    private readonly Button _botaoCopiar;
    private List<Comissao> _todas;
    private bool _carregando;

    /// <summary>True se algum pagamento foi registrado (o chamador recarrega a grade).</summary>
    public bool HouveAlteracao { get; private set; }

    public FechamentoComissaoForm()
    {
        _todas = ComissaoRepository.Listar();
        var (de, ate) = FechamentoComissoes.PeriodoSugerido(_todas, DateTime.Today);

        Text = "Fechamento do mês — comissão do vendedor";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MinimumSize = new Size(960, 560);
        ClientSize = new Size(1080, 640);

        // --- Período ---------------------------------------------------------------
        _campoDe = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Value = de, Margin = new Padding(0) };
        _campoAte = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Value = ate, Margin = new Padding(0) };
        _campoAnteriores = new CheckBox
        {
            Text = "Mostrar também \"Não pago\" de antes do período",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(16, 0, 0, 0),
            ForeColor = Estilo.CorTextoSuave,
        };
        _campoDe.ValueChanged += (_, _) => Carregar();
        _campoAte.ValueChanged += (_, _) => Carregar();
        _campoAnteriores.CheckedChanged += (_, _) => Carregar();

        var botaoTodas = Estilo.BotaoPadrao("Marcar todas");
        botaoTodas.Click += (_, _) => MarcarTodas(true);
        var botaoNenhuma = Estilo.BotaoPadrao("Desmarcar todas");
        botaoNenhuma.Click += (_, _) => MarcarTodas(false);
        var fluxoMarcar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
        };
        botaoNenhuma.Margin = new Padding(0, 2, 0, 2);
        fluxoMarcar.Controls.Add(botaoNenhuma);
        fluxoMarcar.Controls.Add(botaoTodas);

        var barraPeriodo = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = Estilo.AlturaBotao + 24,
            ColumnCount = 6,
            RowCount = 1,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 10, 12, 10),
        };
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        barraPeriodo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barraPeriodo.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        barraPeriodo.Controls.Add(Rotulo("Vencimento de", ContentAlignment.MiddleLeft), 0, 0);
        barraPeriodo.Controls.Add(_campoDe, 1, 0);
        barraPeriodo.Controls.Add(Rotulo("até", ContentAlignment.MiddleCenter), 2, 0);
        barraPeriodo.Controls.Add(_campoAte, 3, 0);
        barraPeriodo.Controls.Add(_campoAnteriores, 4, 0);
        barraPeriodo.Controls.Add(fluxoMarcar, 5, 0);

        var ajuda = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(14, 0, 12, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
            Text = "Clique nas linhas para marcar/desmarcar — as destacadas em azul entram na SOMA. Vêm marcadas " +
                   "as de status \"Pago\" (cliente pagou); as \"Não pago\" vêm desmarcadas.",
        };

        // --- Grade -----------------------------------------------------------------
        _grade = new GradeMarcavel
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
        };
        Estilo.EstilizarGrade(_grade);
        AdicionarColuna("numero", "Nº", 30, direita: true);
        AdicionarColuna("nf", "Nota fiscal", 58);
        AdicionarColuna("boleto", "Nº b", 50);
        AdicionarColuna("cliente", "Cliente", 150);
        AdicionarColuna("pedido", "REF. Pedido", 60);
        AdicionarColuna("valorNf", "Valor NF", 70, direita: true);
        AdicionarColuna("vencimento", "Vencimento", 66);
        AdicionarColuna("parcela", "Nº de parcelas", 60);
        AdicionarColuna("valorParcela", "Valor da parcela", 72, direita: true);
        AdicionarColuna("comissao", "Comissão", 64, direita: true);
        AdicionarColuna("status", "Status", 54);

        // A marcação é a própria seleção (destaque) da linha.
        _grade.SelectionChanged += (_, _) =>
        {
            if (!_carregando)
                AtualizarSoma();
        };

        var painelGrade = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 0) };
        painelGrade.Controls.Add(_grade);

        // --- Rodapé: SOMA + ações ----------------------------------------------------
        _soma = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = Estilo.CorTexto,
        };
        _botaoRegistrar = Estilo.BotaoPrimario("Registrar pagamento...");
        _botaoRegistrar.Click += (_, _) => RegistrarPagamento();
        _botaoPdf = Estilo.BotaoPadrao("Gerar PDF");
        _botaoPdf.Click += (_, _) => GerarPdf(Marcadas, abrir: true);
        _botaoCopiar = Estilo.BotaoPadrao("Copiar tabela");
        _botaoCopiar.Click += (_, _) => Copiar();
        var botaoFechar = Estilo.BotaoPadrao("Fechar");
        botaoFechar.DialogResult = DialogResult.Cancel;

        var fluxoAcoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
        };
        fluxoAcoes.Controls.Add(_botaoRegistrar);
        fluxoAcoes.Controls.Add(_botaoPdf);
        fluxoAcoes.Controls.Add(_botaoCopiar);
        fluxoAcoes.Controls.Add(botaoFechar);

        var rodape = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(16, 12, 12, 12),
        };
        rodape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rodape.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rodape.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rodape.Controls.Add(_soma, 0, 0);
        rodape.Controls.Add(fluxoAcoes, 1, 0);
        fluxoAcoes.AutoSize = true;
        fluxoAcoes.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Controls.Add(painelGrade);
        Controls.Add(rodape);
        Controls.Add(ajuda);
        Controls.Add(barraPeriodo);
        CancelButton = botaoFechar;

        Carregar();
        // Ao aparecer, o WinForms seleciona sozinho a linha atual; refaz a marcação.
        Shown += (_, _) => AplicarMarcacaoPadrao();
    }

    private static Label Rotulo(string texto, ContentAlignment alinhamento) => new()
    {
        Text = texto,
        Dock = DockStyle.Fill,
        TextAlign = alinhamento,
        ForeColor = Estilo.CorTextoSuave,
    };

    private void AdicionarColuna(string nome, string titulo, int peso, bool direita = false)
    {
        var coluna = new DataGridViewTextBoxColumn
        {
            Name = nome,
            HeaderText = titulo,
            FillWeight = peso,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        if (direita)
        {
            coluna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            coluna.DefaultCellStyle.Padding = new Padding(0, 0, 10, 0);
            coluna.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
        _grade.Columns.Add(coluna);
    }

    private DateTime De => _campoDe.Value.Date;
    private DateTime Ate => _campoAte.Value.Date;

    private List<Comissao> Marcadas => _grade.SelectedRows.Cast<DataGridViewRow>()
        .OrderBy(r => r.Index)
        .Select(r => (Comissao)r.Tag!)
        .ToList();

    private void Carregar()
    {
        // "Nº" igual ao da tela principal: posição na ordem de vencimento de todas as parcelas.
        var numeroPorId = _todas
            .OrderBy(c => c.Vencimento).ThenBy(c => c.Id)
            .Select((c, i) => (c.Id, Numero: i + 1))
            .ToDictionary(p => p.Id, p => p.Numero);

        _carregando = true;
        _grade.Rows.Clear();
        foreach (var c in FechamentoComissoes.Candidatas(_todas, De, Ate, _campoAnteriores.Checked))
        {
            var indice = _grade.Rows.Add(
                numeroPorId.GetValueOrDefault(c.Id),
                c.NotaFiscal,
                c.NumeroBoleto,
                c.Cliente,
                c.RefPedido,
                Moeda(c.ValorNf),
                c.Vencimento.ToString("dd/MM/yyyy", CulturaBr),
                c.Parcela,
                Moeda(c.ValorParcela),
                Moeda(c.ValorComissao),
                c.StatusDescricao);
            var linha = _grade.Rows[indice];
            linha.Tag = c;
            // Pendência de antes do período: destaca a data, para não passar batido.
            if (c.Vencimento.Date < De)
                linha.Cells["vencimento"].Style.ForeColor = Estilo.CorPerigo;
        }
        _carregando = false;
        AplicarMarcacaoPadrao();
    }

    /// <summary>Destaca (marca) as de status "Pago"; as "Não pago" ficam sem destaque.</summary>
    private void AplicarMarcacaoPadrao()
    {
        _carregando = true;
        _grade.ClearSelection();
        foreach (DataGridViewRow linha in _grade.Rows)
            linha.Selected = FechamentoComissoes.MarcadaPorPadrao((Comissao)linha.Tag!);
        _carregando = false;
        AtualizarSoma();
    }

    private void MarcarTodas(bool marcar)
    {
        if (marcar)
            _grade.SelectAll();
        else
            _grade.ClearSelection();
        _grade.Focus();
    }

    private void AtualizarSoma()
    {
        var marcadas = Marcadas;
        _soma.Text = $"SOMA: {Moeda(marcadas.Sum(c => c.ValorComissao))}   " +
                     $"({marcadas.Count} de {_grade.Rows.Count} parcela(s))";
        _botaoRegistrar.Enabled = _botaoPdf.Enabled = _botaoCopiar.Enabled = marcadas.Count > 0;
    }

    private void RegistrarPagamento()
    {
        var marcadas = Marcadas;
        if (marcadas.Count == 0)
            return;
        using var formulario = new DataPagamentoForm(marcadas, dataObrigatoria: true);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        // Grava "Pago" + a data em todas as marcadas (inclusive "Não pago"
        // marcadas à mão: se vai pagar a comissão, o cliente pagou).
        ComissaoRepository.DefinirPagamento(marcadas.Select(c => c.Id), true, formulario.Data);
        HouveAlteracao = true;

        var resposta = MessageBox.Show(this,
            $"Pagamento registrado: {marcadas.Count} parcela(s), comissão {Moeda(marcadas.Sum(c => c.ValorComissao))}.\n\n" +
            "Gerar o PDF deste fechamento para o vendedor?",
            "Fechamento registrado", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (resposta == DialogResult.Yes)
            GerarPdf(marcadas, abrir: true);

        // As acertadas saem da lista (agora têm data de pagamento).
        _todas = ComissaoRepository.Listar();
        Carregar();
    }

    private void GerarPdf(List<Comissao> parcelas, bool abrir)
    {
        if (parcelas.Count == 0)
            return;
        string caminho;
        try
        {
            caminho = FechamentoComissaoPdf.Gerar(parcelas, De, Ate);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Não foi possível gravar o PDF:\n\n" + ex.Message, "Erro ao gerar PDF",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (abrir)
            Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
    }

    /// <summary>Copia a tabela marcada (com a SOMA) para colar no Excel, WhatsApp, e-mail...</summary>
    private void Copiar()
    {
        var marcadas = Marcadas;
        if (marcadas.Count == 0)
            return;
        var texto = new StringBuilder();
        texto.AppendLine("Nota fiscal\tNº b\tCliente\tREF. Pedido\tValor NF\tVencimento\tNº de parcelas\tValor da parcela\tComissão");
        foreach (var c in marcadas)
        {
            texto.AppendLine(string.Join('\t',
                c.NotaFiscal, c.NumeroBoleto, c.Cliente, c.RefPedido, Moeda(c.ValorNf),
                c.Vencimento.ToString("dd/MM/yyyy", CulturaBr), c.Parcela, Moeda(c.ValorParcela), Moeda(c.ValorComissao)));
        }
        texto.AppendLine($"\t\t\t\t\t\t\tSOMA:\t{Moeda(marcadas.Sum(c => c.ValorComissao))}");
        Clipboard.SetText(texto.ToString());
        MessageBox.Show(this, $"{marcadas.Count} parcela(s) copiada(s) — é só colar (Ctrl+V).", "Tabela copiada",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Moeda(decimal valor) => valor.ToString("C2", CulturaBr);

    /// <summary>
    /// Grade em que o clique simples liga/desliga o destaque da linha, sem
    /// precisar segurar Ctrl (Ctrl/Shift continuam funcionando como sempre).
    /// Espaço alterna a linha atual. As setas só movem o cursor, sem desfazer
    /// a marcação.
    /// </summary>
    private sealed class GradeMarcavel : DataGridView
    {
        protected override void OnMouseDown(MouseEventArgs e)
        {
            var alvo = HitTest(e.X, e.Y);
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.None &&
                alvo.Type == DataGridViewHitTestType.Cell && alvo.RowIndex >= 0)
            {
                Focus();
                var linha = Rows[alvo.RowIndex];
                linha.Selected = !linha.Selected;
                return;
            }
            // Clique no espaço vazio da grade não desfaz a marcação.
            if (alvo.Type == DataGridViewHitTestType.None)
            {
                Focus();
                return;
            }
            base.OnMouseDown(e);
        }

        protected override bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space && e.Modifiers == Keys.None && CurrentRow is { } atual)
            {
                atual.Selected = !atual.Selected;
                return true;
            }
            // Setas sem Shift: move a linha atual preservando a seleção.
            if (e.Modifiers == Keys.None && e.KeyCode is Keys.Up or Keys.Down && CurrentCell is { } celula)
            {
                var destino = celula.RowIndex + (e.KeyCode == Keys.Up ? -1 : 1);
                if (destino >= 0 && destino < Rows.Count)
                {
                    var marcadas = SelectedRows.Cast<DataGridViewRow>().ToList();
                    CurrentCell = Rows[destino].Cells[celula.ColumnIndex];
                    ClearSelection();
                    foreach (var linha in marcadas)
                        linha.Selected = true;
                }
                return true;
            }
            return base.ProcessDataGridViewKey(e);
        }
    }
}
