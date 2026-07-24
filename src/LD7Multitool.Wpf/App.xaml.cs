using System.Windows;
using LD7Multitool.Core;

namespace LD7Multitool.Wpf;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Garante que o banco (o mesmo dados.db do WinForms) exista/esteja
        // migrado ANTES de abrir a janela, que já lê os clientes.
        Database.Inicializar();
        new MainWindow().Show();
    }
}
