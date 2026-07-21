using LD7Multitool.Core;

namespace LD7Multitool;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.Inicializar();
        Application.Run(new MainForm());
    }
}
