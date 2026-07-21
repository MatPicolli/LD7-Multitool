namespace LD7Multitool.Modulos.Boletos;

public enum EstadoBoleto
{
    Aberto = 0,
    Pago = 1,
    Cancelado = 2,
}

public static class EstadoBoletoExtensoes
{
    public static string Descricao(this EstadoBoleto estado) => estado switch
    {
        EstadoBoleto.Aberto => "Aberto",
        EstadoBoleto.Pago => "Pago",
        EstadoBoleto.Cancelado => "Cancelado",
        _ => estado.ToString(),
    };
}

public class Boleto
{
    public long Id { get; set; }
    public string Nome { get; set; } = "";
    public decimal Valor { get; set; }
    public DateTime Validade { get; set; } = DateTime.Today;
    public string NossoNumero { get; set; } = "";
    public string NfeReferente { get; set; } = "";
    public EstadoBoleto Estado { get; set; } = EstadoBoleto.Aberto;

    /// <summary>Caminho do PDF do boleto (vazio se não houver arquivo vinculado).</summary>
    public string CaminhoArquivo { get; set; } = "";

    public bool Vencido => Estado == EstadoBoleto.Aberto && Validade.Date < DateTime.Today;
}
