using System.Diagnostics;
using System.Globalization;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Tela principal do módulo Despesas: uma linha por item de despesa mostrando
/// a <b>última conta</b> daquele item — competência, vencimento, valor e
/// situação — para dar de olho o que já chegou e o que ainda falta buscar.
/// </summary>
public class DespesasControl : UserControl
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Cor de quem ainda não tem a conta do mês corrente lançada.</summary>
    private static readonly Color CorPendente = Color.FromArgb(191, 116, 20);

    private readonly DataGridView _grade;
    private readonly TextBox _campoPesquisa;
    private readonly ComboBox _filtro;
    private readonly Label _resumo;

    private List<Despesa> _todas = new();
    private Dictionary<long, LancamentoDespesa> _ultimos = new();

    public DespesasControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior --------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoColetar = Estilo.BotaoPrimario("Buscar contas do mês");
        var botaoLancar = Estilo.BotaoPrimario("+ Lançar conta");
        var botaoAbrirPortal = Estilo.BotaoPadrao("Abrir portal");
        var botaoContas = Estilo.BotaoPadrao("Contas do item");
        var botaoEditar = Estilo.BotaoPadrao("Editar item");
        var botaoConfiguracoes = Estilo.BotaoIcone("⚙", "Configurações (pasta de downloads e e-mail)");

        botaoColetar.Click += (_, _) => Coletar();
        botaoLancar.Click += (_, _) => LancarConta();
        botaoAbrirPortal.Click += (_, _) => AbrirPortal();
        botaoContas.Click += (_, _) => AbrirHistorico();
        botaoEditar.Click += (_, _) => EditarItem();
        botaoConfiguracoes.Click += (_, _) => AbrirConfiguracoes();

        var fluxoAcoes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = false };
        fluxoAcoes.Controls.Add(botaoColetar);
        fluxoAcoes.Controls.Add(botaoLancar);
        fluxoAcoes.Controls.Add(botaoAbrirPortal);
        fluxoAcoes.Controls.Add(botaoContas);
        fluxoAcoes.Controls.Add(botaoEditar);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfiguracoes.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfiguracoes);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Barra de filtro --------------------------------------------------
        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Pesquisar por item, fornecedor, valor ou observação...",
            Margin = new Padding(0),
        };
        _campoPesquisa.TextChanged += (_, _) => AtualizarGrade();

        _filtro = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0),
        };
        _filtro.Items.AddRange(new object[]
        {
            "Todos os itens",
            "Falta a conta do mês",
            "Em aberto",
            "Vencidos",
            "Com coleta automática",
        });
        _filtro.SelectedIndex = 0;
        _filtro.SelectedIndexChanged += (_, _) => AtualizarGrade();

        var barraFiltro = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 6, 12, 6),
        };
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        barraFiltro.Controls.Add(new Label
        {
            Text = "Pesquisar:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, 0);
        barraFiltro.Controls.Add(_campoPesquisa, 1, 0);
        barraFiltro.Controls.Add(new Label
        {
            Text = "Mostrar:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Estilo.CorTextoSuave,
            Padding = new Padding(0, 0, 8, 0),
        }, 2, 0);
        barraFiltro.Controls.Add(_filtro, 3, 0);

        // --- Grade ------------------------------------------------------------
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
        _grade.Columns.Add("item", "Item");
        _grade.Columns.Add("fornecedor", "Fornecedor");
        _grade.Columns.Add("obter", "Como obter");
        _grade.Columns.Add("competencia", "Última");
        _grade.Columns.Add("vencimento", "Vencimento");
        _grade.Columns.Add("valor", "Valor");
        _grade.Columns.Add("situacao", "Situação");
        _grade.Columns.Add("coleta", "Coleta");
        _grade.Columns["item"]!.FillWeight = 130;
        _grade.Columns["fornecedor"]!.FillWeight = 70;
        _grade.Columns["obter"]!.FillWeight = 70;
        _grade.Columns["competencia"]!.FillWeight = 50;
        _grade.Columns["vencimento"]!.FillWeight = 60;
        _grade.Columns["valor"]!.FillWeight = 60;
        _grade.Columns["situacao"]!.FillWeight = 70;
        _grade.Columns["coleta"]!.FillWeight = 70;

        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) AbrirHistorico();
        };

        // Ordenação pelo valor real (data e dinheiro), não pelo texto formatado.
        _grade.SortCompare += (_, e) =>
        {
            if (_grade.Rows[e.RowIndex1].Tag is not Despesa d1 ||
                _grade.Rows[e.RowIndex2].Tag is not Despesa d2)
            {
                return;
            }

            var u1 = Ultimo(d1);
            var u2 = Ultimo(d2);
            e.SortResult = e.Column.Name switch
            {
                "vencimento" or "competencia" =>
                    (u1?.Vencimento ?? DateTime.MinValue).CompareTo(u2?.Vencimento ?? DateTime.MinValue),
                "valor" => (u1?.Valor ?? 0m).CompareTo(u2?.Valor ?? 0m),
                _ => string.Compare(
                    Convert.ToString(e.CellValue1), Convert.ToString(e.CellValue2),
                    StringComparison.CurrentCultureIgnoreCase),
            };
            e.Handled = true;
        };

        var menuContexto = new ContextMenuStrip();
        menuContexto.Items.Add("Contas do item...", null, (_, _) => AbrirHistorico());
        menuContexto.Items.Add("Lançar conta...", null, (_, _) => LancarConta());
        menuContexto.Items.Add(new ToolStripSeparator());
        menuContexto.Items.Add("Abrir portal no navegador", null, (_, _) => AbrirPortal());
        menuContexto.Items.Add("Copiar login", null, (_, _) => CopiarLogin());
        menuContexto.Items.Add("Copiar senha", null, (_, _) => CopiarSenha());
        menuContexto.Items.Add("Copiar linha digitável da última conta", null, (_, _) => CopiarLinhaDigitavel());
        menuContexto.Items.Add(new ToolStripSeparator());
        menuContexto.Items.Add("Marcar última conta como paga", null, (_, _) => MarcarUltimaComoPaga());
        menuContexto.Items.Add("Buscar só este item", null, (_, _) => ColetarSelecionado());
        menuContexto.Items.Add(new ToolStripSeparator());
        menuContexto.Items.Add("Editar item...", null, (_, _) => EditarItem());
        menuContexto.Items.Add("Novo item...", null, (_, _) => NovoItem());
        menuContexto.Items.Add("Excluir item", null, (_, _) => ExcluirItem());
        _grade.ContextMenuStrip = menuContexto;
        _grade.CellMouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
                _grade.Rows[e.RowIndex].Selected = true;
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

        // Fill primeiro, depois os docks de borda (convenção do projeto).
        Controls.Add(_grade);
        Controls.Add(_resumo);
        Controls.Add(barraFiltro);
        Controls.Add(barraSuperior);

        CatalogoInicial.SemearSeNecessario();
        Recarregar();
    }

    private Despesa? Selecionada =>
        _grade.SelectedRows.Count == 0 ? null : (Despesa)_grade.SelectedRows[0].Tag!;

    private LancamentoDespesa? Ultimo(Despesa despesa) =>
        _ultimos.TryGetValue(despesa.Id, out var lancamento) ? lancamento : null;

    private void Recarregar()
    {
        _todas = DespesaRepository.Listar();
        _ultimos = LancamentoDespesaRepository.UltimoPorDespesa();
        AtualizarGrade();
    }

    private void AtualizarGrade()
    {
        var termo = Normalizar(_campoPesquisa.Text.Trim());
        var visiveis = _todas
            .Where(PassaFiltro)
            .Where(d => termo.Length == 0 || TextoPesquisavel(d).Contains(termo))
            .ToList();

        _grade.Rows.Clear();
        foreach (var despesa in visiveis)
        {
            var ultimo = Ultimo(despesa);
            var indice = _grade.Rows.Add(
                despesa.Nome,
                despesa.Fornecedor,
                despesa.Forma.Descricao(),
                ultimo?.CompetenciaFormatada ?? "—",
                ultimo?.Vencimento.ToString("dd/MM/yyyy") ?? "—",
                ultimo is null ? "—" : ultimo.Valor.ToString("C2", CulturaBr),
                ultimo?.Situacao.Descricao() ?? "sem lançamento",
                despesa.Metodo.Descricao());

            var linha = _grade.Rows[indice];
            linha.Tag = despesa;

            if (!despesa.Ativo)
                linha.DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
            else if (ultimo is { Vencido: true })
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
            else if (FaltaContaDoMes(despesa))
                linha.DefaultCellStyle.ForeColor = CorPendente;
            else if (ultimo?.Situacao.CorTexto() is { } cor)
                linha.DefaultCellStyle.ForeColor = cor;
        }

        var faltando = visiveis.Count(FaltaContaDoMes);
        var emAberto = visiveis
            .Select(Ultimo)
            .Where(l => l is { Situacao: SituacaoDespesa.Aberto })
            .Sum(l => l!.Valor);

        _resumo.Text =
            $"{visiveis.Count} item(ns) — {faltando} sem a conta deste mês — " +
            $"último(s) em aberto: {emAberto.ToString("C2", CulturaBr)}" +
            "   |   Clique-direito num item para mais ações";
    }

    /// <summary>
    /// Item ativo cuja última conta é anterior à competência corrente — ou seja,
    /// a conta deste mês ainda não foi buscada.
    /// </summary>
    private bool FaltaContaDoMes(Despesa despesa)
    {
        if (!despesa.Ativo)
            return false;
        var ultimo = Ultimo(despesa);
        if (ultimo is null)
            return true;

        var competenciaAtual = DateTime.Today.ToString("yyyy-MM");
        var competencia = ultimo.Competencia.Length > 0
            ? ultimo.Competencia
            : ultimo.Vencimento.ToString("yyyy-MM");
        return string.CompareOrdinal(competencia, competenciaAtual) < 0;
    }

    private bool PassaFiltro(Despesa despesa)
    {
        var ultimo = Ultimo(despesa);
        return _filtro.SelectedIndex switch
        {
            1 => FaltaContaDoMes(despesa),
            2 => ultimo is { Situacao: SituacaoDespesa.Aberto },
            3 => ultimo is { Vencido: true },
            4 => despesa.Metodo != MetodoColeta.Nenhum,
            _ => true,
        };
    }

    private string TextoPesquisavel(Despesa despesa)
    {
        var ultimo = Ultimo(despesa);
        var partes = string.Join(' ',
            despesa.Nome,
            despesa.Fornecedor,
            despesa.Forma.Descricao(),
            despesa.Identificador,
            despesa.Observacoes,
            ultimo?.Valor.ToString("0.00", CulturaBr) ?? "",
            ultimo?.Vencimento.ToString("dd/MM/yyyy") ?? "");
        return Normalizar(partes);
    }

    /// <summary>Minúsculas e sem acentos, para uma busca tolerante (igual ao módulo Boletos).</summary>
    private static string Normalizar(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var construtor = new StringBuilder(decomposto.Length);
        foreach (var caractere in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                construtor.Append(char.ToLowerInvariant(caractere));
        }
        return construtor.ToString();
    }

    // --- Ações ---------------------------------------------------------------

    private void Coletar() => ExecutarColeta(ServicoColeta.Automatizaveis(_todas), todos: true);

    private void ColetarSelecionado()
    {
        if (Selecionada is not { } despesa)
            return;
        if (despesa.Metodo == MetodoColeta.Nenhum)
        {
            MessageBox.Show(this,
                $"O item \"{despesa.Nome}\" não tem coleta automática configurada.\n" +
                "Abra \"Editar item\" → aba \"Coleta automática\" para escolher um método.",
                "Sem coleta automática", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ExecutarColeta(new List<Despesa> { despesa }, todos: false);
    }

    private void ExecutarColeta(List<Despesa> despesas, bool todos)
    {
        if (despesas.Count == 0)
        {
            MessageBox.Show(this,
                "Nenhum item está com coleta automática ligada.\n\n" +
                "Abra um item em \"Editar item\" → aba \"Coleta automática\" e escolha como ele deve " +
                "ser buscado (pasta de downloads, e-mail ou portal).",
                "Nada a buscar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var formulario = new ColetaForm(despesas);
        formulario.ShowDialog(this);
        if (formulario.TotalNovos > 0 || todos)
            Recarregar();
    }

    private void NovoItem()
    {
        using var formulario = new DespesaForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        DespesaRepository.Inserir(formulario.Despesa);
        Recarregar();
    }

    private void EditarItem()
    {
        if (Selecionada is not { } despesa)
            return;
        using var formulario = new DespesaForm(despesa);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        DespesaRepository.Atualizar(formulario.Despesa);
        Recarregar();
    }

    private void ExcluirItem()
    {
        if (Selecionada is not { } despesa)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o item \"{despesa.Nome}\" e todas as contas lançadas nele?",
            "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (resposta != DialogResult.Yes)
            return;
        DespesaRepository.Excluir(despesa.Id);
        Recarregar();
    }

    private void LancarConta()
    {
        if (Selecionada is not { } despesa)
            return;
        using var formulario = new LancamentoDespesaForm(despesa);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        LancamentoDespesaRepository.Inserir(formulario.Lancamento);
        Recarregar();
    }

    private void AbrirHistorico()
    {
        if (Selecionada is not { } despesa)
            return;
        using var formulario = new HistoricoDespesaForm(despesa);
        formulario.ShowDialog(this);
        Recarregar();
    }

    private void MarcarUltimaComoPaga()
    {
        if (Selecionada is not { } despesa || Ultimo(despesa) is not { } ultimo)
            return;
        LancamentoDespesaRepository.AlterarSituacao(ultimo.Id, SituacaoDespesa.Pago);
        Recarregar();
    }

    private void AbrirPortal()
    {
        if (Selecionada is not { } despesa)
            return;
        if (despesa.UrlPortal.Trim().Length == 0)
        {
            MessageBox.Show(this,
                $"O item \"{despesa.Nome}\" não tem endereço de portal cadastrado.\n\n" +
                (despesa.Observacoes.Length > 0 ? "Observações:\n" + despesa.Observacoes : ""),
                "Sem portal", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(despesa.UrlPortal) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Não foi possível abrir o endereço:\n" + ex.Message,
                "Erro ao abrir o portal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void CopiarLogin()
    {
        if (Selecionada is { } despesa)
            Copiar(despesa.Login, "login");
    }

    private void CopiarSenha()
    {
        if (Selecionada is { } despesa)
            Copiar(despesa.Senha, "senha");
    }

    private void CopiarLinhaDigitavel()
    {
        if (Selecionada is { } despesa)
            Copiar(Ultimo(despesa)?.LinhaDigitavel ?? "", "linha digitável");
    }

    private void Copiar(string valor, string oQue)
    {
        if (valor.Length == 0)
        {
            MessageBox.Show(this, $"Este item não tem {oQue} guardada(o).", "Nada para copiar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Clipboard.SetText(valor);
        _resumo.Text = $"{char.ToUpper(oQue[0]) + oQue[1..]} copiada(o) para a área de transferência.";
    }

    private void AbrirConfiguracoes()
    {
        using var formulario = new DespesasConfigForm();
        formulario.ShowDialog(this);
    }
}
