using System.Diagnostics;
using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Opções da impressão da tabela (para preencher à mão): título e quantas
/// linhas em branco acrescentar depois das vendas. Gera o PDF e o abre para
/// imprimir.
/// </summary>
public class ImprimirComissoesForm : Form
{
    private const string ChaveLinhasEmBranco = "comissoes_linhas_em_branco";

    private readonly IReadOnlyList<Comissao> _parcelas;
    private readonly TextBox _campoTitulo;
    private readonly NumericUpDown _campoLinhas;

    public ImprimirComissoesForm(IReadOnlyList<Comissao> parcelas, string tituloSugerido)
    {
        _parcelas = parcelas;

        Text = "Imprimir tabela de comissões";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 250);

        var resumo = new Label
        {
            Text = $"Serão impressas as {parcelas.Count} parcela(s) exibidas na tela, na ordem do Nº. " +
                   "Para escolher outras, use os filtros (ex.: Ano 2026) antes de imprimir.",
            Dock = DockStyle.Fill,
            ForeColor = Estilo.CorTextoSuave,
        };
        _campoTitulo = new TextBox { Dock = DockStyle.Fill, Text = tituloSugerido, Margin = new Padding(0, 4, 0, 4) };
        var linhasSalvas = int.TryParse(ConfiguracaoRepository.Obter(ChaveLinhasEmBranco), out var n) ? n : 50;
        _campoLinhas = new NumericUpDown
        {
            Width = 90,
            Minimum = 0,
            Maximum = 1000,
            Value = Math.Clamp(linhasSalvas, 0, 1000),
            Margin = new Padding(0, 4, 10, 4),
        };
        var linhaBrancas = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };
        linhaBrancas.Controls.Add(_campoLinhas);
        linhaBrancas.Controls.Add(new Label
        {
            Text = "(a última página é completada até o fim)",
            AutoSize = true,
            ForeColor = Estilo.CorTextoSuave,
            Margin = new Padding(0, 8, 0, 0),
        });

        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 12, 16, 0) };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.Controls.Add(resumo, 0, 0);
        tabela.SetColumnSpan(resumo, 2);
        tabela.Controls.Add(Rotulo("Título:"), 0, 1);
        tabela.Controls.Add(_campoTitulo, 1, 1);
        tabela.Controls.Add(Rotulo("Linhas em branco:"), 0, 2);
        tabela.Controls.Add(linhaBrancas, 1, 2);

        var botaoGerar = Estilo.BotaoPrimario("Gerar PDF para imprimir");
        botaoGerar.Click += (_, _) => Gerar();
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 10, 12, 10),
        };
        painelBotoes.Controls.Add(botaoGerar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(tabela);
        Controls.Add(painelBotoes);
        AcceptButton = botaoGerar;
        CancelButton = botaoCancelar;
    }

    private static Label Rotulo(string texto) => new()
    {
        Text = texto,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Estilo.CorTextoSuave,
    };

    private void Gerar()
    {
        var linhas = (int)_campoLinhas.Value;
        ConfiguracaoRepository.Definir(ChaveLinhasEmBranco, linhas.ToString(CultureInfo.InvariantCulture));

        string caminho;
        try
        {
            caminho = TabelaComissoesPdf.Gerar(_parcelas, linhas, _campoTitulo.Text.Trim());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Não foi possível gravar o PDF:\n\n" + ex.Message, "Erro ao gerar PDF",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Abre no leitor de PDF padrão; de lá é só imprimir (Ctrl+P).
        Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
        DialogResult = DialogResult.OK;
        Close();
    }
}
