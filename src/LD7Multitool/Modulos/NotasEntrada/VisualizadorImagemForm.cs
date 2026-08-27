namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Janela flutuante (não tampa a tela toda) para dar zoom numa foto de nota
/// fiscal e ler os dados sem precisar abrir outro programa. O tamanho é uma
/// proporção da resolução do monitor — não do tamanho atual da janela
/// principal — e a janela nasce centralizada e pode ser redimensionada.
/// Fecha com o botão direito do mouse (em qualquer ponto da imagem), com Esc
/// ou pelo X da barra de título.
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

    // Proporção da tela (não da janela do programa) ocupada pela janela
    // flutuante — assim ela nunca tampa tudo, e escala junto com a resolução
    // do monitor em vez de depender do tamanho da janela principal.
    private const float ProporcaoTela = 0.8f;

    private VisualizadorImagemForm(Image imagem, Control referenciaTela, string caminho)
    {
        _imagem = imagem;

        Text = "Zoom — " + Path.GetFileName(caminho);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        ShowInTaskbar = false;
        KeyPreview = true;

        var areaTrabalho = Screen.FromControl(referenciaTela).WorkingArea;
        var tamanho = new Size(
            (int)(areaTrabalho.Width * ProporcaoTela),
            (int)(areaTrabalho.Height * ProporcaoTela));
        StartPosition = FormStartPosition.Manual;
        Size = tamanho;
        Location = new Point(
            areaTrabalho.Left + (areaTrabalho.Width - tamanho.Width) / 2,
            areaTrabalho.Top + (areaTrabalho.Height - tamanho.Height) / 2);
        MinimumSize = new Size(360, 260);

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

        using var formulario = new VisualizadorImagemForm(imagem, referenciaTela, caminho);
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
