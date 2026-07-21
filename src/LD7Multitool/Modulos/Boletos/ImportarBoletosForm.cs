using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

/// <summary>
/// Lista os PDFs da pasta configurada que ainda não foram importados
/// e cria um boleto para cada arquivo selecionado.
/// </summary>
public class ImportarBoletosForm : Form
{
    private readonly CheckedListBox _listaArquivos;
    private readonly List<string> _caminhos = new();

    /// <summary>Boletos criados a partir dos arquivos selecionados.</summary>
    public List<Boleto> BoletosImportados { get; } = new();

    public ImportarBoletosForm(string pasta)
    {
        Text = "Importar boletos";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 420);
        ClientSize = new Size(560, 420);

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
                : $"{novos.Count} PDF(s) novo(s) encontrado(s). Selecione quais importar:",
        };

        _listaArquivos = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            IntegralHeight = false,
        };
        foreach (var caminho in novos)
        {
            _caminhos.Add(caminho);
            _listaArquivos.Items.Add(Path.GetFileName(caminho), isChecked: true);
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
            Height = 52,
            Padding = new Padding(8),
        };
        painelBotoes.Controls.Add(botaoImportar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(painelLista);
        Controls.Add(rotulo);
        Controls.Add(painelBotoes);
        CancelButton = botaoCancelar;
    }

    private void Importar()
    {
        for (var i = 0; i < _caminhos.Count; i++)
        {
            if (!_listaArquivos.GetItemChecked(i))
                continue;

            BoletosImportados.Add(new Boleto
            {
                Nome = Path.GetFileNameWithoutExtension(_caminhos[i]),
                Valor = 0m,
                Validade = DateTime.Today,
                Estado = EstadoBoleto.Aberto,
                CaminhoArquivo = _caminhos[i],
            });
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
