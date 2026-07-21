using System.Diagnostics;
using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>Tela principal do gerenciador de boletos: grade com filtro e ações.</summary>
public class BoletosControl : UserControl
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly DataGridView _grade;
    private readonly ComboBox _filtroEstado;
    private readonly Label _resumo;
    private List<Boleto> _boletos = new();

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

        _filtroEstado = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 120,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(6, 4, 0, 0),
        };
        _filtroEstado.Items.Add("Todos");
        foreach (EstadoBoleto estado in Enum.GetValues<EstadoBoleto>())
            _filtroEstado.Items.Add(estado.Descricao());
        _filtroEstado.SelectedIndex = 0;
        _filtroEstado.SelectedIndexChanged += (_, _) => Recarregar();

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
        fluxoAcoes.Controls.Add(new Label
        {
            Text = "Estado:",
            AutoSize = true,
            ForeColor = Estilo.CorTextoSuave,
            Margin = new Padding(12, 8, 0, 0),
        });
        fluxoAcoes.Controls.Add(_filtroEstado);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0) };
        botaoConfiguracoes.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfiguracoes);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

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
        _grade.Columns.Add("validade", "Validade");
        _grade.Columns.Add("nossoNumero", "Nosso número");
        _grade.Columns.Add("nfeReferente", "NF-e referente");
        _grade.Columns.Add("estado", "Estado");
        _grade.Columns.Add("arquivo", "PDF");
        _grade.Columns["arquivo"]!.FillWeight = 30;
        _grade.Columns["valor"]!.FillWeight = 55;
        _grade.Columns["validade"]!.FillWeight = 55;
        _grade.Columns["estado"]!.FillWeight = 50;
        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Editar();
        };

        // Ações de estado ficam no menu de contexto para não lotar a barra.
        var menuContexto = new ContextMenuStrip();
        menuContexto.Items.Add("Editar", null, (_, _) => Editar());
        menuContexto.Items.Add("Marcar como pago", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Pago));
        menuContexto.Items.Add("Cancelar boleto", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Cancelado));
        menuContexto.Items.Add("Reabrir boleto", null, (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Aberto));
        menuContexto.Items.Add(new ToolStripSeparator());
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

        Controls.Add(_grade);
        Controls.Add(_resumo);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    private EstadoBoleto? FiltroSelecionado =>
        _filtroEstado.SelectedIndex <= 0 ? null : (EstadoBoleto)(_filtroEstado.SelectedIndex - 1);

    private Boleto? BoletoSelecionado =>
        _grade.SelectedRows.Count == 0 ? null : (Boleto)_grade.SelectedRows[0].Tag!;

    private void Recarregar()
    {
        _boletos = BoletoRepository.Listar(FiltroSelecionado);

        _grade.Rows.Clear();
        foreach (var boleto in _boletos)
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
            if (boleto.Estado == EstadoBoleto.Pago)
                linha.DefaultCellStyle.ForeColor = Color.FromArgb(30, 130, 76);
            else if (boleto.Estado == EstadoBoleto.Cancelado)
                linha.DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
            else if (boleto.Vencido)
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
        }

        var totalAberto = _boletos
            .Where(b => b.Estado == EstadoBoleto.Aberto)
            .Sum(b => b.Valor);
        _resumo.Text = $"{_boletos.Count} boleto(s) — total em aberto: {totalAberto.ToString("C2", CulturaBr)}" +
            "   |   Clique-direito num boleto para mais ações";
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
