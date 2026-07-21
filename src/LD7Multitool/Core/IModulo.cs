namespace LD7Multitool.Core;

/// <summary>
/// Contrato de um "mini-programa" do canivete suíço.
/// Para adicionar um módulo novo, basta criar uma classe que implemente
/// esta interface — ela é descoberta automaticamente na inicialização.
/// </summary>
public interface IModulo
{
    /// <summary>Nome exibido no menu lateral.</summary>
    string Nome { get; }

    /// <summary>Posição no menu lateral (menor aparece primeiro).</summary>
    int Ordem { get; }

    /// <summary>Cria o controle com a interface do módulo (chamado uma única vez).</summary>
    Control CriarControle();
}
