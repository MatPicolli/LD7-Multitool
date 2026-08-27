using System.Drawing.Drawing2D;

namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Área de desenho com pan e zoom livres, ao estilo de um visualizador de
/// fotos: arraste com o botão esquerdo para mover, gire a roda do mouse para
/// dar zoom (centrado no ponteiro, para ampliar exatamente onde o usuário
/// está olhando — útil pra ler um valor ou um CNPJ pequeno na nota).
/// </summary>
public class VisualizadorImagemControl : Control
{
    private const float ZoomMinimo = 0.05f;
    private const float ZoomMaximo = 10f;
    private const float FatorRoda = 1.15f;

    private readonly Image _imagem;
    private float _zoom = 1f;

    /// <summary>Posição, em pixels de tela, onde cai o pixel (0,0) da imagem.</summary>
    private PointF _origem;

    private Point _ultimoMouse;
    private bool _arrastando;

    /// <summary>Disparado quando o botão direito é clicado — quem decide fechar o visualizador é o dono.</summary>
    public event EventHandler? FechamentoSolicitado;

    public VisualizadorImagemControl(Image imagem)
    {
        _imagem = imagem;
        BackColor = Color.FromArgb(24, 24, 24);
        Cursor = Cursors.SizeAll;
        TabStop = true;

        SetStyle(
            ControlStyles.Selectable | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw,
            true);
    }

    /// <summary>Encaixa a imagem inteira no espaço disponível e centraliza — chamado quando o controle é exibido.</summary>
    public void AjustarParaCaber()
    {
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            return;

        _zoom = Math.Min((float)ClientSize.Width / _imagem.Width, (float)ClientSize.Height / _imagem.Height);
        _zoom = Math.Clamp(_zoom, ZoomMinimo, ZoomMaximo);
        _origem = new PointF(
            (ClientSize.Width - _imagem.Width * _zoom) / 2f,
            (ClientSize.Height - _imagem.Height * _zoom) / 2f);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.DrawImage(_imagem, _origem.X, _origem.Y, _imagem.Width * _zoom, _imagem.Height * _zoom);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        // O Windows só entrega a roda do mouse a quem tem o foco de teclado —
        // sem isso, o zoom só funcionaria depois do primeiro clique.
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button == MouseButtons.Right)
        {
            FechamentoSolicitado?.Invoke(this, EventArgs.Empty);
            return;
        }
        if (e.Button == MouseButtons.Left)
        {
            _arrastando = true;
            _ultimoMouse = e.Location;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_arrastando)
            return;

        _origem = new PointF(_origem.X + (e.X - _ultimoMouse.X), _origem.Y + (e.Y - _ultimoMouse.Y));
        _ultimoMouse = e.Location;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
            _arrastando = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        // Mantém o ponto da imagem sob o cursor no mesmo lugar da tela depois
        // do zoom — assim o zoom "cresce" a partir de onde o usuário mirou,
        // em vez de sempre a partir do canto ou do centro.
        var pontoImagemX = (e.X - _origem.X) / _zoom;
        var pontoImagemY = (e.Y - _origem.Y) / _zoom;

        var fator = e.Delta > 0 ? FatorRoda : 1f / FatorRoda;
        _zoom = Math.Clamp(_zoom * fator, ZoomMinimo, ZoomMaximo);

        _origem = new PointF(e.X - pontoImagemX * _zoom, e.Y - pontoImagemY * _zoom);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}
