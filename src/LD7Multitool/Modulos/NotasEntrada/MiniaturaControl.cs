using LD7Multitool.Core;

namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Um quadrado clicável na galeria: miniatura + nome do arquivo. Quando
/// selecionado, mostra um selo numerado — a ordem de clique vira a ordem das
/// páginas na hora de separar (útil para notas de mais de uma folha).
/// </summary>
public class MiniaturaControl : UserControl
{
    private const int LadoImagem = 118;

    private readonly PictureBox _imagem;
    private readonly Label _nome;
    private readonly Label _selo;

    public string Caminho { get; }

    /// <summary>Disparado a cada clique — quem decide se seleciona/deseleciona é o dono da galeria.</summary>
    public event EventHandler? Alternado;

    public MiniaturaControl(string caminho)
    {
        Caminho = caminho;

        Size = new Size(140, 166);
        Margin = new Padding(6);
        BackColor = Estilo.CorSuperficie;
        Cursor = Cursors.Hand;
        Padding = new Padding(1);

        _imagem = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(LadoImagem, LadoImagem),
            Location = new Point(10, 8),
            BackColor = Estilo.CorFundo,
            Cursor = Cursors.Hand,
        };

        _nome = new Label
        {
            Text = Path.GetFileName(caminho),
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(4, LadoImagem + 12),
            Size = new Size(132, 32),
            ForeColor = Estilo.CorTextoSuave,
            Font = new Font(Estilo.FontePadrao.FontFamily, 8f),
            Cursor = Cursors.Hand,
        };

        _selo = new Label
        {
            Visible = false,
            BackColor = Estilo.CorPrimaria,
            ForeColor = Color.White,
            Size = new Size(24, 24),
            Location = new Point(Width - 32, 4),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 9f),
        };

        Controls.Add(_imagem);
        Controls.Add(_nome);
        Controls.Add(_selo);

        Click += (_, e) => Alternado?.Invoke(this, e);
        _imagem.Click += (_, e) => Alternado?.Invoke(this, e);
        _nome.Click += (_, e) => Alternado?.Invoke(this, e);
    }

    /// <summary>Define a miniatura já pronta (gerada em segundo plano); dono anterior é liberado.</summary>
    public void DefinirImagem(Image? imagem)
    {
        _imagem.Image?.Dispose();
        _imagem.Image = imagem;
        if (imagem is null)
        {
            _nome.Text = "⚠ " + Path.GetFileName(Caminho) + " (não abriu)";
            _nome.ForeColor = Estilo.CorPerigo;
        }
    }

    public void AtualizarSelecao(bool selecionada, int? ordem)
    {
        BackColor = selecionada ? Estilo.CorSelecao : Estilo.CorSuperficie;
        _selo.Visible = selecionada;
        if (ordem is { } numero)
            _selo.Text = numero.ToString();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _imagem.Image?.Dispose();
        base.Dispose(disposing);
    }
}
