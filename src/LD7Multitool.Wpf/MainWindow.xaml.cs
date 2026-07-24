using System.Windows;
using System.Windows.Controls;
using LD7Multitool.Wpf.Views;

namespace LD7Multitool.Wpf;

public partial class MainWindow : Window
{
    private ClientesView? _clientes;

    public MainWindow()
    {
        InitializeComponent();
        // Abre já no módulo Clientes (o que tem tela de verdade no protótipo).
        navClientes.IsChecked = true;
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (areaConteudo is null)
            return;

        var tag = (sender as RadioButton)?.Tag as string;
        areaConteudo.Content = tag switch
        {
            "clientes" => _clientes ??= new ClientesView(),
            "autoemail" => new PlaceholderView("Auto-Email"),
            "boletos" => new PlaceholderView("Boletos"),
            _ => null,
        };
    }
}
