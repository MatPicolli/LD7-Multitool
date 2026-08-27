namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Visualizador em tela cheia (sem borda) de uma foto de nota fiscal, para dar
/// zoom e ler os dados sem precisar abrir outro programa. Fecha com o botão
/// direito do mouse (em qualquer ponto da imagem) ou com Esc.
///
/// A imagem é lida para a memória (e clonada) em vez de aberta direto do
/// arquivo: <see cref="Image.FromFile"/> mantém o arquivo travado enquanto o
/// <see cref="Image"/> existir, e o usuário normalmente abre a nota bem antes
/// de separá-la — um arquivo travado impediria o <c>File.Move</c> na hora de
/// separar.
/// </summary>
public class VisualizadorImagemForm : Form
{
    private readonly VisualizadorImagemControl _visualizador;
    private Image? _imagem;

    private VisualizadorImagemForm(Image imagem, Control referenciaTela)
    {
        _imagem = imagem;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Bounds = Screen.FromControl(referenciaTela).Bounds;
        ShowInTaskbar = false;
        KeyPreview = true;

        _visualizador = new VisualizadorImagemControl(imagem) { Dock = DockStyle.Fill };
        _visualizador.FechamentoSolicitado += (_, _) => Close();

        var dica = new Label
        {
            Text = "Botão direito ou Esc para fechar   •   Arraste para mover   •   Roda do mouse para zoom",
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(24, 24, 24),
            ForeColor = Color.FromArgb(170, 170, 170),
            Font = new Font("Segoe UI", 9f),
        };

        Controls.Add(_visualizador);
        Controls.Add(dica);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
                Close();
        };
        Shown += (_, _) =>
        {
            _visualizador.AjustarParaCaber();
            _visualizador.Focus();
        };
    }

    /// <summary>Abre o visualizador para o arquivo informado; não faz nada (e avisa) se não conseguir ler a imagem.</summary>
    public static void Exibir(IWin32Window dono, Control referenciaTela, string caminho)
    {
        Image imagem;
        try
        {
            using var bytes = new MemoryStream(File.ReadAllBytes(caminho));
            using var original = Image.FromStream(bytes);
            imagem = new Bitmap(original); // cópia independente — libera o arquivo original na hora
        }
        catch (Exception ex)
        {
            MessageBox.Show(dono as Form, "Não foi possível abrir esta imagem:\n" + ex.Message,
                "Erro ao abrir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var formulario = new VisualizadorImagemForm(imagem, referenciaTela);
        formulario.ShowDialog(dono);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _imagem?.Dispose();
            _imagem = null;
        }
        base.Dispose(disposing);
    }
}
