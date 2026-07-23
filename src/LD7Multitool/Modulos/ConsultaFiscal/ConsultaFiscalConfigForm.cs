using System.Drawing.Printing;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>Configurações da consulta: certificado A1, ambiente, impressora e endpoints.</summary>
public class ConsultaFiscalConfigForm : Form
{
    private readonly TextBox _campoCertificado;
    private readonly TextBox _campoSenha;
    private readonly ComboBox _campoAmbiente;
    private readonly CheckBox _campoAutoImprimir;
    private readonly ComboBox _campoImpressora;
    private readonly TextBox _campoUrlNfe;
    private readonly TextBox _campoUrlCte;

    public ConsultaFiscalConfigForm()
    {
        Text = "Configurações — Consulta NF-e/CT-e";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 470);

        var config = ConsultaFiscalConfig.Carregar();

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _campoCertificado = new TextBox { Dock = DockStyle.Fill, Text = config.CaminhoCertificado };
        _campoSenha = new TextBox { Dock = DockStyle.Fill, Text = config.SenhaCertificado, UseSystemPasswordChar = true };

        _campoAmbiente = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        _campoAmbiente.Items.AddRange(new object[] { "Produção", "Homologação" });
        _campoAmbiente.SelectedIndex = config.Producao ? 0 : 1;

        _campoAutoImprimir = new CheckBox
        {
            Dock = DockStyle.Fill,
            Text = "Imprimir automaticamente (desligado: aguarda Enter p/ inserir a nota)",
            Checked = config.ImprimirAutomaticamente,
        };

        _campoImpressora = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        _campoImpressora.Items.Add("(impressora padrão)");
        foreach (string nome in PrinterSettings.InstalledPrinters)
            _campoImpressora.Items.Add(nome);
        _campoImpressora.SelectedIndex = Math.Max(0, _campoImpressora.Items.IndexOf(config.Impressora));

        _campoUrlNfe = new TextBox { Dock = DockStyle.Fill, Text = config.UrlNfe, PlaceholderText = "(automático pela UF da chave)" };
        _campoUrlCte = new TextBox { Dock = DockStyle.Fill, Text = config.UrlCte, PlaceholderText = "(automático pela UF da chave)" };

        AdicionarLinha(tabela, 0, "Certificado A1:", CriarSeletorArquivo(_campoCertificado));
        AdicionarLinha(tabela, 1, "Senha:", _campoSenha);
        AdicionarLinha(tabela, 2, "Ambiente:", _campoAmbiente);
        AdicionarLinha(tabela, 3, "", _campoAutoImprimir);
        AdicionarLinha(tabela, 4, "Impressora:", _campoImpressora);
        AdicionarLinha(tabela, 5, "Forçar URL NF-e:", _campoUrlNfe);
        AdicionarLinha(tabela, 6, "Forçar URL CT-e:", _campoUrlCte);
        AdicionarLinha(tabela, 7, "", new Label
        {
            Text = "URLs em branco = endpoint escolhido automaticamente pela UF da chave.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        });

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(painelBotoes, 1, 8);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    private Control CriarSeletorArquivo(TextBox campo)
    {
        var botao = Estilo.BotaoPadrao("...");
        botao.AutoSize = false;
        botao.MinimumSize = Size.Empty;
        botao.Size = new Size(36, 29);
        botao.Margin = new Padding(4, 0, 0, 0);
        botao.Padding = new Padding(0);
        botao.Click += (_, _) =>
        {
            using var dialogo = new OpenFileDialog
            {
                Title = "Selecionar o certificado A1",
                Filter = "Certificado (*.pfx;*.p12)|*.pfx;*.p12|Todos os arquivos (*.*)|*.*",
            };
            if (dialogo.ShowDialog(this) == DialogResult.OK)
                campo.Text = dialogo.FileName;
        };

        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        painel.Controls.Add(campo, 0, 0);
        painel.Controls.Add(botao, 1, 0);
        return painel;
    }

    private void Salvar()
    {
        if (_campoCertificado.Text.Trim().Length > 0 && !File.Exists(_campoCertificado.Text.Trim()))
        {
            MessageBox.Show(this, "O arquivo do certificado não existe.", "Certificado",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var config = new ConsultaFiscalConfig
        {
            CaminhoCertificado = _campoCertificado.Text.Trim(),
            SenhaCertificado = _campoSenha.Text,
            Ambiente = _campoAmbiente.SelectedIndex == 1 ? 2 : 1,
            ImprimirAutomaticamente = _campoAutoImprimir.Checked,
            Impressora = _campoImpressora.SelectedIndex <= 0 ? "" : (string)_campoImpressora.SelectedItem!,
            UrlNfe = _campoUrlNfe.Text.Trim(),
            UrlCte = _campoUrlCte.Text.Trim(),
        };
        config.Gravar();

        DialogResult = DialogResult.OK;
        Close();
    }
}
