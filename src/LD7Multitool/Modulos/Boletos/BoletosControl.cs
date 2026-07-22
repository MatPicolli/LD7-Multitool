using System.Diagnostics;
using System.Globalization;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>Tela principal do gerenciador de boletos: grade com filtro e ações.</summary>
public class BoletosControl : UserControl
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    // Largura (px) da zona clicável do ícone de alerta, no canto direito da célula.
    private const int LarguraAlerta = 26;

    private readonly DataGridView _grade;
    private readonly ComboBox _filtroEstado;
    private readonly TextBox _campoPesquisa;
    private readonly Label _resumo;
    private List<Boleto> _todos = new();
    private List<Boleto> _visiveis = new();

    public BoletosControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior --------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoNovo = Estilo.BotaoPrimario("+ Novo");
        var botaoImportar = Estilo.BotaoPrimario("Importar boletos");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoAbrirPdf = Estilo.BotaoPadrao("Abrir PDF");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        var botaoConfiguracoes = Estilo.BotaoIcone("⚙", "Configurações (pasta dos PDFs)");

        botaoNovo.Click += (_, _) => Novo();
        botaoImportar.Click += (_, _) => Importar();
        botaoEditar.Click += (_, _) => Editar();
        botaoAbrirPdf.Click += (_, _) => AbrirPdf();
        botaoExcluir.Click += (_, _) => Excluir();
        botaoConfiguracoes.Click += (_, _) => AbrirConfiguracoes();

        var fluxoAcoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0),
        };
        fluxoAcoes.Controls.Add(botaoNovo);
        fluxoAcoes.Controls.Add(botaoImportar);
        fluxoAcoes.Controls.Add(botaoEditar);
        fluxoAcoes.Controls.Add(botaoAbrirPdf);
        fluxoAcoes.Controls.Add(botaoExcluir);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfiguracoes.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfiguracoes);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Barra de filtro (pesquisa + estado) -----------------------------
        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Pesquisar por nome, valor, vencimento, nosso número ou NF-e...",
            Margin = new Padding(0),
        };
        _campoPesquisa.TextChanged += (_, _) => AtualizarGrade();

        _filtroEstado = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0),
        };
        _filtroEstado.Items.Add("Todos");
        foreach (EstadoBoleto estado in Enum.GetValues<EstadoBoleto>())
            _filtroEstado.Items.Add(estado.Descricao());
        _filtroEstado.SelectedIndex = 0;
        _filtroEstado.SelectedIndexChanged += (_, _) => AtualizarGrade();

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
        barraFiltro.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
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
            Text = "Estado:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Estilo.CorTextoSuave,
            Padding = new Padding(0, 0, 8, 0),
        }, 2, 0);
        barraFiltro.Controls.Add(_filtroEstado, 3, 0);

        // --- Grade -----------------------------------------------------------
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
        _grade.Columns.Add("nome", "Nome");
        _grade.Columns.Add("valor", "Valor");
        _grade.Columns.Add("validade", "Vencimento");
        _grade.Columns.Add("nossoNumero", "Nosso número");
        _grade.Columns.Add("nfeReferente", "NF-e referente");
        _grade.Columns.Add("estado", "Estado");
        _grade.Columns.Add("arquivo", "PDF");
        _grade.Columns["arquivo"]!.FillWeight = 30;
        _grade.Columns["valor"]!.FillWeight = 55;
        _grade.Columns["validade"]!.FillWeight = 55;
        _grade.Columns["estado"]!.FillWeight = 50;

        // Reserva um espaço fixo à direita da data para o ícone de alerta,
        // presente ou não — assim a data nunca "dança" nem colide com o ícone.
        _grade.Columns["validade"]!.DefaultCellStyle.Padding = new Padding(6, 0, LarguraAlerta, 0);
        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Editar();
        };

        // Desenha o ícone de alerta (⚠) à direita da data quando o boleto está
        // a até 2 dias de vencer. SystemIcons.Warning é colorido e não depende
        // de fonte de emoji.
        _grade.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grade.Columns[e.ColumnIndex].Name != "validade") return;
            if (_grade.Rows[e.RowIndex].Tag is not Boleto boleto || !boleto.AlertaVencimento) return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);
            const int tamanho = 16;
            var x = e.CellBounds.Right - LarguraAlerta + (LarguraAlerta - tamanho) / 2;
            var y = e.CellBounds.Top + (e.CellBounds.Height - tamanho) / 2;
            e.Graphics.DrawIcon(SystemIcons.Warning, new Rectangle(x, y, tamanho, tamanho));
            e.Handled = true;
        };

        // Clicar no ícone de alerta abre o envio do boleto por e-mail.
        _grade.CellMouseClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grade.Columns[e.ColumnIndex].Name != "validade") return;
            if (_grade.Rows[e.RowIndex].Tag is not Boleto boleto || !boleto.AlertaVencimento) return;
            if (e.Location.X < _grade.Columns[e.ColumnIndex].Width - LarguraAlerta) return;

            _grade.Rows[e.RowIndex].Selected = true;
            EnviarPorEmail();
        };

        // Ordenação por clique no cabeçalho usando o valor real (não o texto):
        // validade compara datas e valor compara números, corrigindo o sort
        // que antes ordenava "dd/MM/yyyy" e "R$ x" como texto.
        _grade.SortCompare += (_, e) =>
        {
            if (_grade.Rows[e.RowIndex1].Tag is not Boleto b1 ||
                _grade.Rows[e.RowIndex2].Tag is not Boleto b2)
            {
                return;
            }

            e.SortResult = e.Column.Name switch
            {
                "validade" => b1.Validade.CompareTo(b2.Validade),
                "valor" => b1.Valor.CompareTo(b2.Valor),
                _ => string.Compare(
                    Convert.ToString(e.CellValue1), Convert.ToString(e.CellValue2),
                    StringComparison.CurrentCultureIgnoreCase),
            };
            e.Handled = true;
        };

        // Ações de estado ficam no menu de contexto para não lotar a barra.
        var menuContexto = new ContextMenuStrip();
        menuContexto.Items.Add("Editar", null, (_, _) => Editar());
        menuContexto.Items.Add("Marcar como pago", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Pago));
        menuContexto.Items.Add("Marcar como protestado", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Protestado));
        menuContexto.Items.Add("Cancelar boleto", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Cancelado));
        menuContexto.Items.Add("Reabrir boleto", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Aberto));
        menuContexto.Items.Add(new ToolStripSeparator());
        menuContexto.Items.Add("Enviar por e-mail...", null, (_, _) => EnviarPorEmail());
        menuContexto.Items.Add("Abrir PDF", null, (_, _) => AbrirPdf());
        menuContexto.Items.Add("Excluir", null, (_, _) => Excluir());
        _grade.ContextMenuStrip = menuContexto;
        _grade.CellMouseDown += (_, e) =>
        {
            // Clique-direito seleciona a linha antes de abrir o menu.
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

        // Ordem de docking (índice maior é posicionado primeiro / mais externo):
        // barraSuperior no topo, barraFiltro logo abaixo, resumo no rodapé e
        // a grade preenchendo o restante.
        Controls.Add(_grade);
        Controls.Add(_resumo);
        Controls.Add(barraFiltro);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    private Boleto? BoletoSelecionado =>
        _grade.SelectedRows.Count == 0 ? null : (Boleto)_grade.SelectedRows[0].Tag!;

    /// <summary>Recarrega todos os boletos do banco e reaplica filtro/pesquisa.</summary>
    private void Recarregar()
    {
        _todos = BoletoRepository.Listar();
        AtualizarGrade();
    }

    /// <summary>Decide se um boleto passa pelo filtro de estado selecionado.</summary>
    private bool PassaFiltroEstado(Boleto boleto)
    {
        // Índice 0 = "Todos"; os demais seguem a ordem do enum.
        if (_filtroEstado.SelectedIndex <= 0)
            return true;

        var filtro = (EstadoBoleto)(_filtroEstado.SelectedIndex - 1);
        // "Aberto" também mostra os protestados (ainda são dívidas em aberto);
        // qualquer outro filtro mostra somente o estado escolhido.
        return filtro == EstadoBoleto.Aberto
            ? boleto.Estado is EstadoBoleto.Aberto or EstadoBoleto.Protestado
            : boleto.Estado == filtro;
    }

    /// <summary>Aplica o filtro de estado e a pesquisa sobre a lista carregada e repovoa a grade.</summary>
    private void AtualizarGrade()
    {
        var termo = Normalizar(_campoPesquisa.Text.Trim());
        _visiveis = _todos
            .Where(PassaFiltroEstado)
            .Where(b => termo.Length == 0 || TextoPesquisavel(b).Contains(termo))
            .ToList();

        _grade.Rows.Clear();
        foreach (var boleto in _visiveis)
        {
            var indice = _grade.Rows.Add(
                boleto.Nome,
                boleto.Valor.ToString("C2", CulturaBr),
                boleto.Validade.ToString("dd/MM/yyyy"),
                boleto.NossoNumero,
                boleto.NfeReferente,
                boleto.Estado.Descricao(),
                boleto.CaminhoArquivo.Length > 0 ? "📄" : "");

            var linha = _grade.Rows[indice];
            linha.Tag = boleto;
            if (boleto.Estado.CorTexto() is { } cor)
                linha.DefaultCellStyle.ForeColor = cor;
            else if (boleto.Vencido)
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
        }

        var totalAberto = _visiveis
            .Where(b => b.Estado == EstadoBoleto.Aberto)
            .Sum(b => b.Valor);
        _resumo.Text = $"{_visiveis.Count} boleto(s) — total em aberto: {totalAberto.ToString("C2", CulturaBr)}" +
            "   |   Clique-direito num boleto para mais ações";
    }

    /// <summary>Junta os campos de um boleto num texto normalizado para a busca.</summary>
    private static string TextoPesquisavel(Boleto boleto)
    {
        // Inclui o valor em dois formatos (1234,50 e 1234.50) para casar tanto
        // com vírgula quanto com ponto, e a validade como dd/MM/yyyy.
        var partes = string.Join(' ',
            boleto.Nome,
            boleto.Valor.ToString("0.00", CulturaBr),
            boleto.Valor.ToString("0.00", CultureInfo.InvariantCulture),
            boleto.Validade.ToString("dd/MM/yyyy"),
            boleto.NossoNumero,
            boleto.NfeReferente,
            boleto.Estado.Descricao());
        return Normalizar(partes);
    }

    /// <summary>Minúsculas e sem acentos, para uma busca tolerante.</summary>
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

    private void Novo()
    {
        using var formulario = new BoletoForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        BoletoRepository.Inserir(formulario.Boleto);
        Recarregar();
    }

    private void Editar()
    {
        if (BoletoSelecionado is not { } boleto)
            return;
        using var formulario = new BoletoForm(boleto);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        BoletoRepository.Atualizar(formulario.Boleto);
        Recarregar();
    }

    private void Excluir()
    {
        if (BoletoSelecionado is not { } boleto)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o boleto \"{boleto.Nome}\"?\n(O arquivo PDF não é apagado.)",
            "Confirmar exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        BoletoRepository.Excluir(boleto.Id);
        Recarregar();
    }

    private void AlterarEstadoSelecionado(EstadoBoleto estado)
    {
        if (BoletoSelecionado is not { } boleto)
            return;
        BoletoRepository.AlterarEstado(boleto.Id, estado);
        Recarregar();
    }

    private void EnviarPorEmail()
    {
        if (BoletoSelecionado is not { } boleto)
            return;
        using var formulario = new EnviarBoletoForm(boleto);
        formulario.ShowDialog(this);
    }

    private void AbrirConfiguracoes()
    {
        using var formulario = new BoletosConfigForm();
        formulario.ShowDialog(this);
    }

    private void Importar()
    {
        var pasta = BoletosConfigForm.PastaPdfs;
        if (pasta.Length == 0 || !Directory.Exists(pasta))
        {
            MessageBox.Show(this,
                "Configure primeiro a pasta dos PDFs no botão de engrenagem (⚙).",
                "Pasta não configurada",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            AbrirConfiguracoes();
            return;
        }

        using var formulario = new ImportarBoletosForm(pasta);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        foreach (var boleto in formulario.BoletosImportados)
            BoletoRepository.Inserir(boleto);
        Recarregar();

        MessageBox.Show(this,
            $"{formulario.BoletosImportados.Count} boleto(s) importado(s).\n" +
            "Os dados foram lidos automaticamente dos PDFs — confira e, se " +
            "algum campo ficou em branco, complete com dois cliques no boleto.",
            "Importação concluída",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void AbrirPdf()
    {
        if (BoletoSelecionado is not { } boleto)
            return;
        if (boleto.CaminhoArquivo.Length == 0)
        {
            MessageBox.Show(this, "Este boleto não tem um PDF vinculado.", "Sem arquivo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!File.Exists(boleto.CaminhoArquivo))
        {
            MessageBox.Show(this,
                "O arquivo não foi encontrado:\n" + boleto.CaminhoArquivo,
                "Arquivo não encontrado",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        Process.Start(new ProcessStartInfo(boleto.CaminhoArquivo) { UseShellExecute = true });
    }
}
