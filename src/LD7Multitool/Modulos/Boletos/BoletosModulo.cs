using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Boletos;

public class BoletosModulo : IModulo
{
    public string Nome => "Boletos";
    public int Ordem => 2;
    public string Categoria => Categorias.LojaoDa7;
    public Control CriarControle() => new BoletosControl();
}
