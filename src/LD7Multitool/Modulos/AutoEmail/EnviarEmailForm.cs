using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Diálogo de envio: escolhe o que mandar (NF-e, NF-e e Boleto, ou outros
/// arquivos). Para NF-e/Boleto mostra um dropdown com todos os PDFs do cliente
/// (mais recente pré-selecionado), permitindo escolher documentos antigos.
/// </summary>
public class EnviarEmailForm : Form
{
    private readonly CadastroEmail _cadastro;
    private readonly ConfigSmtp _config;

    private readonly RadioButton _opcaoNfe;
    private readonly RadioButton _opcaoNfeBoleto;
    private readonly RadioButton _opcaoOutro;
    private readonly Panel _areaArquivos;
    private readonly Label _aviso;
    private readonly TextBox _campoAssunto;
    private readonly TextBox _campoCorpo;
    private readonly Button _botaoEnviar;

    // Controles ativos conforme a opção escolhida (recriados em AtualizarOpcao).
    private ComboBox? _comboNfe;
    private ComboBox? _comboBoleto;
    private ListBox? _listaOutro;

    public EnviarEmailForm(CadastroEmail cadastro)
    {
        _cadastro = cadastro;
        _config = ConfigSmtp.Carregar();

        Text = $"Enviar e-mail — {cadastro.Nome}";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(680, 560);
        ClientSize = new Size(680, 560);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // cliente
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));   // destinatários
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // opções
        tabela.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // arquivos
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // assunto
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));  // mensagem
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));   // botões

        AdicionarLinha(tabela, 0, "Cliente:", new Label
        {
            Text = $"{cadastro.Codigo} — {cadastro.Nome}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI Semibold", 9.75f),
        });

        AdicionarLinha(tabela, 1, "Para:", new Label
        {
            Text = string.Join(";  ", cadastro.Destinatarios),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
        });

        // --- Opções de envio -----------------------------------------------
        _opcaoNfe = new RadioButton { Text = "NF-e", AutoSize = true, Checked = true };
        _opcaoNfeBoleto = new RadioButton { Text = "NF-e e Boleto", AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        _opcaoOutro = new RadioButton { Text = "Outro", AutoSize = true, Margin = new Padding(16, 0, 0, 0) };
        _opcaoNfe.CheckedChanged += (_, _) => AtualizarOpcao();
        _opcaoNfeBoleto.CheckedChanged += (_, _) => AtualizarOpcao();
        _opcaoOutro.CheckedChanged += (_, _) => AtualizarOpcao();

        var painelOpcoes = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
        painelOpcoes.Controls.Add(_opcaoNfe);
        painelOpcoes.Controls.Add(_opcaoNfeBoleto);
        painelOpcoes.Controls.Add(_opcaoOutro);
        AdicionarLinha(tabela, 2, "Enviar:", painelOpcoes);

        // --- Arquivos (conteúdo dinâmico + aviso) --------------------------
        _areaArquivos = new Panel { Dock = DockStyle.Fill };
        _aviso = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            ForeColor = Estilo.CorPerigo,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var painelArquivos = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        painelArquivos.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painelArquivos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        painelArquivos.Controls.Add(_areaArquivos, 0, 0);
        painelArquivos.Controls.Add(_aviso, 0, 1);
        AdicionarLinha(tabela, 3, "Arquivos:", painelArquivos);

        // --- Assunto e mensagem --------------------------------------------
        _campoAssunto = new TextBox { Dock = DockStyle.Fill };
        AdicionarLinha(tabela, 4, "Assunto:", _campoAssunto);

        _campoCorpo = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = "Olá,\r\n\r\nSegue(m) em anexo o(s) documento(s).\r\n\r\nAtenciosamente.",
        };
        AdicionarLinha(tabela, 5, "Mensagem:", _campoCorpo);

        // --- Botões --------------------------------------------------------
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
        };
        _botaoEnviar = Estilo.BotaoPrimario("Enviar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        _botaoEnviar.Click += async (_, _) => await EnviarAsync();
        painelBotoes.Controls.Add(_botaoEnviar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.Controls.Add(painelBotoes, 1, 6);

        Controls.Add(tabela);
        CancelButton = botaoCancelar;

        AtualizarOpcao();
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Padding = new Padding(0, 6, 0, 0),
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    /// <summary>Reconstrói a área de arquivos conforme a opção escolhida.</summary>
    private void AtualizarOpcao()
    {
        // CheckedChanged dispara para o botão que desmarcou e o que marcou;
        // só reagimos ao estado final.
        if (!_opcaoNfe.Checked && !_opcaoNfeBoleto.Checked && !_opcaoOutro.Checked)
            return;

        _comboNfe = _comboBoleto = null;
        _listaOutro = null;
        _areaArquivos.Controls.Clear();
        var avisos = new List<string>();

        if (_opcaoOutro.Checked)
            MontarAreaManual();
        else
            MontarAreaDropdowns(avisos);

        _aviso.Text = string.Join("   |   ", avisos);

        _campoAssunto.Text = _opcaoNfe.Checked
            ? $"NF-e — {_cadastro.Nome}"
            : _opcaoNfeBoleto.Checked
                ? $"NF-e e Boleto — {_cadastro.Nome}"
                : $"Documentos — {_cadastro.Nome}";
    }

    private void MontarAreaDropdowns(List<string> avisos)
    {
        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0),
        };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _comboNfe = CriarComboArquivos("NF-e", _config.PastaNfe, avisos);
        AdicionarDropdown(painel, 0, "NF-e:", _comboNfe);

        if (_opcaoNfeBoleto.Checked)
        {
            _comboBoleto = CriarComboArquivos("Boleto", _config.PastaBoletos, avisos);
            AdicionarDropdown(painel, 1, "Boleto:", _comboBoleto);
        }

        _areaArquivos.Controls.Add(painel);
    }

    private static void AdicionarDropdown(TableLayoutPanel painel, int linha, string rotulo, ComboBox combo)
    {
        painel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        painel.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        combo.Dock = DockStyle.Fill;
        combo.Margin = new Padding(0, 4, 0, 4);
        painel.Controls.Add(combo, 1, linha);
    }

    private ComboBox CriarComboArquivos(string rotulo, string pasta, List<string> avisos)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
        };
        if (string.IsNullOrWhiteSpace(pasta))
        {
            avisos.Add($"Pasta de {rotulo} não configurada (⚙)");
            return combo;
        }
        foreach (var arquivo in LocalizadorArquivos.TodosPdfsDoCliente(pasta, _cadastro.Codigo))
            combo.Items.Add(new ItemArquivo(arquivo));

        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0; // mais recente, mantendo o padrão
        else
            avisos.Add($"{rotulo}: nenhum PDF \"Cliente-{_cadastro.Codigo}\" na pasta");
        return combo;
    }

    private void MontarAreaManual()
    {
        _listaOutro = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };

        var botaoAdicionar = Estilo.BotaoPadrao("Adicionar...");
        var botaoRemover = Estilo.BotaoPadrao("Remover");
        botaoAdicionar.Click += (_, _) =>
        {
            using var dialogo = new OpenFileDialog { Multiselect = true, Title = "Selecionar arquivos" };
            if (dialogo.ShowDialog(this) != DialogResult.OK)
                return;
            foreach (var arquivo in dialogo.FileNames)
                if (!_listaOutro.Items.Contains(arquivo))
                    _listaOutro.Items.Add(arquivo);
        };
        botaoRemover.Click += (_, _) =>
        {
            if (_listaOutro!.SelectedIndex >= 0)
                _listaOutro.Items.RemoveAt(_listaOutro.SelectedIndex);
        };

        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
        };
        painel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        painel.Controls.Add(_listaOutro, 0, 0);
        var barra = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        barra.Controls.Add(botaoAdicionar);
        barra.Controls.Add(botaoRemover);
        painel.Controls.Add(barra, 0, 1);

        _areaArquivos.Controls.Add(painel);
    }

    /// <summary>Anexos conforme a opção atual.</summary>
    private List<string> ObterAnexos()
    {
        if (_opcaoOutro.Checked)
            return _listaOutro?.Items.Cast<string>().ToList() ?? new List<string>();

        var anexos = new List<string>();
        if (_comboNfe?.SelectedItem is ItemArquivo nfe)
            anexos.Add(nfe.Caminho);
        if (_comboBoleto?.SelectedItem is ItemArquivo boleto)
            anexos.Add(boleto.Caminho);
        return anexos;
    }

    private async Task EnviarAsync()
    {
        var arquivos = ObterAnexos();
        if (arquivos.Count == 0)
        {
            MessageBox.Show(this,
                "Nenhum arquivo para enviar. Verifique as pastas configuradas " +
                "ou use a opção \"Outro\" para escolher manualmente.",
                "Sem anexos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _botaoEnviar.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await EnvioEmailService.EnviarAsync(
                _config, _cadastro.Destinatarios, _campoAssunto.Text, _campoCorpo.Text, arquivos);
            HistoricoEmailRepository.Registrar(
                _cadastro.Destinatarios, _campoAssunto.Text, arquivos.Count);
            MessageBox.Show(this,
                $"E-mail enviado para {_cadastro.Destinatarios.Count} destinatário(s)!",
                "Envio concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
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

    /// <summary>Item de dropdown de arquivo: mostra o nome, guarda o caminho.</summary>
    private sealed record ItemArquivo(string Caminho)
    {
        public override string ToString() => Path.GetFileName(Caminho);
    }
}
