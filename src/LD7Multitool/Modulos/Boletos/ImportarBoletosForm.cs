using System.Globalization;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Lista os PDFs da pasta configurada que ainda não foram importados,
/// já mostrando os dados extraídos de cada um (pagador, valor, vencimento),
/// e cria um boleto para cada arquivo selecionado.
/// </summary>
public class ImportarBoletosForm : Form
{
    private static readonly CultureInfo CulturaBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly CheckedListBox _listaArquivos;
    private readonly List<Boleto> _candidatos = new();

    /// <summary>Boletos criados a partir dos arquivos selecionados.</summary>
    public List<Boleto> BoletosImportados { get; } = new();

    public ImportarBoletosForm(string pasta)
    {
        Text = "Importar boletos";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(720, 440);
        ClientSize = new Size(720, 440);

        var jaImportados = BoletoRepository.ListarCaminhosImportados();
        var novos = Directory.EnumerateFiles(pasta, "*.pdf", SearchOption.TopDirectoryOnly)
            .Where(caminho => !jaImportados.Contains(caminho))
            .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var rotulo = new Label
        {
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(12, 10, 12, 0),
            Text = novos.Count == 0
                ? "Nenhum PDF novo encontrado na pasta configurada."
                : $"{novos.Count} PDF(s) novo(s) encontrado(s). Os dados abaixo foram lidos de cada arquivo:",
        };

        _listaArquivos = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
            HorizontalScrollbar = true,
        };

        // A leitura dos PDFs acontece aqui, na abertura do diálogo.
        Cursor.Current = Cursors.WaitCursor;
        try
        {
            foreach (var caminho in novos)
            {
                var boleto = LeitorBoletoPdf.Ler(caminho);
                _candidatos.Add(boleto);
                _listaArquivos.Items.Add(DescreverCandidato(boleto), isChecked: true);
            }
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        var painelLista = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
        painelLista.Controls.Add(_listaArquivos);

        var botaoImportar = Estilo.BotaoPrimario("Importar selecionados");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoImportar.Enabled = novos.Count > 0;
        botaoImportar.Click += (_, _) => Importar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(8),
        };
        painelBotoes.Controls.Add(botaoImportar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(painelLista);
        Controls.Add(rotulo);
        Controls.Add(painelBotoes);
        CancelButton = botaoCancelar;
    }

    private static string DescreverCandidato(Boleto boleto)
    {
        var arquivo = Path.GetFileName(boleto.CaminhoArquivo);
        var valor = boleto.Valor > 0
            ? boleto.Valor.ToString("C2", CulturaBr)
            : "valor não lido";
        return $"{boleto.Nome}  —  {valor}, venc. {boleto.Validade:dd/MM/yyyy}  ({arquivo})";
    }

    private void Importar()
    {
        for (var i = 0; i < _candidatos.Count; i++)
        {
            if (_listaArquivos.GetItemChecked(i))
                BoletosImportados.Add(_candidatos[i]);
        }

        if (BoletosImportados.Count == 0)
        {
            MessageBox.Show(this, "Selecione pelo menos um arquivo.", "Nada selecionado",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
