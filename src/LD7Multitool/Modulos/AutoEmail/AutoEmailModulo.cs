using LD7Multitool.Core;

namespace LD7Multitool.Modulos.AutoEmail;

public class AutoEmailModulo : IModulo
{
    public string Nome => "Auto-Email";
    public int Ordem => 1;
    public Control CriarControle() => new AutoEmailControl();
}
