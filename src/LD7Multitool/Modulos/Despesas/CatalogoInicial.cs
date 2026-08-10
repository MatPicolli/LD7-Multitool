using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Despesas;

/// <summary>
/// Semeia o módulo com os itens do relatório "DESPESAS LOJA" na primeira vez
/// que ele é aberto, para o usuário não ter que digitar os 25 itens.
///
/// <b>De propósito, nada de sigiloso é gravado aqui</b> — nem senha, nem CPF/CNPJ,
/// nem número de unidade consumidora/contrato, nem links com token de acesso
/// (como o do BRCondos). Isso ficaria versionado no repositório e visível para
/// qualquer um que clonasse o projeto. Esses campos são preenchidos uma única
/// vez pelo usuário na tela do item; a senha é guardada cifrada (ver
/// <see cref="Segredo"/>) e os demais campos ficam apenas no dados.db local.
/// </summary>
public static class CatalogoInicial
{
    /// <summary>Marca que a carga inicial já rodou (para os itens não voltarem depois de apagados).</summary>
    public const string ChaveSemeado = "despesas_catalogo_semeado";

    public static void SemearSeNecessario()
    {
        if (ConfiguracaoRepository.Obter(ChaveSemeado) == "1")
            return;

        // Segurança extra: se já existe qualquer item, não mexe em nada.
        if (DespesaRepository.Contar() == 0)
        {
            foreach (var despesa in Itens())
                DespesaRepository.Inserir(despesa);
        }

        ConfiguracaoRepository.Definir(ChaveSemeado, "1");
    }

