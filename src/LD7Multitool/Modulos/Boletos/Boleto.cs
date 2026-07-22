namespace LD7Multitool.Modulos.Boletos;

public enum EstadoBoleto
{
    Aberto = 0,
    Pago = 1,
    Cancelado = 2,
    Protestado = 3,
}

public static class EstadoBoletoExtensoes
{
    public static string Descricao(this EstadoBoleto estado) => estado switch
    {
        EstadoBoleto.Aberto => "Aberto",
        EstadoBoleto.Pago => "Pago",
        EstadoBoleto.Cancelado => "Cancelado",
        EstadoBoleto.Protestado => "Protestado",
        _ => estado.ToString(),
    };

    /// <summary>Cor do texto usada na grade para cada estado (null = cor padrão).</summary>
    public static Color? CorTexto(this EstadoBoleto estado) => estado switch
    {
        EstadoBoleto.Pago => Color.FromArgb(30, 130, 76),        // verde
        EstadoBoleto.Cancelado => Color.FromArgb(110, 115, 130), // cinza
        EstadoBoleto.Protestado => Color.FromArgb(142, 68, 173), // lilás/roxo
        _ => null,
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

    /// <summary>Boleto em aberto vencendo em até 2 dias (ou já vencido) — merece alerta.</summary>
    public bool AlertaVencimento =>
        Estado == EstadoBoleto.Aberto && Validade.Date <= DateTime.Today.AddDays(2);
}
