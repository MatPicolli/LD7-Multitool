using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Tela principal do Auto-Email: lista de cadastros à esquerda,
/// detalhes do cadastro selecionado à direita e ações na barra superior.
/// </summary>
public class AutoEmailControl : UserControl
{
    private readonly ListBox _listaCadastros;
    private readonly TextBox _detalhes;
    private readonly Button _botaoEnviar;
    private List<CadastroEmail> _cadastros = new();

    public AutoEmailControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        var barraSuperior = Estilo.CriarBarraSuperior();

        var botaoNovo = Estilo.BotaoPrimario("+ Novo");
        _botaoEnviar = Estilo.BotaoPrimario("Enviar e-mail");
        var botaoEditar = Estilo.BotaoPadrao("Editar");
        var botaoExcluir = Estilo.BotaoPerigo("Excluir");
        var botaoConfigSmtp = Estilo.BotaoIcone("⚙", "Configurar servidor SMTP");

        botaoNovo.Click += (_, _) => Novo();
        botaoEditar.Click += (_, _) => Editar();
        botaoExcluir.Click += (_, _) => Excluir();
        _botaoEnviar.Click += async (_, _) => await EnviarAsync();
        botaoConfigSmtp.Click += (_, _) =>
        {
            using var formulario = new ConfigSmtpForm();
            formulario.ShowDialog(this);
        };

        var fluxoAcoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Padding = new Padding(0),
        };
        fluxoAcoes.Controls.Add(botaoNovo);
        fluxoAcoes.Controls.Add(_botaoEnviar);
        fluxoAcoes.Controls.Add(botaoEditar);
        fluxoAcoes.Controls.Add(botaoExcluir);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0) };
        botaoConfigSmtp.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfigSmtp);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        var divisao = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
        };
        // SplitterDistance só pode ser definido depois que o controle tem tamanho real.
        Load += (_, _) => divisao.SplitterDistance = 240;

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
        divisao.Panel1.Controls.Add(_listaCadastros);

        _detalhes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BorderStyle = BorderStyle.None,
            BackColor = Estilo.CorSuperficie,
        };
        divisao.Panel1.Padding = new Padding(8);
        divisao.Panel2.Padding = new Padding(12);
        divisao.Panel1.BackColor = Estilo.CorSuperficie;
        divisao.Panel2.BackColor = Estilo.CorSuperficie;
        divisao.Panel2.Controls.Add(_detalhes);

        Controls.Add(divisao);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    private CadastroEmail? CadastroSelecionado =>
        _listaCadastros.SelectedIndex < 0 ? null : _cadastros[_listaCadastros.SelectedIndex];

    private void Recarregar(long? selecionarId = null)
    {
        _cadastros = CadastroEmailRepository.Listar();

        _listaCadastros.Items.Clear();
        foreach (var cadastro in _cadastros)
            _listaCadastros.Items.Add(cadastro.Nome);

        if (_cadastros.Count > 0)
        {
            var indice = selecionarId is null
                ? 0
                : Math.Max(0, _cadastros.FindIndex(c => c.Id == selecionarId));
            _listaCadastros.SelectedIndex = indice;
        }

        MostrarDetalhes();
    }

    private void MostrarDetalhes()
    {
        if (CadastroSelecionado is not { } cadastro)
        {
            _detalhes.Text = "Nenhum cadastro selecionado." + Environment.NewLine +
                "Use o botão \"Novo\" para criar um cadastro de envio.";
            return;
        }

        var linhas = new List<string>
        {
            $"Cadastro: {cadastro.Nome}",
            "",
            $"Assunto: {cadastro.Assunto}",
            "",
            "Destinatários:",
        };
        linhas.AddRange(cadastro.Destinatarios.Select(d => $"  • {d}"));
        linhas.Add("");
        linhas.Add("Arquivos anexados:");
        if (cadastro.Arquivos.Count == 0)
            linhas.Add("  (nenhum)");
        else
            linhas.AddRange(cadastro.Arquivos.Select(a =>
                $"  • {a}{(File.Exists(a) ? "" : "  [NÃO ENCONTRADO]")}"));
        linhas.Add("");
        linhas.Add("Mensagem:");
        linhas.Add(cadastro.Corpo);

        _detalhes.Text = string.Join(Environment.NewLine, linhas);
    }

    private void Novo()
    {
        using var formulario = new CadastroEmailForm();
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        CadastroEmailRepository.Salvar(formulario.Cadastro);
        Recarregar(formulario.Cadastro.Id);
    }

    private void Editar()
    {
        if (CadastroSelecionado is not { } cadastro)
            return;
        using var formulario = new CadastroEmailForm(cadastro);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;
        CadastroEmailRepository.Salvar(formulario.Cadastro);
        Recarregar(formulario.Cadastro.Id);
    }

    private void Excluir()
    {
        if (CadastroSelecionado is not { } cadastro)
            return;
        var resposta = MessageBox.Show(this,
            $"Excluir o cadastro \"{cadastro.Nome}\"?",
            "Confirmar exclusão",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;
        CadastroEmailRepository.Excluir(cadastro.Id);
        Recarregar();
    }

    private async Task EnviarAsync()
    {
        if (CadastroSelecionado is not { } cadastro)
            return;

        var resposta = MessageBox.Show(this,
            $"Enviar \"{cadastro.Nome}\" para {cadastro.Destinatarios.Count} destinatário(s)?",
            "Confirmar envio",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (resposta != DialogResult.Yes)
            return;

        _botaoEnviar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await EnvioEmailService.EnviarAsync(ConfigSmtp.Carregar(), cadastro);
            MessageBox.Show(this, "E-mail enviado com sucesso!", "Envio concluído",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha ao enviar o e-mail:\n\n" + ex.Message, "Erro no envio",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _botaoEnviar.Enabled = true;
            UseWaitCursor = false;
        }
    }
}