    /// <summary>Os 25 itens do relatório, na mesma numeração/ordem do documento.</summary>
    public static List<Despesa> Itens() => new()
    {
        Novo(1, "SEMASA — Lojão", "Semasa Itajaí", FormaObtencao.Portal,
            "http://agencia.semasaitajai.com.br/Home/InfoSegundaVia",
            "Segunda via pela unidade consumidora — preencha a unidade em \"Identificador\"."),

        Novo(2, "Condomínio Haras Rio do Ouro", "BRCondos", FormaObtencao.Portal, "",
            "O boleto tem um link direto do BRCondos (ssl.brcondos.com.br/Bill/...). " +
            "Cole esse link em \"Portal\": basta selecionar o mês no canto superior esquerdo e imprimir."),

        Novo(3, "VIVO — José Bittencourt (fixo)", "Vivo", FormaObtencao.Telefone, "",
            "Ligar no 10315 e pedir a segunda via informando o CPF do titular; o boleto chega no e-mail. " +
            "Também dá para tirar pelo aplicativo do celular (o PIN fica anotado no relatório de despesas)."),

        Novo(4, "Cartão Arthur — Mastercard", "Mastercard", FormaObtencao.Terceiro, "",
            "A Jane solicita direto com ele para pagamento."),

        Novo(5, "FGTS — Lojão da 7", "Contabilidade", FormaObtencao.Email, "",
            "Chega todo mês junto com as folhas de pagamento. Se não vier, solicitar à contabilidade."),

        Novo(6, "Boleto Generation", "Generation", FormaObtencao.Email, "",
            "Chega todo mês por e-mail."),

        Novo(7, "VIVO — celulares Mateus, Ricardo e Arthur", "Vivo", FormaObtencao.Portal,
            "https://www.vivo.com.br",
            "Entrar em \"login\" com o CPF do titular. Depois: Minhas contas (canto esquerdo) → " +
            "Segunda via de conta → \"...\" → 2ª via detalhada."),

        Novo(8, "VIVO — celulares Jane e Marlésio", "Vivo", FormaObtencao.Portal,
            "https://www.vivo.com.br",
            "Entrar em \"login\" com o CPF do titular. Depois: Minhas contas (canto esquerdo) → " +
            "Segunda via de conta → \"...\" → 2ª via detalhada."),

        Novo(9, "EMBRATEL — José Bittencourt", "Embratel", FormaObtencao.Email,
            "http://fatura.embratel.net.br:9094/ebpp/ServBoletoSimplificado",
            "Vem por e-mail. O link da segunda via nem sempre imprime — conferir antes de depender dele."),

        Novo(10, "Apartamento Ricardo", "—", FormaObtencao.Terceiro, "",
            "A Jane pega direto com o Ricardo."),

        Novo(11, "CLARO — Lojão da Sete (internet)", "Claro Residencial", FormaObtencao.Email,
            "https://minhaclaroresidencial.claro.com.br/login",
            "Sempre vem por e-mail. No portal, o acesso é pelo CNPJ da loja."),

        Novo(12, "Redel — Arthur", "Redel", FormaObtencao.Terceiro, "",
            "A Jane pega diretamente com ele."),

        Novo(13, "Cassol — cartão", "Cassol", FormaObtencao.Terceiro, "",
            "Chega pelo WhatsApp — a Jane que pega."),

        Novo(14, "Cartão Ponto Frio", "Ponto Frio", FormaObtencao.Terceiro, "",
            "A Jane que pega."),

        Novo(15, "Cartão Marlésio — Visa Santander", "Santander", FormaObtencao.Terceiro, "",
            "A Jane que pega."),

        Novo(16, "Redel — Haras Rio do Ouro", "Redel", FormaObtencao.Terceiro, "",
            "A Jane que pega."),

        Novo(17, "Vuon — cartão Jane", "Vuon", FormaObtencao.Terceiro, "",
            "A Jane que pega."),

        Novo(18, "EMASA — Haras Rio do Ouro", "Emasa Balneário Camboriú", FormaObtencao.Portal,
            "https://balneariocamboriu.jtech.com.br/",
            "Entrar em \"segunda via\" e informar a matrícula (campo \"Identificador\") e o CPF do titular."),

        Novo(19, "CELESC — Haras", "Celesc", FormaObtencao.Portal,
            "https://agenciaweb.celesc.com.br/AgenciaWeb/autenticar/loginCliente.do",
            "Acesso pela unidade consumidora (campo \"Identificador\") com CPF do titular, e-mail e senha."),

        Novo(20, "CELESC — Lojão", "Celesc", FormaObtencao.Portal,
            "https://agenciaweb.celesc.com.br/AgenciaWeb/autenticar/loginCliente.do",
            "Acesso pela unidade consumidora (campo \"Identificador\") com o CNPJ da loja, e-mail e senha."),

        Novo(21, "DAS MEI — Militari", "Receita Federal", FormaObtencao.Portal,
            "https://www8.receita.fazenda.gov.br/SimplesNacional/Aplicacoes/ATSPO/pgmei.app/Identificacao",
            "Informar o CNPJ → Emitir guia de pagamento (DAS) → selecionar o ano, marcar o mês " +
            "no campo à esquerda, apurar valores e imprimir."),

        Novo(22, "CLARO — Lojão da Sete (celulares)", "Claro Empresas", FormaObtencao.Portal,
            "https://contaonline.claro.com.br/webbow/login/initPJ_oqe.do",
            "Login com o código da conta; depois Pagamentos → Boletos para pagamento. " +
            "O acesso pede uma senha de 6 dígitos e outra de 4."),

        Novo(23, "Cartão Marlésio — Mastercard Santander", "Santander", FormaObtencao.Terceiro, "",
            "A Jane que solicita."),

        Novo(24, "Fatura cartão Vivo (Jane)", "Vivo", FormaObtencao.Terceiro, "",
            "Fatura no nome da Jane — ela mesma solicita."),

        Novo(25, "Previdência — Marlésio", "Contabilidade", FormaObtencao.Email, "",
            "Vem por e-mail da contabilidade. Se não receber, solicitar que eles enviem."),
    };

    private static Despesa Novo(
        int ordem, string nome, string fornecedor, FormaObtencao forma, string url, string observacoes) => new()
    {
        Ordem = ordem,
        Nome = nome,
        Fornecedor = fornecedor,
        Forma = forma,
        UrlPortal = url,
        Observacoes = observacoes,
        // A coleta automática começa desligada em todos: cada item só passa a
        // ser buscado sozinho depois que o usuário informar os dados de acesso.
        Metodo = MetodoColeta.Nenhum,
        Ativo = true,
    };
}
