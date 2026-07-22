using System.Globalization;
using System.Text;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Tela principal do Auto-Email: pesquisa (por nome ou código) e lista de
/// clientes à esquerda, detalhes à direita e ações na barra superior.
/// </summary>
public class AutoEmailControl : UserControl
{
    private readonly TextBox _campoPesquisa;
    private readonly ListBox _listaCadastros;
    private readonly TextBox _detalhes;
    private readonly ListBox _historico;
    private List<CadastroEmail> _todos = new();
    private List<CadastroEmail> _filtrados = new();

    public AutoEmailControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior ------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoNovo = Estilo.BotaoPrimario("+ Novo");
        var botaoEnviar = Estilo.BotaoPrimario("Enviar e-mail");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        var botaoConfig = Estilo.BotaoIcone("⚙", "Configurações (SMTP e pastas)");

        botaoNovo.Click += (_, _) => Novo();
        botaoEnviar.Click += (_, _) => Enviar();
        botaoEditar.Click += (_, _) => Editar();
        botaoExcluir.Click += (_, _) => Excluir();
        botaoConfig.Click += (_, _) =>
        {
            using var formulario = new AutoEmailConfigForm();
            if (formulario.ShowDialog(this) == DialogResult.OK)
                MostrarDetalhes(); // pastas podem ter mudado
        };

