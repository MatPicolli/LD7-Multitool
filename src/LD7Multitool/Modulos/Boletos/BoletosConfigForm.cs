using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>Configurações do módulo Boletos (pastas dos PDFs de boletos e NF-e).</summary>
public class BoletosConfigForm : Form
{
    public const string ChavePastaPdfs = "boletos_pasta_pdfs";

    // Mesma chave usada pelo Auto-Email: a pasta de NF-e é única no programa.
    public const string ChavePastaNfe = "autoemail_pasta_nfe";

    public static string PastaPdfs => ConfiguracaoRepository.Obter(ChavePastaPdfs) ?? "";
    public static string PastaNfe => ConfiguracaoRepository.Obter(ChavePastaNfe) ?? "";

    private readonly TextBox _campoPastaBoletos;
    private readonly TextBox _campoPastaNfe;

    public BoletosConfigForm()
    {
        Text = "Configurações — Boletos";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, 270);

        _campoPastaBoletos = new TextBox { Width = 440, Text = PastaPdfs };
        _campoPastaNfe = new TextBox { Width = 440, Text = PastaNfe };

        var rotuloBoletos = new Label
        {
            Text = "Pasta com os PDFs dos boletos:",
            AutoSize = true,
            Location = new Point(16, 16),
        };
        _campoPastaBoletos.Location = new Point(16, 42);
        var botaoProcurarBoletos = Estilo.BotaoPadrao("Procurar...");
        botaoProcurarBoletos.Location = new Point(464, 40);
        botaoProcurarBoletos.Click += (_, _) => Procurar(_campoPastaBoletos, "boletos");

        var rotuloNfe = new Label
        {
            Text = "Pasta com os PDFs das Notas Fiscais (NF-e):",
            AutoSize = true,
            Location = new Point(16, 84),
        };
        _campoPastaNfe.Location = new Point(16, 110);
        var botaoProcurarNfe = Estilo.BotaoPadrao("Procurar...");
        botaoProcurarNfe.Location = new Point(464, 108);
        botaoProcurarNfe.Click += (_, _) => Procurar(_campoPastaNfe, "Notas Fiscais");

        var rotuloVincular = new Label
        {
            Text = "Vincular NF-es aos boletos já cadastrados (pelo número da NF-e):",
            AutoSize = true,
            Location = new Point(16, 156),
        };
        var botaoVincular = Estilo.BotaoPadrao("Vincular NF-es agora");
        botaoVincular.Location = new Point(16, 180);
        botaoVincular.Click += (_, _) => VincularExistentes();

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(8),
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(rotuloBoletos);
        Controls.Add(_campoPastaBoletos);
        Controls.Add(botaoProcurarBoletos);
        Controls.Add(rotuloNfe);
        Controls.Add(_campoPastaNfe);
        Controls.Add(botaoProcurarNfe);
        Controls.Add(rotuloVincular);
        Controls.Add(botaoVincular);
        Controls.Add(painelBotoes);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private void Procurar(TextBox campo, string oQue)
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = $"Selecione a pasta com os PDFs de {oQue}",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(campo.Text) ? campo.Text : "",
        };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            campo.Text = dialogo.SelectedPath;
    }

    private void VincularExistentes()
    {
        var pastaNfe = _campoPastaNfe.Text.Trim();
        if (pastaNfe.Length == 0 || !Directory.Exists(pastaNfe))
        {
            MessageBox.Show(this,
                "Informe uma pasta de Notas Fiscais válida antes de vincular.",
                "Pasta de NF-e", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // Grava a pasta para que fique valendo daqui em diante.
        ConfiguracaoRepository.Definir(ChavePastaNfe, pastaNfe);

        UseWaitCursor = true;
        int vinculados;
        try
        {
            vinculados = VinculadorNfe.VincularExistentes(pastaNfe);
        }
        finally
        {
            UseWaitCursor = false;
        }

        MessageBox.Show(this,
            $"{vinculados} boleto(s) vinculado(s) à NF-e correspondente.\n" +
            "(Boletos que já tinham NF-e ou sem número correspondente na pasta não são alterados.)",
            "Vínculo concluído", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Salvar()
    {
        var pastaBoletos = _campoPastaBoletos.Text.Trim();
        var pastaNfe = _campoPastaNfe.Text.Trim();

        if (pastaBoletos.Length > 0 && !Directory.Exists(pastaBoletos))
        {
            MessageBox.Show(this, "A pasta de boletos informada não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (pastaNfe.Length > 0 && !Directory.Exists(pastaNfe))
        {
            MessageBox.Show(this, "A pasta de Notas Fiscais informada não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ConfiguracaoRepository.Definir(ChavePastaPdfs, pastaBoletos);
        ConfiguracaoRepository.Definir(ChavePastaNfe, pastaNfe);
        DialogResult = DialogResult.OK;
        Close();
    }
}
