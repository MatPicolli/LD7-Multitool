using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Gerenciamento (lista + CRUD) dos representantes.</summary>
public class RepresentantesForm : Form
{
    private readonly DataGridView _grade;
    private List<Representante> _representantes = new();

    /// <summary>True se algo mudou (para o chamador atualizar seus combos).</summary>
    public bool HouveAlteracao { get; private set; }

    public RepresentantesForm()
    {
        Text = "Representantes";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 440);
        ClientSize = new Size(620, 440);

        var barra = Estilo.CriarBarraSuperior();
        var botaoNovo = Estilo.BotaoPrimario("+ Novo");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        botaoNovo.Click += (_, _) => Novo();
        botaoEditar.Click += (_, _) => Editar();
        botaoExcluir.Click += (_, _) => Excluir();

        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0) };
        fluxo.Controls.Add(botaoNovo);
        fluxo.Controls.Add(botaoEditar);
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
        _grade.Columns.Add("nome", "Nome");
        _grade.Columns.Add("email", "E-mail");
        _grade.Columns.Add("tel1", "Telefone 1");
        _grade.Columns.Add("tel2", "Telefone 2");
        _grade.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };

        Controls.Add(_grade);
        Controls.Add(barra);

        Recarregar();
    }

    private Representante? Selecionado =>
        _grade.SelectedRows.Count == 0 ? null : (Representante)_grade.SelectedRows[0].Tag!;

    private void Recarregar()
    {
        _representantes = RepresentanteRepository.Listar();
        _grade.Rows.Clear();
        foreach (var r in _representantes)
        {
            var indice = _grade.Rows.Add(r.Nome, r.Email, r.Telefone1, r.Telefone2);
            _grade.Rows[indice].Tag = r;
        }
    }

    private void Novo()
    {
        using var formulario = new RepresentanteForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        RepresentanteRepository.Inserir(formulario.Representante);
        HouveAlteracao = true;
        Recarregar();
    }

    private void Editar()
    {
        if (Selecionado is not { } r)
            return;
        using var formulario = new RepresentanteForm(r);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        RepresentanteRepository.Atualizar(formulario.Representante);
        HouveAlteracao = true;
        Recarregar();
    }

    private void Excluir()
    {
        if (Selecionado is not { } r)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o representante \"{r.Nome}\"?\n" +
            "Clientes vinculados a ele ficarão sem representante.",
            "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        RepresentanteRepository.Excluir(r.Id);
        HouveAlteracao = true;
        Recarregar();
    }
}
