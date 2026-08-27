using LD7Multitool.Core;

namespace LD7Multitool.Modulos.NotasEntrada;

public class NotasEntradaModulo : IModulo
{
    public string Nome => "Notas de Entrada";
    public int Ordem => 5;
    public Control CriarControle() => new NotasEntradaControl();
}
