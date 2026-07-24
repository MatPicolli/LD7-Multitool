using System.Globalization;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Tela principal do cadastro de clientes: pesquisa, grade e ações.</summary>
public class ClientesControl : UserControl
{
    private readonly DataGridView _grade;
    private readonly TextBox _campoPesquisa;
    private readonly Label _resumo;
    private List<Cliente> _todos = new();
    private List<Cliente> _visiveis = new();

    public ClientesControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior --------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoNovo = Estilo.BotaoPrimario("+ Novo");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        var botaoPdf = Estilo.BotaoPadrao("Gerar ficha (PDF)");
        var botaoRepresentantes = Estilo.BotaoPadrao("Representantes");
        var botaoConfig = Estilo.BotaoIcone("⚙", "Configurações (importar CSV)");

        botaoNovo.Click += (_, _) => Novo();
        botaoEditar.Click += (_, _) => Editar();
        botaoExcluir.Click += (_, _) => Excluir();
        botaoPdf.Click += (_, _) => GerarPdf();
        botaoRepresentantes.Click += (_, _) => AbrirRepresentantes();
        botaoConfig.Click += (_, _) => AbrirConfiguracoes();

        var fluxoAcoes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0) };
        fluxoAcoes.Controls.Add(botaoNovo);
        fluxoAcoes.Controls.Add(botaoEditar);
        fluxoAcoes.Controls.Add(botaoExcluir);
        fluxoAcoes.Controls.Add(botaoPdf);
        fluxoAcoes.Controls.Add(botaoRepresentantes);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfig.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfig);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Barra de pesquisa -------------------------------------------------
        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Pesquisar por código, razão social, nome fantasia ou CPF/CNPJ...",
            Margin = new Padding(0),
        };
        _campoPesquisa.TextChanged += (_, _) => AtualizarGrade();

        var barraPesquisa = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 6, 12, 6),
        };
        barraPesquisa.Controls.Add(_campoPesquisa);

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
        _grade.Columns.Add("codigo", "Código");
        _grade.Columns.Add("razaoSocial", "Razão Social");
        _grade.Columns.Add("nomeFantasia", "Nome Fantasia");
        _grade.Columns.Add("cpfCnpj", "CPF/CNPJ");
        _grade.Columns.Add("cidadeUf", "Cidade/UF");
        _grade.Columns.Add("representante", "Representante");
        _grade.Columns.Add("ativo", "Ativo");
        _grade.Columns["codigo"]!.FillWeight = 40;
        _grade.Columns["cidadeUf"]!.FillWeight = 70;
        _grade.Columns["ativo"]!.FillWeight = 40;
        _grade.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };

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
        Controls.Add(barraPesquisa);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    private Cliente? ClienteSelecionado =>
        _grade.SelectedRows.Count == 0 ? null : (Cliente)_grade.SelectedRows[0].Tag!;

    private void Recarregar()
    {
        _todos = ClienteRepository.Listar();
        AtualizarGrade();
    }

    private void AtualizarGrade()
    {
        var termo = Normalizar(_campoPesquisa.Text.Trim());
        _visiveis = termo.Length == 0
            ? _todos.ToList()
            : _todos.Where(c => TextoPesquisavel(c).Contains(termo)).ToList();

        _grade.Rows.Clear();
        foreach (var c in _visiveis)
        {
            var indice = _grade.Rows.Add(
                c.Codigo,
                c.RazaoSocial,
                c.NomeFantasia,
                c.DocumentoExibicao,
                string.Join("/", new[] { c.Cidade, c.Uf }.Where(s => s.Length > 0)),
                c.RepresentanteNome,
                c.Ativo ? "Sim" : "Não");

            var linha = _grade.Rows[indice];
            linha.Tag = c;
            if (!c.Ativo)
                linha.DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
        }

        _resumo.Text = $"{_visiveis.Count} cliente(s)";
    }

    private static string TextoPesquisavel(Cliente c) =>
        Normalizar(string.Join(' ', c.Codigo, c.RazaoSocial, c.NomeFantasia, c.Cpf, c.Cnpj));

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
        using var formulario = new ClienteForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        ClienteRepository.Inserir(formulario.Cliente);
        Recarregar();
    }

    private void Editar()
    {
        if (ClienteSelecionado is not { } cliente)
            return;
        using var formulario = new ClienteForm(cliente);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        ClienteRepository.Atualizar(formulario.Cliente);
        Recarregar();
    }

    private void Excluir()
    {
        if (ClienteSelecionado is not { } cliente)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o cliente \"{cliente.RazaoSocial}\"?",
            "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        ClienteRepository.Excluir(cliente.Id);
        Recarregar();
    }

    private void AbrirRepresentantes()
    {
        using var formulario = new RepresentantesForm();
        formulario.ShowDialog(this);
        if (formulario.HouveAlteracao)
            Recarregar(); // nomes de representante exibidos na grade podem ter mudado
    }

    private void AbrirConfiguracoes()
    {
        using var formulario = new ClientesConfigForm();
        formulario.ShowDialog(this);
        if (formulario.HouveAlteracao)
            Recarregar();
    }

    private void GerarPdf()
    {
        if (ClienteSelecionado is not { } cliente)
        {
            MessageBox.Show(this, "Selecione um cliente para gerar a ficha.", "Nenhum cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var representante = cliente.RepresentanteId is { } id
                ? RepresentanteRepository.Listar().FirstOrDefault(r => r.Id == id)
                : null;

            var caminho = FichaClientePdf.Gerar(cliente, representante);

            var resposta = MessageBox.Show(this,
                $"Ficha gerada em:\n{caminho}\n\nAbrir o PDF?",
                "Ficha gerada", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (resposta == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(caminho) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha ao gerar a ficha:\n\n" + ex.Message, "Erro",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
