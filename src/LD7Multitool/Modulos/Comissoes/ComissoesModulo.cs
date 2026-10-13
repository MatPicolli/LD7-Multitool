using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Comissoes;

public class ComissoesModulo : IModulo
{
    public string Nome => "Comissões";
    public int Ordem => 6;
    public string Categoria => Categorias.Militari;
    public Control CriarControle() => new ComissoesControl();
}
