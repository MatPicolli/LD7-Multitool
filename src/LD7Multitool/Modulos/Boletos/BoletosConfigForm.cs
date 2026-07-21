using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>Configurações do módulo Boletos (pasta onde ficam os PDFs).</summary>
public class BoletosConfigForm : Form
{
    public const string ChavePastaPdfs = "boletos_pasta_pdfs";

    public static string PastaPdfs => ConfiguracaoRepository.Obter(ChavePastaPdfs) ?? "";

    private readonly TextBox _campoPasta;

    public BoletosConfigForm()
    {
        Text = "Configurações — Boletos";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 150);

        var rotulo = new Label
        {
            Text = "Pasta com os PDFs dos boletos:",
            AutoSize = true,
            Location = new Point(16, 16),
        };

        _campoPasta = new TextBox
        {
            Location = new Point(16, 44),
            Width = 430,
            Text = PastaPdfs,
        };

        var botaoProcurar = Estilo.BotaoPadrao("Procurar...");
        botaoProcurar.Location = new Point(454, 42);
        botaoProcurar.Click += (_, _) => Procurar();

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

        Controls.Add(rotulo);
        Controls.Add(_campoPasta);
        Controls.Add(botaoProcurar);
        Controls.Add(painelBotoes);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private void Procurar()
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Selecione a pasta com os PDFs dos boletos",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(_campoPasta.Text) ? _campoPasta.Text : "",
        };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
            _campoPasta.Text = dialogo.SelectedPath;
    }

    private void Salvar()
    {
        var pasta = _campoPasta.Text.Trim();
        if (pasta.Length > 0 && !Directory.Exists(pasta))
        {
            MessageBox.Show(this, "A pasta informada não existe.", "Pasta inválida",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        ConfiguracaoRepository.Definir(ChavePastaPdfs, pasta);
        DialogResult = DialogResult.OK;
        Close();
    }
}
