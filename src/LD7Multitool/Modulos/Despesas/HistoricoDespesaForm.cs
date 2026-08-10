using System.Diagnostics;
using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>Todas as contas já lançadas de um item de despesa, da mais nova para a mais antiga.</summary>
public class HistoricoDespesaForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly Despesa _despesa;
    private readonly DataGridView _grade;
    private readonly Label _resumo;

    public HistoricoDespesaForm(Despesa despesa)
    {
        _despesa = despesa;

        Text = "Contas — " + despesa.Nome;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(880, 520);
        ClientSize = new Size(880, 520);

        var barra = Estilo.CriarBarraSuperior();
        var botaoNovo = Estilo.BotaoPrimario("+ Lançar conta");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoPago = Estilo.BotaoPadrao("Marcar como pago");
        var botaoAbrir = Estilo.BotaoPadrao("Abrir PDF");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");

        botaoNovo.Click += (_, _) => Novo();
        botaoEditar.Click += (_, _) => Editar();
        botaoPago.Click += (_, _) => AlterarSituacao(SituacaoDespesa.Pago);
        botaoAbrir.Click += (_, _) => AbrirPdf();
        botaoExcluir.Click += (_, _) => Excluir();

        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        fluxo.Controls.Add(botaoNovo);
        fluxo.Controls.Add(botaoEditar);
        fluxo.Controls.Add(botaoPago);
        fluxo.Controls.Add(botaoAbrir);
        fluxo.Controls.Add(botaoExcluir);
        barra.Controls.Add(fluxo);

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
        _grade.Columns.Add("competencia", "Competência");
        _grade.Columns.Add("vencimento", "Vencimento");
        _grade.Columns.Add("valor", "Valor");
        _grade.Columns.Add("situacao", "Situação");
        _grade.Columns.Add("origem", "Origem");
        _grade.Columns.Add("arquivo", "PDF");
        _grade.Columns["arquivo"]!.FillWeight = 30;
        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Editar();
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
        Controls.Add(barra);

        Recarregar();
    }

    private LancamentoDespesa? Selecionado =>
        _grade.SelectedRows.Count == 0 ? null : (LancamentoDespesa)_grade.SelectedRows[0].Tag!;

    private void Recarregar()
    {
        var lancamentos = LancamentoDespesaRepository.ListarPorDespesa(_despesa.Id);

        _grade.Rows.Clear();
        foreach (var lancamento in lancamentos)
        {
            var indice = _grade.Rows.Add(
                lancamento.CompetenciaFormatada,
                lancamento.Vencimento.ToString("dd/MM/yyyy"),
                lancamento.Valor.ToString("C2", CulturaBr),
                lancamento.Situacao.Descricao(),
                lancamento.Origem.Descricao(),
                lancamento.CaminhoArquivo.Length > 0 ? "📄" : "");

            var linha = _grade.Rows[indice];
            linha.Tag = lancamento;
            if (lancamento.Situacao.CorTexto() is { } cor)
                linha.DefaultCellStyle.ForeColor = cor;
            else if (lancamento.Vencido)
                linha.DefaultCellStyle.ForeColor = Estilo.CorPerigo;
        }

        var emAberto = lancamentos.Where(l => l.Situacao == SituacaoDespesa.Aberto).Sum(l => l.Valor);
        _resumo.Text = $"{lancamentos.Count} conta(s) — em aberto: {emAberto.ToString("C2", CulturaBr)}";
    }

    private void Novo()
    {
        using var formulario = new LancamentoDespesaForm(_despesa);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        LancamentoDespesaRepository.Inserir(formulario.Lancamento);
        Recarregar();
    }

    private void Editar()
    {
        if (Selecionado is not { } lancamento)
            return;
        using var formulario = new LancamentoDespesaForm(_despesa, lancamento);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        LancamentoDespesaRepository.Atualizar(formulario.Lancamento);
        Recarregar();
    }

    private void AlterarSituacao(SituacaoDespesa situacao)
    {
        if (Selecionado is not { } lancamento)
            return;
        LancamentoDespesaRepository.AlterarSituacao(lancamento.Id, situacao);
        Recarregar();
    }

    private void Excluir()
    {
        if (Selecionado is not { } lancamento)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir a conta de {lancamento.CompetenciaFormatada} " +
            $"({lancamento.Valor.ToString("C2", CulturaBr)})?\n(O arquivo PDF não é apagado.)",
            "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        LancamentoDespesaRepository.Excluir(lancamento.Id);
        Recarregar();
    }

    private void AbrirPdf()
    {
        if (Selecionado is not { } lancamento)
            return;
        if (lancamento.CaminhoArquivo.Length == 0 || !File.Exists(lancamento.CaminhoArquivo))
        {
            MessageBox.Show(this, "Esta conta não tem um PDF disponível.", "Sem arquivo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(lancamento.CaminhoArquivo) { UseShellExecute = true });
    }
}