        var fluxoAcoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0),
        };
        fluxoAcoes.Controls.Add(botaoNovo);
        fluxoAcoes.Controls.Add(botaoEnviar);
        fluxoAcoes.Controls.Add(botaoEditar);
        fluxoAcoes.Controls.Add(botaoExcluir);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfig.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfig);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Painéis -------------------------------------------------------
        var divisao = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
        };
        Load += (_, _) => divisao.SplitterDistance = 300;

        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "Pesquisar por nome ou código...",
        };
        _campoPesquisa.TextChanged += (_, _) => AplicarFiltro();

        _listaCadastros = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Estilo.CorSuperficie,
            ItemHeight = 28,
            IntegralHeight = false,
        };
        _listaCadastros.SelectedIndexChanged += (_, _) => MostrarDetalhes();
        _listaCadastros.DoubleClick += (_, _) => Editar();

        divisao.Panel1.Padding = new Padding(8);
        divisao.Panel1.BackColor = Estilo.CorSuperficie;
        divisao.Panel1.Controls.Add(_listaCadastros);
        divisao.Panel1.Controls.Add(_campoPesquisa);

        _detalhes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Estilo.CorSuperficie,
        };
        divisao.Panel2.Padding = new Padding(12);
        divisao.Panel2.BackColor = Estilo.CorSuperficie;
        divisao.Panel2.Controls.Add(_detalhes);

        // --- Histórico de e-mails enviados ---------------------------------
        _historico = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackColor = Estilo.CorSuperficie,
            ForeColor = Estilo.CorTextoSuave,
            IntegralHeight = false,
        };
        var painelHistorico = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 150,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 6, 12, 10),
        };
        painelHistorico.Paint += (_, e) =>
            e.Graphics.DrawLine(new Pen(Estilo.CorBorda), 0, 0, painelHistorico.Width, 0);
        painelHistorico.Controls.Add(_historico);
        painelHistorico.Controls.Add(new Label
        {
            Text = "Últimos e-mails enviados",
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI Semibold", 9.75f),
            ForeColor = Estilo.CorTexto,
        });

        // Ordem de docking: barra no topo, histórico no rodapé, divisão preenche.
        Controls.Add(divisao);
        Controls.Add(painelHistorico);
        Controls.Add(barraSuperior);

        // Atualiza o histórico ao voltar para a aba (ex.: após enviar um boleto).
        VisibleChanged += (_, _) => { if (Visible) AtualizarHistorico(); };

        RecarregarDoBanco();
        AtualizarHistorico();
    }

    private void AtualizarHistorico()
    {
        _historico.Items.Clear();
        foreach (var email in HistoricoEmailRepository.ListarRecentes(20))
        {
            _historico.Items.Add(
                $"{email.EnviadoEm:dd/MM/yyyy HH:mm}   ·   {email.Para}   ·   " +
                $"{email.Assunto}   ({email.Anexos} anexo(s))");
        }
        if (_historico.Items.Count == 0)
            _historico.Items.Add("(nenhum e-mail enviado ainda)");
    }

    private CadastroEmail? CadastroSelecionado =>
        _listaCadastros.SelectedIndex < 0 ? null : _filtrados[_listaCadastros.SelectedIndex];

    private void RecarregarDoBanco(long? selecionarId = null)
    {
        _todos = CadastroEmailRepository.Listar();
        AplicarFiltro(selecionarId);
    }

    /// <summary>
    /// Filtro "fuzzy": aceita busca exata por código, prefixo, trecho no
    /// meio do nome e até letras fora de sequência (ex.: "kest" acha
    /// "KEISLI ART ESTOFADOS"), ignorando acentos e maiúsculas.
    /// </summary>
    private void AplicarFiltro(long? selecionarId = null)
    {
        var termo = Normalizar(_campoPesquisa.Text.Trim());

        _filtrados = termo.Length == 0
            ? _todos.ToList()
            : _todos
                .Select(c => (Cadastro: c, Pontos: Pontuar(c, termo)))
                .Where(x => x.Pontos >= 0)
                .OrderBy(x => x.Pontos)
                .ThenBy(x => x.Cadastro.Nome)
                .Select(x => x.Cadastro)
                .ToList();

        _listaCadastros.Items.Clear();
        foreach (var cadastro in _filtrados)
            _listaCadastros.Items.Add($"{cadastro.Codigo} — {cadastro.Nome}");

        if (_filtrados.Count > 0)
        {
            var indice = selecionarId is null
                ? 0
                : Math.Max(0, _filtrados.FindIndex(c => c.Id == selecionarId));
            _listaCadastros.SelectedIndex = indice;
        }

        MostrarDetalhes();
    }

    private static int Pontuar(CadastroEmail cadastro, string termo)
    {
        var codigo = Normalizar(cadastro.Codigo);
        var nome = Normalizar(cadastro.Nome);

        if (codigo == termo) return 0;
        if (codigo.StartsWith(termo)) return 1;
        if (nome.StartsWith(termo)) return 2;
        if (nome.Contains(termo) || codigo.Contains(termo)) return 3;
        if (EhSubsequencia(termo, nome)) return 4;
        return -1;
    }

    /// <summary>Todas as letras do termo aparecem no alvo, na mesma ordem.</summary>
    private static bool EhSubsequencia(string termo, string alvo)
    {
        var i = 0;
        foreach (var caractere in alvo)
        {
            if (i < termo.Length && caractere == termo[i])
                i++;
        }
        return i == termo.Length;
    }

    /// <summary>Minúsculas e sem acentos, para comparação tolerante.</summary>
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

    private void MostrarDetalhes()
    {
        if (CadastroSelecionado is not { } cadastro)
        {
            _detalhes.Text = "Nenhum cliente selecionado." + Environment.NewLine +
                "Use o botão \"+ Novo\" para cadastrar um cliente.";
            return;
        }

        var config = ConfigSmtp.Carregar();
        var nfe = LocalizadorArquivos.UltimoPdfDoCliente(config.PastaNfe, cadastro.Codigo);
        var boleto = LocalizadorArquivos.UltimoPdfDoCliente(config.PastaBoletos, cadastro.Codigo);

        var linhas = new List<string>
        {
            $"Código: {cadastro.Codigo}",
            $"Nome: {cadastro.Nome}",
            "",
            "E-mail(s):",
        };
        linhas.AddRange(cadastro.Destinatarios.Select(d => $"  • {d}"));
        linhas.Add("");
        linhas.Add($"Arquivos mais recentes com \"Cliente-{cadastro.Codigo}\":");
        linhas.Add($"  NF-e:   {(nfe is null ? "(nenhum encontrado)" : Path.GetFileName(nfe))}");
        linhas.Add($"  Boleto: {(boleto is null ? "(nenhum encontrado)" : Path.GetFileName(boleto))}");

        _detalhes.Text = string.Join(Environment.NewLine, linhas);
    }

    private void Novo()
    {
        using var formulario = new CadastroEmailForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        CadastroEmailRepository.Salvar(formulario.Cadastro);
        RecarregarDoBanco(formulario.Cadastro.Id);
    }

    private void Editar()
    {
        if (CadastroSelecionado is not { } cadastro)
            return;
        using var formulario = new CadastroEmailForm(cadastro);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        CadastroEmailRepository.Salvar(formulario.Cadastro);
        RecarregarDoBanco(formulario.Cadastro.Id);
    }

    private void Excluir()
    {
        if (CadastroSelecionado is not { } cadastro)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o cliente \"{cadastro.Nome}\"?",
            "Confirmar exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        CadastroEmailRepository.Excluir(cadastro.Id);
        RecarregarDoBanco();
    }

    private void Enviar()
    {
        if (CadastroSelecionado is not { } cadastro)
        {
            MessageBox.Show(this, "Selecione um cliente para enviar.", "Nenhum cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var formulario = new EnviarEmailForm(cadastro);
        if (formulario.ShowDialog(this) == DialogResult.OK)
            AtualizarHistorico();
    }
}
