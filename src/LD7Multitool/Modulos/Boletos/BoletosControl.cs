using System.Globalization;

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

        var barraSuperior = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8, 8, 8, 0),
        };

        var botaoNovo = new Button { Text = "Novo", Width = 90 };
        var botaoEditar = new Button { Text = "Editar", Width = 90 };
        var botaoExcluir = new Button { Text = "Excluir", Width = 90 };
        var botaoMarcarPago = new Button { Text = "Marcar como pago", Width = 140 };
        var botaoCancelarBoleto = new Button { Text = "Cancelar boleto", Width = 120 };

        botaoNovo.Click += (_, _) => Novo();
        botaoEditar.Click += (_, _) => Editar();
        botaoExcluir.Click += (_, _) => Excluir();
        botaoMarcarPago.Click += (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Pago);
        botaoCancelarBoleto.Click += (_, _) => AlterarEstadoSelecionado(EstadoBoleto.Cancelado);

        _filtroEstado = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 130,
            Margin = new Padding(16, 3, 3, 3),
        };
        _filtroEstado.Items.Add("Todos");
        foreach (EstadoBoleto estado in Enum.GetValues<EstadoBoleto>())
            _filtroEstado.Items.Add(estado.Descricao());
        _filtroEstado.SelectedIndex = 0;
        _filtroEstado.SelectedIndexChanged += (_, _) => Recarregar();

        barraSuperior.Controls.Add(botaoNovo);
        barraSuperior.Controls.Add(botaoEditar);
        barraSuperior.Controls.Add(botaoExcluir);
        barraSuperior.Controls.Add(botaoMarcarPago);
        barraSuperior.Controls.Add(botaoCancelarBoleto);
        barraSuperior.Controls.Add(new Label
        {
            Text = "Filtrar estado:",
            AutoSize = true,
            Margin = new Padding(24, 8, 0, 0),
        });
        barraSuperior.Controls.Add(_filtroEstado);

        _grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            BackgroundColor = SystemColors.Window,
        };
        _grade.Columns.Add("nome", "Nome");
        _grade.Columns.Add("valor", "Valor");
        _grade.Columns.Add("validade", "Validade");
        _grade.Columns.Add("nossoNumero", "Nosso número");
        _grade.Columns.Add("nfeReferente", "NF-e referente");
        _grade.Columns.Add("estado", "Estado");
        _grade.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0) Editar();
        };

        _resumo = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
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
                boleto.Estado.Descricao());

            var linha = _grade.Rows[indice];
            linha.Tag = boleto;
            if (boleto.Estado == EstadoBoleto.Pago)
                linha.DefaultCellStyle.ForeColor = Color.DarkGreen;
            else if (boleto.Estado == EstadoBoleto.Cancelado)
                linha.DefaultCellStyle.ForeColor = Color.Gray;
            else if (boleto.Vencido)
                linha.DefaultCellStyle.ForeColor = Color.Firebrick;
        }

        var totalAberto = _boletos
            .Where(b => b.Estado == EstadoBoleto.Aberto)
            .Sum(b => b.Valor);
        _resumo.Text = $"{_boletos.Count} boleto(s) — total em aberto: {totalAberto.ToString("C2", CulturaBr)}";
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
            $"Excluir o boleto \"{boleto.Nome}\"?",
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
}
