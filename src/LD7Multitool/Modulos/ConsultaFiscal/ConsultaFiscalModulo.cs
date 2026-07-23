using LD7Multitool.Core;

namespace LD7Multitool.Modulos.ConsultaFiscal;

public class ConsultaFiscalModulo : IModulo
{
    public string Nome => "Consulta NF-e/CT-e";
    public int Ordem => 3;
    public Control CriarControle() => new ConsultaFiscalControl();
}
