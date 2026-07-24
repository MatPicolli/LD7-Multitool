using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

public class ClientesModulo : IModulo
{
    public string Nome => "Clientes";
    public int Ordem => 3;
    public Control CriarControle() => new ClientesControl();
}
