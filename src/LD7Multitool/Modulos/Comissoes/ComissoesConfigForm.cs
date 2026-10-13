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
        ClientSize = new Size(600, 500);

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

        var botaoImportar = Estilo.BotaoPadrao("Selecionar planilha (.xlsx)...");
        botaoImportar.Margin = new Padding(0, 4, 0, 0);
        botaoImportar.Click += (_, _) => SelecionarEImportar();

        var conteudo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16),
            BackColor = Estilo.CorSuperficie,
        };
        conteudo.Controls.Add(CriarTitulo("Comissão padrão"));
        conteudo.Controls.Add(linhaPercentual);
        conteudo.Controls.Add(CriarAjuda(
            "Sugerida ao lançar uma nova venda. Cada parcela guarda o seu próprio " +
            "percentual, então mudar aqui não altera o que já foi lançado."));
        conteudo.Controls.Add(CriarTitulo("Importar da planilha do Excel", topo: 18));
        conteudo.Controls.Add(CriarAjuda(
            "Traz as linhas de uma aba da planilha de comissões (ex.: \"2026 (NOVA MILITAR)\"). " +
            "As colunas são reconhecidas pelo cabeçalho — Nota fiscal, Nº b, Cliente, REF. Pedido, " +
            "Valor NF, Vencimento, Nº de parcelas, Valor da parcela, Comissão, Status da comissão " +
            "e pag. comissão — então as abas de outros anos também funcionam.\r\n\r\n" +
            "• Uma prévia é mostrada antes de confirmar.\r\n" +
            "• Parcelas que já existem (mesma NF, boleto, parcela, vencimento e valor) são " +
            "puladas — dá para importar várias abas sem duplicar.\r\n" +
            "• Só o formato .xlsx (no Excel: Salvar como → Pasta de Trabalho do Excel)."));
        conteudo.Controls.Add(botaoImportar);

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
        MaximumSize = new Size(560, 0),
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
