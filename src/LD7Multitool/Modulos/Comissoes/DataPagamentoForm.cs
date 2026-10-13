using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>
/// Confirma o pagamento de parcelas. Dois usos:
/// <list type="bullet">
/// <item>tela principal — status "Pago" (cliente pagou); a data de pagamento da
///   comissão é <b>opcional</b>: sem ela, a parcela fica esperando o fechamento
///   do mês, como as linhas "Pago" sem data da planilha;</item>
/// <item>fechamento do mês — registra o pagamento ao vendedor; data obrigatória.</item>
/// </list>
/// </summary>
public class DataPagamentoForm : Form
{
    private readonly DateTimePicker _campoData;

    /// <summary>Data escolhida, ou null se ficou em branco (só no modo opcional).</summary>
    public DateTime? Data => _campoData.ShowCheckBox && !_campoData.Checked ? null : _campoData.Value.Date;

    public DataPagamentoForm(IReadOnlyCollection<Comissao> parcelas, bool dataObrigatoria)
    {
        Text = dataObrigatoria ? "Registrar pagamento da comissão" : "Marcar como pago";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, dataObrigatoria ? 170 : 210);

        var total = parcelas.Sum(p => p.ValorComissao).ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
        var resumo = new Label
        {
            Text = $"{parcelas.Count} parcela(s) — comissão {total}",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Estilo.FonteCabecalhoGrade,
        };
        _campoData = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            Margin = new Padding(0, 4, 0, 4),
            ShowCheckBox = !dataObrigatoria,
        };
        if (!dataObrigatoria)
            _campoData.Checked = false;

        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 12, 16, 0) };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.Controls.Add(resumo, 0, 0);
        tabela.SetColumnSpan(resumo, 2);
        tabela.Controls.Add(new Label
        {
            Text = dataObrigatoria ? "Data do pagamento:" : "Comissão paga em:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, 1);
        tabela.Controls.Add(_campoData, 1, 1);
        if (!dataObrigatoria)
        {
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            var ajuda = new Label
            {
                Text = "Deixe a data desmarcada se o vendedor ainda vai receber: " +
                       "a parcela entra no próximo fechamento do mês.",
                Dock = DockStyle.Fill,
                ForeColor = Estilo.CorTextoSuave,
            };
            tabela.Controls.Add(ajuda, 0, 2);
            tabela.SetColumnSpan(ajuda, 2);
        }

        var botaoOk = Estilo.BotaoPrimario(dataObrigatoria ? "Registrar pagamento" : "Marcar como pago");
        botaoOk.DialogResult = DialogResult.OK;
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 10, 12, 10),
        };
        painelBotoes.Controls.Add(botaoOk);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(tabela);
        Controls.Add(painelBotoes);
        AcceptButton = botaoOk;
        CancelButton = botaoCancelar;
    }
}
