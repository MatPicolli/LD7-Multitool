using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

public class DespesasModulo : IModulo
{
    public string Nome => "Despesas";
    public int Ordem => 4;
    public string Categoria => Categorias.LojaoDa7;
    public Control CriarControle() => new DespesasControl();
}
