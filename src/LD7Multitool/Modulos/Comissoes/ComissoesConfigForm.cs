using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>Configurações das comissões: percentual padrão e importação da planilha do Excel.</summary>
public class ComissoesConfigForm : Form
{
    public const string ChavePercentual = "comissoes_percentual_padrao";

    private readonly NumericUpDown _campoPercentual;

    /// <summary>True se alguma parcela foi importada (para o chamador recarregar a grade).</summary>
    public bool HouveImportacao { get; private set; }

    /// <summary>Percentual sugerido numa nova venda (10% se nunca configurado).</summary>
    public static decimal PercentualPadrao =>
        decimal.TryParse(ConfiguracaoRepository.Obter(ChavePercentual), NumberStyles.Number,
            CultureInfo.InvariantCulture, out var percentual)
            ? percentual
            : 10m;

    public ComissoesConfigForm()
    {
        Text = "Configurações — Comissões";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(600, 440);

        _campoPercentual = ComissaoForm.CriarCampoPercentual(PercentualPadrao);
        _campoPercentual.Dock = DockStyle.None;
        _campoPercentual.Width = 90;
        _campoPercentual.Margin = new Padding(0, 0, 10, 0);

        var linhaPercentual = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        linhaPercentual.Controls.Add(_campoPercentual);
        linhaPercentual.Controls.Add(new Label
        {
            Text = "% sobre o valor de cada parcela",
            AutoSize = true,
            ForeColor = Estilo.CorTextoSuave,
            Margin = new Padding(0, 4, 0, 0),
        });

        // O botão vem logo abaixo do título (antes do texto de ajuda): com a
        // escala do Windows acima de 100% o texto cresce, e um botão no fim da
        // página acabava escondido atrás da barra Fechar/Salvar.
        var botaoImportar = Estilo.BotaoPrimario("Importar planilha (.xlsx)...");
        botaoImportar.Margin = new Padding(0, 0, 0, 8);
        botaoImportar.Click += (_, _) => SelecionarEImportar();

        var conteudo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            // Rolagem como garantia: nada fica inalcançável em telas menores.
            AutoScroll = true,
            Padding = new Padding(16),
            BackColor = Estilo.CorSuperficie,
        };
        conteudo.Controls.Add(CriarTitulo("Importar da planilha do Excel"));
        conteudo.Controls.Add(botaoImportar);
        conteudo.Controls.Add(CriarAjuda(
            "Escolha o arquivo e depois a aba (ex.: \"2026 (NOVA MILITAR)\"); uma prévia é " +
            "mostrada antes de confirmar. Serve para as abas de qualquer ano, e parcelas " +
            "que já existem são puladas — dá para importar várias abas sem duplicar. " +
            "Só o formato .xlsx."));
        conteudo.Controls.Add(CriarTitulo("Comissão padrão", topo: 18));
        conteudo.Controls.Add(linhaPercentual);
        conteudo.Controls.Add(CriarAjuda(
            "Sugerida ao lançar uma nova venda. Cada parcela guarda o seu próprio " +
            "percentual, então mudar aqui não altera o que já foi lançado."));

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        botaoSalvar.Click += (_, _) => Salvar();
        var botaoFechar = Estilo.BotaoPadrao("Fechar");
        botaoFechar.DialogResult = DialogResult.Cancel;
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoFechar);

        Controls.Add(conteudo);
        Controls.Add(painelBotoes);
        AcceptButton = botaoSalvar;
        CancelButton = botaoFechar;
    }

    private static Label CriarTitulo(string texto, int topo = 0) => new()
    {
        Text = texto,
        Font = new Font("Segoe UI Semibold", 10.5f),
        ForeColor = Estilo.CorPrimaria,
        AutoSize = true,
        Margin = new Padding(0, topo, 0, 8),
    };

    private static Label CriarAjuda(string texto) => new()
    {
        Text = texto,
        ForeColor = Estilo.CorTextoSuave,
        AutoSize = true,
        MaximumSize = new Size(530, 0),
        Margin = new Padding(0, 0, 0, 4),
    };

    private void Salvar()
    {
        ConfiguracaoRepository.Definir(ChavePercentual,
            _campoPercentual.Value.ToString(CultureInfo.InvariantCulture));
        DialogResult = DialogResult.OK;
        Close();
    }

    private void SelecionarEImportar()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Selecionar a planilha de comissões",
            Filter = "Planilhas do Excel (*.xlsx)|*.xlsx|Todos os arquivos (*.*)|*.*",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        LeitorXlsx leitor;
        try
        {
            leitor = new LeitorXlsx(dialogo.FileName);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException
                                       or System.Xml.XmlException)
        {
            MessageBox.Show(this,
                "Não foi possível ler o arquivo como planilha .xlsx.\n\n" +
                "Se for o formato antigo (.xls), abra no Excel e use Salvar como → " +
                "Pasta de Trabalho do Excel (.xlsx).\n\nDetalhe: " + ex.Message,
                "Erro ao abrir a planilha", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // O formulário de importação passa a ser dono do leitor (fecha o arquivo).
        using var formulario = new ImportarComissoesForm(leitor, Path.GetFileName(dialogo.FileName), _campoPercentual.Value);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        HouveImportacao = true;
        MessageBox.Show(this,
            $"{formulario.ImportadasCount} parcela(s) importada(s).\n" +
            $"{formulario.PuladasCount} linha(s) pulada(s) (já existentes, repetidas ou com problema).",
            "Importação concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
