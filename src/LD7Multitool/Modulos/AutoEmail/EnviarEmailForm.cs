using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>
/// Diálogo de envio: escolhe o que mandar (NF-e, NF-e e Boleto, ou outros
/// arquivos), resolve os PDFs mais recentes do cliente nas pastas
/// configuradas e envia para os destinatários do cadastro.
/// </summary>
public class EnviarEmailForm : Form
{
    private readonly CadastroEmail _cadastro;
    private readonly ConfigSmtp _config;

    private readonly RadioButton _opcaoNfe;
    private readonly RadioButton _opcaoNfeBoleto;
    private readonly RadioButton _opcaoOutro;
    private readonly ListBox _listaArquivos;
    private readonly Button _botaoAdicionarArquivo;
    private readonly Button _botaoRemoverArquivo;
    private readonly Label _aviso;
    private readonly TextBox _campoAssunto;
    private readonly TextBox _campoCorpo;
    private readonly Button _botaoEnviar;

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

        // --- Arquivos ------------------------------------------------------
        _listaArquivos = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };

        _botaoAdicionarArquivo = Estilo.BotaoPadrao("Adicionar...");
        _botaoRemoverArquivo = Estilo.BotaoPadrao("Remover");
        _botaoAdicionarArquivo.Click += (_, _) => AdicionarArquivos();
        _botaoRemoverArquivo.Click += (_, _) => RemoverArquivo();

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
            RowCount = 3,
            Margin = new Padding(0),
        };
        painelArquivos.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        painelArquivos.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        painelArquivos.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        painelArquivos.Controls.Add(_listaArquivos, 0, 0);
        painelArquivos.Controls.Add(_aviso, 0, 1);

        var barraArquivos = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0) };
        barraArquivos.Controls.Add(_botaoAdicionarArquivo);
        barraArquivos.Controls.Add(_botaoRemoverArquivo);
        painelArquivos.Controls.Add(barraArquivos, 0, 2);

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

    /// <summary>Reresolve os arquivos e o assunto quando a opção de envio muda.</summary>
    private void AtualizarOpcao()
    {
        // CheckedChanged dispara para o botão que desmarcou e o que marcou;
        // só reagimos ao estado final.
        if (!_opcaoNfe.Checked && !_opcaoNfeBoleto.Checked && !_opcaoOutro.Checked)
            return;

        _listaArquivos.Items.Clear();
        var avisos = new List<string>();

        var manual = _opcaoOutro.Checked;
        _botaoAdicionarArquivo.Visible = manual;
        _botaoRemoverArquivo.Visible = manual;

        if (!manual)
        {
            ResolverArquivo("NF-e", _config.PastaNfe, avisos);
            if (_opcaoNfeBoleto.Checked)
                ResolverArquivo("Boleto", _config.PastaBoletos, avisos);
        }

        _aviso.Text = string.Join("   |   ", avisos);

        _campoAssunto.Text = _opcaoNfe.Checked
            ? $"NF-e — {_cadastro.Nome}"
            : _opcaoNfeBoleto.Checked
                ? $"NF-e e Boleto — {_cadastro.Nome}"
                : $"Documentos — {_cadastro.Nome}";
    }

    private void ResolverArquivo(string rotulo, string pasta, List<string> avisos)
    {
        if (string.IsNullOrWhiteSpace(pasta))
        {
            avisos.Add($"Pasta de {rotulo} não configurada (⚙)");
            return;
        }

        var arquivo = LocalizadorArquivos.UltimoPdfDoCliente(pasta, _cadastro.Codigo);
        if (arquivo is null)
            avisos.Add($"{rotulo}: nenhum PDF \"Cliente-{_cadastro.Codigo}\" na pasta");
        else
            _listaArquivos.Items.Add(arquivo);
    }

    private void AdicionarArquivos()
    {
        using var dialogo = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Selecionar arquivos para enviar",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;
        foreach (var arquivo in dialogo.FileNames)
        {
            if (!_listaArquivos.Items.Contains(arquivo))
                _listaArquivos.Items.Add(arquivo);
        }
    }

    private void RemoverArquivo()
    {
        if (_listaArquivos.SelectedIndex >= 0)
            _listaArquivos.Items.RemoveAt(_listaArquivos.SelectedIndex);
    }

    private async Task EnviarAsync()
    {
        var arquivos = _listaArquivos.Items.Cast<string>().ToList();
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
}
