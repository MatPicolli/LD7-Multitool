using LD7Multitool.Core;

namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>Configuração do módulo: só a pasta raiz onde ficam as pastas de empresa e a de "para separar".</summary>
public class NotasEntradaConfigForm : Form
{
    public const string ChavePastaRaiz = "notasentrada_pasta_raiz";

    public static string PastaRaiz => ConfiguracaoRepository.Obter(ChavePastaRaiz) ?? "";

    private readonly TextBox _campoPasta;
    private readonly Label _avisoPastaSeparar;

    public NotasEntradaConfigForm()
    {
        Text = "Configurações — Notas de Entrada";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(580, 300);

        var rotulo = new Label
        {
            Text = "Pasta raiz (contém as pastas das empresas e a pasta \"" +
                   ServicoSeparacao.PastaSepararNome + "\"):",
            AutoSize = true,
            Location = new Point(16, 16),
        };

        _campoPasta = new TextBox { Width = 440, Text = PastaRaiz, Location = new Point(16, 42) };
        _campoPasta.TextChanged += (_, _) => AtualizarAviso();

        var botaoProcurar = Estilo.BotaoPadrao("Procurar...");
        botaoProcurar.Location = new Point(464, 40);
        botaoProcurar.Click += (_, _) => Procurar();

        _avisoPastaSeparar = new Label
        {
            AutoSize = false,
            Location = new Point(16, 84),
            Size = new Size(540, 40),
            ForeColor = Estilo.CorTextoSuave,
        };

        var explicacao = new Label
        {
            AutoSize = false,
            Location = new Point(16, 136),
            Size = new Size(540, 120),
            ForeColor = Estilo.CorTextoSuave,
            Text =
                "Dentro da pasta raiz, o programa espera:\n\n" +
                "•  \"" + ServicoSeparacao.PastaSepararNome + "\"  — as fotos ainda não separadas (.jpg);\n" +
                "•  uma pasta por empresa (razão social) — dentro dela, uma pasta por ano e, dentro do ano, " +
                "\"dd-MM-aaaa.jpg\" (nota de uma folha) ou uma pasta \"dd-MM-aaaa\" com \"01.jpg\", \"02.jpg\"... " +
                "(nota de várias folhas).",
        };

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
        Controls.Add(_avisoPastaSeparar);
        Controls.Add(explicacao);
        Controls.Add(painelBotoes);

        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;

        AtualizarAviso();
    }

    private void AtualizarAviso()
    {
        var raiz = _campoPasta.Text.Trim();
        if (raiz.Length == 0)
        {
            _avisoPastaSeparar.Text = "";
            return;
        }
        if (!Directory.Exists(raiz))
        {
            _avisoPastaSeparar.Text = "⚠ Esta pasta não existe.";
            _avisoPastaSeparar.ForeColor = Estilo.CorPerigo;
            return;
        }
        if (!Directory.Exists(ServicoSeparacao.PastaParaSeparar(raiz)))
        {
            _avisoPastaSeparar.Text = "⚠ Não encontrei a pasta \"" + ServicoSeparacao.PastaSepararNome +
                                       "\" dentro desta pasta raiz.";
            _avisoPastaSeparar.ForeColor = Estilo.CorPerigo;
            return;
        }
        _avisoPastaSeparar.ForeColor = Estilo.CorTextoSuave;
        var pendentes = ServicoSeparacao.ListarPendentes(raiz).Count;
        var empresas = ServicoSeparacao.ListarEmpresas(raiz).Count;
        _avisoPastaSeparar.Text = $"✓ {pendentes} foto(s) pendente(s) — {empresas} empresa(s) cadastrada(s).";
    }

    private void Procurar()
    {
        using var dialogo = new FolderBrowserDialog
        {
            Description = "Selecione a pasta raiz das notas fiscais de entrada",
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

        ConfiguracaoRepository.Definir(ChavePastaRaiz, pasta);
        DialogResult = DialogResult.OK;
        Close();
    }
}
