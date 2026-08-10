using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>De onde a conta do mês costuma vir (é informativo, orienta o usuário).</summary>
public enum FormaObtencao
{
    Portal = 0,
    Email = 1,
    Terceiro = 2,
    Telefone = 3,
}

/// <summary>Como o programa tenta buscar a conta sozinho.</summary>
public enum MetodoColeta
{
    /// <summary>Só lançamento manual — nada é buscado automaticamente.</summary>
    Nenhum = 0,

    /// <summary>Varre a pasta de downloads atrás do PDF do boleto.</summary>
    Pasta = 1,

    /// <summary>Lê a caixa de entrada (IMAP) atrás do e-mail com o boleto anexado.</summary>
    Email = 2,

    /// <summary>Consulta o portal por HTTP usando a receita configurada no item.</summary>
    Http = 3,
}

public enum SituacaoDespesa
{
    Aberto = 0,
    Pago = 1,
    Cancelado = 2,
}

/// <summary>Como o lançamento entrou no sistema.</summary>
public enum OrigemLancamento
{
    Manual = 0,
    Pasta = 1,
    Email = 2,
    Portal = 3,
}

public static class DespesaExtensoes
{
    public static string Descricao(this FormaObtencao forma) => forma switch
    {
        FormaObtencao.Portal => "Portal (site)",
        FormaObtencao.Email => "Chega por e-mail",
        FormaObtencao.Terceiro => "Alguém pega",
        FormaObtencao.Telefone => "Por telefone",
        _ => forma.ToString(),
    };

    public static string Descricao(this MetodoColeta metodo) => metodo switch
    {
        MetodoColeta.Nenhum => "Manual",
        MetodoColeta.Pasta => "Pasta de downloads",
        MetodoColeta.Email => "E-mail (IMAP)",
        MetodoColeta.Http => "Portal (HTTP)",
        _ => metodo.ToString(),
    };

    public static string Descricao(this SituacaoDespesa situacao) => situacao switch
    {
        SituacaoDespesa.Aberto => "Em aberto",
        SituacaoDespesa.Pago => "Pago",
        SituacaoDespesa.Cancelado => "Cancelado",
        _ => situacao.ToString(),
    };

    public static string Descricao(this OrigemLancamento origem) => origem switch
    {
        OrigemLancamento.Manual => "Manual",
        OrigemLancamento.Pasta => "Pasta",
        OrigemLancamento.Email => "E-mail",
        OrigemLancamento.Portal => "Portal",
        _ => origem.ToString(),
    };

    /// <summary>Cor do texto na grade para cada situação (null = cor padrão).</summary>
    public static Color? CorTexto(this SituacaoDespesa situacao) => situacao switch
    {
        SituacaoDespesa.Pago => Color.FromArgb(30, 130, 76),      // verde
        SituacaoDespesa.Cancelado => Color.FromArgb(110, 115, 130), // cinza
        _ => null,
    };
}

/// <summary>
/// Um item de despesa recorrente da loja (água, luz, telefone, cartão...):
/// onde buscar a conta, com quais dados e como o programa pode automatizar.
/// </summary>
public class Despesa
{
    public long Id { get; set; }

    /// <summary>Rótulo do item, como aparece no relatório (ex.: "CELESC — Lojão").</summary>
    public string Nome { get; set; } = "";

    /// <summary>Empresa/credor (ex.: "Celesc", "Claro", "Vivo").</summary>
    public string Fornecedor { get; set; } = "";

    public FormaObtencao Forma { get; set; } = FormaObtencao.Portal;
    public MetodoColeta Metodo { get; set; } = MetodoColeta.Nenhum;

    /// <summary>Endereço do portal de segunda via.</summary>
    public string UrlPortal { get; set; } = "";

    /// <summary>Unidade consumidora, matrícula, código da conta — o que identifica o contrato.</summary>
    public string Identificador { get; set; } = "";

    /// <summary>CPF/CNPJ usado no acesso (só dígitos, como no resto do programa).</summary>
    public string Documento { get; set; } = "";

    public string Login { get; set; } = "";

    /// <summary>Senha já protegida (DPAPI). Use <see cref="Senha"/> para ler/gravar em claro.</summary>
    public string SenhaProtegida { get; set; } = "";

    /// <summary>Senha em texto puro — só existe em memória; grava e lê via <see cref="Segredo"/>.</summary>
    public string Senha
    {
        get => Segredo.Revelar(SenhaProtegida);
        set => SenhaProtegida = Segredo.Proteger(value);
    }

    /// <summary>Dia do mês em que costuma vencer (0 = variável). Usado só para ordenar/avisar.</summary>
    public int DiaVencimento { get; set; }

    /// <summary>Máscara do arquivo na pasta de downloads (ex.: "celesc*haras*.pdf").</summary>
    public string PadraoArquivo { get; set; } = "";

    /// <summary>Trecho do remetente do e-mail que traz o boleto (ex.: "generation").</summary>
    public string EmailRemetente { get; set; } = "";

    /// <summary>Trecho do assunto do e-mail que traz o boleto.</summary>
    public string EmailAssunto { get; set; } = "";

    /// <summary>Receita de consulta HTTP em JSON (ver <see cref="ColetorHttp"/>).</summary>
    public string ConfigHttp { get; set; } = "";

    public string Observacoes { get; set; } = "";

    public bool Ativo { get; set; } = true;

    /// <summary>Posição na lista — segue a numeração do relatório de despesas.</summary>
    public int Ordem { get; set; }

    public override string ToString() => Nome;
}

/// <summary>A conta de um mês (um boleto/fatura) de um item de despesa.</summary>
public class LancamentoDespesa
{
    public long Id { get; set; }
    public long DespesaId { get; set; }

    /// <summary>Mês de referência no formato yyyy-MM (vazio quando não se sabe).</summary>
    public string Competencia { get; set; } = "";

    public DateTime Vencimento { get; set; } = DateTime.Today;
    public decimal Valor { get; set; }
    public string LinhaDigitavel { get; set; } = "";
    public SituacaoDespesa Situacao { get; set; } = SituacaoDespesa.Aberto;

    /// <summary>Caminho do PDF do boleto/fatura (vazio se não houver).</summary>
    public string CaminhoArquivo { get; set; } = "";

    public OrigemLancamento Origem { get; set; } = OrigemLancamento.Manual;

    /// <summary>Quando o lançamento foi criado/coletado.</summary>
    public DateTime ColetadoEm { get; set; } = DateTime.Now;

    /// <summary>
    /// Identidade do lançamento na origem (linha digitável, caminho do arquivo,
    /// id da mensagem...). É única por despesa e serve para a coleta não
    /// cadastrar o mesmo boleto duas vezes.
    /// </summary>
    public string ChaveOrigem { get; set; } = "";

    public bool Vencido =>
        Situacao == SituacaoDespesa.Aberto && Vencimento.Date < DateTime.Today;

    public bool AlertaVencimento =>
        Situacao == SituacaoDespesa.Aberto && Vencimento.Date <= DateTime.Today.AddDays(3);

    /// <summary>Competência legível (MM/yyyy); cai para o mês do vencimento se não houver.</summary>
    public string CompetenciaFormatada =>
        DateTime.TryParseExact(Competencia, "yyyy-MM",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var mes)
            ? mes.ToString("MM/yyyy")
            : Vencimento.ToString("MM/yyyy");
}
