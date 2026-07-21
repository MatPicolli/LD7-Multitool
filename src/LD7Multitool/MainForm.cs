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
            Text = "🧰  LD7 Multitool",
            ForeColor = Color.White,
            Font = Estilo.FonteTitulo,
            AutoSize = false,
            Width = 186,
            Height = 56,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            Margin = new Padding(0, 4, 0, 16),
        };
        _menuLateral.Controls.Add(titulo);

        var modulos = DescobrirModulos();
        foreach (var modulo in modulos)
            _menuLateral.Controls.Add(CriarBotaoModulo(modulo));

        if (modulos.Count > 0)
            AbrirModulo(modulos[0], (Button)_menuLateral.Controls[1]);
    }

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
