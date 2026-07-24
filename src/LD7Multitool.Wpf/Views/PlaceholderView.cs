using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LD7Multitool.Wpf.Views;

/// <summary>Tela de "ainda não portado" para os módulos fora do protótipo.</summary>
public class PlaceholderView : UserControl
{
    public PlaceholderView(string modulo)
    {
        Content = new TextBlock
        {
            Text = $"Módulo \"{modulo}\" — ainda não portado para WPF (protótipo).",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0x73, 0x82)),
        };
    }
}
