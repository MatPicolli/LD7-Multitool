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
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;

        _menuLateral = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 200,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.FromArgb(45, 45, 60),
            Padding = new Padding(8),
        };

        _painelConteudo = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.Control,
        };

        Controls.Add(_painelConteudo);
        Controls.Add(_menuLateral);

        var titulo = new Label
        {
            Text = "LD7 Multitool",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            AutoSize = false,
            Width = 180,
            Height = 48,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 0, 12),
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
            Text = modulo.Nome,
            Width = 180,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(45, 45, 60),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 0, 0, 4),
        };
        botao.FlatAppearance.BorderSize = 0;
        botao.Click += (_, _) => AbrirModulo(modulo, botao);
        return botao;
    }

    private void AbrirModulo(IModulo modulo, Button botao)
    {
        if (_botaoAtivo is not null)
            _botaoAtivo.BackColor = Color.FromArgb(45, 45, 60);
        botao.BackColor = Color.FromArgb(80, 80, 110);
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
