using System.Reflection;
using LD7Multitool.Core;

namespace LD7Multitool;

/// <summary>
/// Janela principal: menu lateral com os módulos descobertos por reflexão
/// e um painel de conteúdo onde o módulo selecionado é exibido.
/// </summary>
public class MainForm : Form
{
    private readonly FlowLayoutPanel _menuLateral;
    private readonly Panel _painelConteudo;
    private readonly Dictionary<IModulo, Control> _controlesCriados = new();
    private Button? _botaoAtivo;

    public MainForm()
    {
        Text = "LD7 Multitool";
        Font = Estilo.FontePadrao;
        MinimumSize = new Size(1000, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Estilo.CorFundo;

        _menuLateral = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 210,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Estilo.CorMenu,
            Padding = new Padding(10),
        };

        _painelConteudo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Estilo.CorFundo,
        };

        Controls.Add(_painelConteudo);
        Controls.Add(_menuLateral);

        var titulo = new Label
        {
            Text = "LD7 Multitool",
            ForeColor = Color.White,
            Font = Estilo.FonteTitulo,
            AutoSize = false,
            Width = 190,
            Height = 52,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            Margin = new Padding(0, 4, 0, 16),
        };
        _menuLateral.Controls.Add(titulo);

        // Módulos agrupados por categoria, cada grupo com um cabeçalho. Os
        // grupos seguem a menor Ordem dos seus módulos (assim "Lojão da 7",
        // que tem o módulo 1, vem antes de "MILITARI"); dentro do grupo vale a
        // Ordem de cada módulo (o GroupBy preserva a ordenação de DescobrirModulos).
        IModulo? primeiroModulo = null;
        Button? primeiroBotao = null;
        var grupos = DescobrirModulos()
            .GroupBy(m => m.Categoria)
            .OrderBy(g => g.Min(m => m.Ordem))
            .ThenBy(g => g.Key);

        var primeiroGrupo = true;
        foreach (var grupo in grupos)
        {
            _menuLateral.Controls.Add(CriarCabecalhoCategoria(grupo.Key, primeiroGrupo));
            primeiroGrupo = false;

            foreach (var modulo in grupo)
            {
                var botao = CriarBotaoModulo(modulo);
                _menuLateral.Controls.Add(botao);
                if (primeiroBotao is null)
                {
                    primeiroModulo = modulo;
                    primeiroBotao = botao;
                }
            }
        }

        if (primeiroModulo is not null && primeiroBotao is not null)
            AbrirModulo(primeiroModulo, primeiroBotao);
    }

    /// <summary>Rótulo discreto que separa os grupos do menu (ex.: "LOJÃO DA 7").</summary>
    private static Label CriarCabecalhoCategoria(string categoria, bool primeiro) => new()
    {
        Text = categoria.ToUpperInvariant(),
        ForeColor = Estilo.CorMenuCategoria,
        Font = Estilo.FonteCategoriaMenu,
        AutoSize = false,
        Width = 186,
        Height = 24,
        TextAlign = ContentAlignment.BottomLeft,
        Padding = new Padding(8, 0, 0, 2),
        // Espaço extra acima de cada grupo, menos do primeiro (já fica sob o título).
        Margin = new Padding(0, primeiro ? 0 : 14, 0, 4),
    };

    private static List<IModulo> DescobrirModulos()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IModulo).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => (IModulo)Activator.CreateInstance(t)!)
            .OrderBy(m => m.Ordem)
            .ThenBy(m => m.Nome)
            .ToList();
    }

    private Button CriarBotaoModulo(IModulo modulo)
    {
        var botao = new Button
        {
            Text = "  " + modulo.Nome,
            Width = 186,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(210, 213, 226),
            BackColor = Estilo.CorMenu,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Estilo.FontePadrao,
            Margin = new Padding(0, 0, 0, 6),
            Cursor = Cursors.Hand,
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.FlatAppearance.MouseOverBackColor = Estilo.CorMenuHover;
        botao.Click += (_, _) => AbrirModulo(modulo, botao);
        return botao;
    }

    private void AbrirModulo(IModulo modulo, Button botao)
    {
        if (_botaoAtivo is not null)
        {
            _botaoAtivo.BackColor = Estilo.CorMenu;
            _botaoAtivo.ForeColor = Color.FromArgb(210, 213, 226);
        }
        botao.BackColor = Estilo.CorMenuAtivo;
        botao.ForeColor = Color.White;
        _botaoAtivo = botao;

        if (!_controlesCriados.TryGetValue(modulo, out var controle))
        {
            controle = modulo.CriarControle();
            controle.Dock = DockStyle.Fill;
            _controlesCriados[modulo] = controle;
        }

        _painelConteudo.Controls.Clear();
        _painelConteudo.Controls.Add(controle);
    }
}
