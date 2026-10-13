using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

/// <summary>Pergunta a data em que a comissão das parcelas selecionadas foi paga.</summary>
public class DataPagamentoForm : Form
{
    private readonly DateTimePicker _campoData;

    public DateTime Data => _campoData.Value.Date;

    public DataPagamentoForm(IReadOnlyCollection<Comissao> parcelas)
    {
        Text = "Marcar comissão como paga";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 170);

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
        };

        var tabela = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(16, 12, 16, 0) };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        tabela.Controls.Add(resumo, 0, 0);
        tabela.SetColumnSpan(resumo, 2);
        tabela.Controls.Add(new Label
        {
            Text = "Data do pagamento:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, 1);
        tabela.Controls.Add(_campoData, 1, 1);

        var botaoOk = Estilo.BotaoPrimario("Marcar como pago");
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
