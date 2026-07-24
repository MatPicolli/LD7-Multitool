namespace LD7Multitool.Core;

/// <summary>
/// Paleta de cores e fábricas de controles para manter a interface
/// consistente e moderna em todos os módulos.
/// </summary>
public static class Estilo
{
    // Paleta
    public static readonly Color CorFundo = Color.FromArgb(245, 246, 250);
    public static readonly Color CorSuperficie = Color.White;
    public static readonly Color CorPrimaria = Color.FromArgb(59, 91, 219);
    public static readonly Color CorPrimariaHover = Color.FromArgb(47, 74, 183);
    public static readonly Color CorPerigo = Color.FromArgb(196, 61, 61);
    public static readonly Color CorTexto = Color.FromArgb(35, 38, 46);
    public static readonly Color CorTextoSuave = Color.FromArgb(110, 115, 130);
    public static readonly Color CorBorda = Color.FromArgb(215, 218, 228);
    public static readonly Color CorMenu = Color.FromArgb(30, 32, 44);
    public static readonly Color CorMenuHover = Color.FromArgb(48, 51, 70);
    public static readonly Color CorMenuAtivo = CorPrimaria;
    public static readonly Color CorSelecao = Color.FromArgb(226, 233, 255);

    public static readonly Font FontePadrao = new("Segoe UI", 9.75f);
    public static readonly Font FonteTitulo = new("Segoe UI Semibold", 12f);
    public static readonly Font FonteCabecalhoGrade = new("Segoe UI Semibold", 9.75f);

    /// <summary>Altura única de todos os botões de barra — evita corte de texto.</summary>
    public const int AlturaBotao = 34;

    /// <summary>Botão de ação principal (fundo colorido, texto branco).</summary>
    public static Button BotaoPrimario(string texto) =>
        ConfigurarBotao(texto, CorPrimaria, Color.White, CorPrimariaHover);

    /// <summary>Botão comum (fundo claro com borda).</summary>
    public static Button BotaoPadrao(string texto)
    {
        var botao = ConfigurarBotao(texto, CorSuperficie, CorTexto, Color.FromArgb(238, 240, 246));
        botao.FlatAppearance.BorderSize = 1;
        botao.FlatAppearance.BorderColor = CorBorda;
        return botao;
    }

    /// <summary>Botão de ação destrutiva (texto vermelho).</summary>
    public static Button BotaoPerigo(string texto)
    {
        var botao = BotaoPadrao(texto);
        botao.ForeColor = CorPerigo;
        return botao;
    }

    /// <summary>Botão quadrado só com ícone (ex.: engrenagem "⚙").</summary>
    public static Button BotaoIcone(string glifo, string dica)
    {
        var botao = new Button
        {
            Text = glifo,
            Font = new Font("Segoe UI", 12f),
            FlatStyle = FlatStyle.Flat,
            BackColor = CorSuperficie,
            ForeColor = CorTextoSuave,
            Size = new Size(40, AlturaBotao),
            Margin = new Padding(0),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(0),
        };
        botao.FlatAppearance.BorderSize = 1;
        botao.FlatAppearance.BorderColor = CorBorda;
        new ToolTip().SetToolTip(botao, dica);
        return botao;
    }

    private static Button ConfigurarBotao(string texto, Color fundo, Color textoCor, Color hover)
    {
        var botao = new Button
        {
            Text = texto,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            // Padding só horizontal: a altura é fixada por MinimumSize para o
            // texto ficar centralizado sem risco de corte.
            Padding = new Padding(14, 0, 14, 0),
            MinimumSize = new Size(0, AlturaBotao),
            // Margem vertical de 2px: centraliza o botão na barra e garante
            // que a borda não seja cortada pelo contêiner.
            Margin = new Padding(0, 2, 8, 2),
            FlatStyle = FlatStyle.Flat,
            BackColor = fundo,
            ForeColor = textoCor,
            Font = FontePadrao,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.FlatAppearance.MouseOverBackColor = hover;
        return botao;
    }

    /// <summary>Barra superior padrão dos módulos (fundo branco com borda inferior).</summary>
    public static Panel CriarBarraSuperior()
    {
        var barra = new Panel
        {
            Dock = DockStyle.Top,
            Height = AlturaBotao + 24,
            BackColor = CorSuperficie,
            Padding = new Padding(12, 10, 12, 10),
        };
        barra.Paint += (_, e) =>
            e.Graphics.DrawLine(new Pen(CorBorda), 0, barra.Height - 1, barra.Width, barra.Height - 1);
        return barra;
    }

    /// <summary>Aplica o visual moderno padrão a uma grade.</summary>
    public static void EstilizarGrade(DataGridView grade)
    {
        grade.BorderStyle = BorderStyle.None;
        grade.BackgroundColor = CorFundo;
        grade.EnableHeadersVisualStyles = false;
        grade.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grade.ColumnHeadersHeight = 40;
        grade.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grade.ColumnHeadersDefaultCellStyle.BackColor = CorSuperficie;
        grade.ColumnHeadersDefaultCellStyle.ForeColor = CorTextoSuave;
        grade.ColumnHeadersDefaultCellStyle.Font = FonteCabecalhoGrade;
        grade.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        // Ao clicar/ordenar um cabeçalho, mantém fundo claro e letra escura
        // (antes ficava azul forte com letra cinza — ilegível).
        grade.ColumnHeadersDefaultCellStyle.SelectionBackColor = CorSelecao;
        grade.ColumnHeadersDefaultCellStyle.SelectionForeColor = CorTexto;
        grade.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grade.GridColor = Color.FromArgb(232, 234, 240);
        grade.RowTemplate.Height = 34;
        grade.DefaultCellStyle.BackColor = CorSuperficie;
        grade.DefaultCellStyle.ForeColor = CorTexto;
        grade.DefaultCellStyle.Font = FontePadrao;
        grade.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        // Seleção com contraste forte: fundo azul sólido e letra branca
        // (vale também para linhas coloridas por estado — pago/cancelado/vencido —
        // pois elas só definem ForeColor, herdando estas cores de seleção).
        grade.DefaultCellStyle.SelectionBackColor = CorPrimaria;
        grade.DefaultCellStyle.SelectionForeColor = Color.White;
        grade.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 253);

        // Divisor vertical sutil entre os cabeçalhos (a grade não tem borda
        // vertical, então desenhamos uma linha fina entre uma coluna e outra).
        grade.CellPainting += DesenharDivisorCabecalho;
    }

    private static void DesenharDivisorCabecalho(object? remetente, DataGridViewCellPaintingEventArgs e)
    {
        // Só a linha de cabeçalho (RowIndex == -1), ignorando o canto (ColumnIndex < 0).
        if (e.RowIndex != -1 || e.ColumnIndex < 0)
            return;

        var grade = (DataGridView)remetente!;
        e.PaintBackground(e.CellBounds, true);
        e.PaintContent(e.CellBounds);

        // Não desenha à direita da última coluna visível (fica na borda da grade).
        var ultima = grade.Columns.GetLastColumn(
            DataGridViewElementStates.Visible, DataGridViewElementStates.None);
        if (ultima is null || e.ColumnIndex != ultima.Index)
        {
            using var caneta = new Pen(CorBorda);
            var x = e.CellBounds.Right - 1;
            e.Graphics.DrawLine(caneta, x, e.CellBounds.Top + 8, x, e.CellBounds.Bottom - 8);
        }

        e.Handled = true;
    }
}
