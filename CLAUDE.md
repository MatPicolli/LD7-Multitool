# CLAUDE.md — guia para agentes de IA

Este arquivo é para **agentes de IA** (Claude Code e afins) que forem mexer no
repositório **LD7 Multitool**. Resume a arquitetura, as convenções e as
armadilhas do projeto para não ter que redescobrir tudo a cada sessão. Se você
mudar algo estrutural, **atualize este arquivo junto**.

## O que é o projeto

Programa "canivete suíço" **modular** para Windows: uma janela principal
(`MainForm`) com menu lateral, onde cada **módulo** é um mini-programa
independente. Interface **inteiramente em português** (código, UI e mensagens).

- **Stack:** C# / **.NET 10** WinForms (`net10.0-windows`), `Nullable` e
  `ImplicitUsings` habilitados, `ApplicationHighDpiMode = PerMonitorV2`.
- **Dados:** **SQLite** (Microsoft.Data.Sqlite) num arquivo `dados.db` **na
  mesma pasta do executável** — o programa é **portátil**. Bancos antigos em
  `%AppData%\LD7Multitool` são migrados na primeira execução.
- **Alvo:** roda **só no Windows**. Este repositório costuma ser editado num
  sandbox Linux, onde **não dá para rodar nem ver a UI** (WinForms não existe
  no Linux) — mas **dá para compilar** e para **executar a lógica sem tela**
  (ver "Compilar e testar no sandbox Linux" abaixo). Faça isso antes de
  entregar; o que depende de tela (layout, cliques) continua sendo validado por
  leitura e pelo usuário, na máquina Windows dele — seja honesto sobre isso.

## Arquitetura modular (o ponto central)

Os módulos são descobertos **automaticamente por reflexão** — não há registro
manual. Para existir, um módulo só precisa de uma classe pública que implemente
`LD7Multitool.Core.IModulo`:

```csharp
public interface IModulo
{
    string Nome { get; }          // rótulo no menu lateral
    int Ordem { get; }            // posição no menu (1, 2, 3, ...)
    string Categoria { get; }     // grupo do menu: Categorias.LojaoDa7 ou Categorias.Militari
    Control CriarControle();      // o UserControl/Control do módulo
}
```

`MainForm` varre o assembly, instancia todos os `IModulo`, **agrupa por
`Categoria`** (um cabeçalho por grupo; os grupos seguem a menor `Ordem` dos seus
módulos) e, dentro do grupo, ordena por `Ordem`. **Para adicionar um módulo:**
crie uma pasta em `src/LD7Multitool/Modulos/<NomeDoModulo>/`, um `UserControl`
com a tela, e a classe `IModulo` — com a `Categoria` vinda de
`Core/Categorias.cs` (nunca string solta; um erro de digitação criaria um grupo
novo). Se precisar de tabelas, adicione o `CREATE TABLE IF NOT EXISTS ...` em
`Core/Database.cs` (ver abaixo). Nada mais.

O menu é dividido em duas categorias, porque o programa atende **dois
negócios** do usuário: **Lojão da 7** (a loja) e **MILITARI** (cadastro de
clientes/representantes e comissões). Não misture: módulo da loja vai em `LojaoDa7`.

### Módulos atuais

| Ordem | Categoria | Pasta        | O que faz |
|-------|-----------|--------------|-----------|
| 1     | Lojão da 7 | `AutoEmail/` | Cadastro de clientes/e-mails e envio por SMTP com anexos (NF-e / NF-e+Boleto / Outro), modelos editáveis, histórico de envios. |
| 2     | Lojão da 7 | `Boletos/`   | CRUD de boletos com estados (Aberto/Pago/Cancelado/Protestado), importação de PDF (linha digitável FEBRABAN), vínculo automático de NF-e por número, alerta de vencimento com envio por e-mail. |
| 3     | MILITARI | `Clientes/`  | Cadastro estilo ERP (reescrita do antigo "MILITARISYS"), Pessoa Física/Jurídica, busca de CEP (ViaCEP) e de CNPJ (BrasilAPI/ReceitaWS), importação de CSV, ficha em PDF, cadastro de representantes. |
| 4     | Lojão da 7 | `Despesas/`  | Despesas fixas da loja (água, luz, telefone, cartões...): catálogo dos itens com portal/credenciais, histórico de contas por item e **coleta automática** da última conta por pasta de downloads, e-mail (IMAP) ou consulta HTTP ao portal. |
| 5     | Lojão da 7 | `NotasEntrada/` | Organiza fotos de notas fiscais de entrada: galeria de miniaturas da pasta "para separar", seleção com ordem de página (clique = próxima página), visualizador com zoom/pan (botão direito abre/fecha) e separação para `Empresa/aaaa/dd-MM-aaaa(.jpg ou pasta com 01.jpg, 02.jpg...)`. Página nova de uma nota já separada vira a próxima numeração em vez de travar. |
| 6     | MILITARI | `Comissoes/` | Comissões da MILITARI — equivalente à aba "2026 (NOVA MILITAR)" da planilha "Comissão": uma linha **por parcela** (NF, Nº boleto, cliente, ref. pedido, valor NF, vencimento, parcela, valor da parcela, comissão = % × parcela, status pago/não pago, data do pagamento), totais (total / pago / falta pagar / vencidas), "Nova venda" que gera as N parcelas e importação da planilha .xlsx. |

> Já existiu um módulo **Consulta NF-e/CT-e** (SEFAZ com certificado A1). Foi
> **removido a pedido do usuário** por não funcionar bem. **Não reintroduza**
> sem ele pedir explicitamente.

### Comissões e a planilha do usuário

O módulo `Comissoes/` nasceu da planilha Excel "Comissão" do usuário (abas
2023…2026). Ao mexer nele, preserve o comportamento que reproduz a planilha:

- **Comissão = `ValorParcela × Percentual / 100`, sem arredondar por linha**
  (a planilha soma `=10%*K` cheio; arredondar linha a linha muda os totais).
  O `Percentual` fica **em cada linha** — mudar o padrão não reescreve o
  histórico.
- **Divisão das parcelas** (`GeradorParcelas.DividirValor`): trunca nos
  centavos e a última leva a diferença (17.230,40 / 13 → 12 × 1.325,41 +
  1.325,48) — é o que os boletos reais da planilha mostram.
- **Boleto "364-6"** segue em sequência (364-7 … 364-9, 365-0); outro texto
  (PIX) se repete.
- **Importação** (`PlanilhaComissoes`): colunas achadas pelo **texto do
  cabeçalho**, escolhendo a linha com mais títulos reconhecidos (a aba 2026 tem
  um cabeçalho parcial de uma tabela auxiliar uma linha acima). O Excel
  converteu parcelas como "1/5" em **data** (exibida "01/05"): dia/mês da data
  = parcela/total. Duplicidade evitada por `Comissao.ChaveImportacao` (NF +
  boleto + parcela + vencimento + valor) — há parcelas repetidas entre abas.
  Validado contra a planilha real: a aba 2026 bate os totais do Excel
  (11.350,18 / 8.736,02 / 2.614,16). **Não versione a planilha** (dados de
  clientes) nem a use como seed.

### Coleta de despesas (ponto de extensão)

O módulo `Despesas/` separa **item recorrente** (`Despesa` — o contrato: portal,
credenciais, identificador) de **conta do mês** (`LancamentoDespesa` — o boleto:
competência, vencimento, valor). A busca automática é plugável:
`IColetorDespesa` + as implementações `ColetorPasta`, `ColetorEmail` e
`ColetorHttp`, orquestradas por `ServicoColeta` (que também dedupe e grava).
Para criar um coletor novo, implemente a interface e registre no array
`ServicoColeta.Coletores`.

Duplicidade é evitada pela `chave_origem` (única por despesa) — de preferência a
própria linha digitável. **Não gere chave vazia.**

> **Não embuta raspador de portal no código.** Cada site (Semasa, Celesc, Vivo,
> Claro, PGMEI...) muda de layout e vários têm captcha/token/app — um raspador
> fixo quebra e ninguém consegue consertar sem recompilar. O `ColetorHttp` lê
> uma "receita" JSON gravada no próprio item (URL, campos, regex); para portal
> difícil, o caminho recomendado ao usuário é `ColetorPasta` (ele baixa o PDF
> como sempre e o programa lê a linha digitável).

## Camada Core (`src/LD7Multitool/Core/`)

- **`Database.cs`** — inicializa o SQLite e concentra **todo** o esquema
  (`CREATE TABLE IF NOT EXISTS`). Migrações de esquema são feitas com helpers
  idempotentes: `ColunaExiste`, `AdicionarColunaSeFaltar` e migrações
  específicas (ex.: `MigrarCpfCnpj`). **Ao mudar o esquema, migre — nunca
  suponha que o banco do usuário está vazio.** Há `AbrirConexao()` e
  `AbrirConexaoComFk()` (esta liga as foreign keys, use ao inserir/atualizar
  com FK).
- **`Estilo.cs`** — **design system** compartilhado: paleta de cores, fábricas
  de botões (`BotaoPrimario`, `BotaoPadrao`, `BotaoPerigo`, `BotaoIcone`),
  `CriarBarraSuperior()` e `EstilizarGrade(DataGridView)`. **Toda a UI deve
  passar por aqui** para manter consistência — não invente cores/botões
  soltos. Alturas de botão são fixas (`AlturaBotao`) para evitar corte de
  texto (foi um bug recorrente que o usuário reclamou).
- **`ConfiguracaoRepository.cs`** — chave/valor genérico para configurações
  (pastas, modelos de e-mail, etc.).
- **`Segredo.cs`** — cifra/decifra segredos gravados no banco (senhas de portal
  e do IMAP) com a **DPAPI do Windows, escopo do usuário atual**. Como o
  `dados.db` é portátil, senha em texto puro ali significaria distribuir a
  senha junto. O preço: ao levar o banco para outra máquina/usuário,
  `Revelar` devolve `""` e a senha precisa ser redigitada — **trate isso, não
  estoure**. Nunca grave senha sem passar por aqui.
  *Atenção:* `AutoEmail/ConfigSmtp.cs` tem a própria DPAPI, anterior a este
  arquivo e **sem o prefixo `dpapi:`**. Não troque uma pela outra sem migrar —
  `Revelar` devolveria o base64 cru como se fosse a senha.
- **`IModulo.cs`** — o contrato descrito acima.
- **`Categorias.cs`** — as constantes de categoria do menu (`LojaoDa7`, `Militari`).
- **`LeitorXlsx.cs`** — leitor mínimo de planilha **.xlsx** sem pacote externo
  (zip + XML do Office Open XML). Devolve só os **valores** das células (o
  resultado gravado das fórmulas, sem estilos): **datas chegam como número
  serial** — use `CelulaXlsx.ComoData()`; `ComoDecimal()`/`ComoTexto()` para o
  resto. Abre o arquivo mesmo aberto no Excel. Não lê o formato antigo `.xls`.

## Convenções de código

- **Tudo em português**: nomes de classes, métodos, variáveis, comentários e
  strings de UI. Siga o estilo do arquivo vizinho (ex.: `_campoNome`,
  `BuscarPorDocumento`, `AtualizarResumo`).
- **Repositórios estáticos** por entidade (`ClienteRepository`,
  `BoletoRepository`, ...) com métodos `Listar/Inserir/Atualizar/Excluir`.
- **CPF/CNPJ**: no banco ficam gravados **só com dígitos**. A máscara
  (`Modulos/Clientes/Mascaras.cs`) é só de exibição/digitação. Ao comparar
  documentos, compare **apenas os dígitos**.
- **Layout WinForms**: prefira `TableLayoutPanel`/`FlowLayoutPanel` com
  **alturas determinísticas** e docking a posições fixas (`Location`) — telas
  com `Location` fixo e `ClientSize` pequeno já quebraram (texto sobrepondo
  botão). Ao empilhar docks numa Form, siga o padrão já usado: adicione o
  controle `Fill` **primeiro**, depois os `Bottom`/`Top`.
- **Serviços de rede** (`ServicoCep`, `ServicoCnpj`): mandam `User-Agent`
  (CDNs recusam requisição sem ele) e têm timeout. `ServicoCnpj` consulta
  **várias fontes em paralelo** (BrasilAPI, ReceitaWS, CNPJ.ws, Minha Receita)
  via `ConsultarAsync`, mede o tempo de cada uma (`RespostaFonte`) e consolida
  os campos pelo voto da maioria (`Consolidar`/`DadosConsolidados`). A tela
  `ComparativoCnpjForm` mostra o comparativo (semelhança + tempo) e deixa o
  usuário escolher a fonte. Ao adicionar uma fonte nova, escreva um parser que
  normalize para o `record DadosCnpj` comum.
- **Leitura de PDF**: `PdfPig` (boletos/NF-e). **Geração de PDF**:
  `PdfSharpCore` (ficha do cliente) — escolhido por ser MIT e não depender de
  GDI+.
- **Linha digitável FEBRABAN**: valor e vencimento saem dos 14 dígitos finais
  (4 = fator de vencimento, 10 = centavos), com o ciclo reiniciado em 1000 em
  22/02/2025. Isso vale para boleto de qualquer banco e está em dois lugares:
  `Boletos/LeitorBoletoPdf.cs` (a partir de PDF) e `Despesas/Febraban.cs` (a
  partir de texto de página/HTML). Mexeu em um, confira o outro.
- **Nada de segredo versionado**: senhas, CPF/CNPJ, número de contrato/unidade
  consumidora e links com token (ex.: boleto de condomínio) **não entram no
  código** — nem em seed, nem em teste, nem em comentário. Ver
  `Despesas/CatalogoInicial.cs`: ele semeia só nome, fornecedor, URL pública e
  instruções; o resto o usuário preenche na tela e fica só no `dados.db`.

## Dependências (NuGet) e por que estão fixadas

Ver `src/LD7Multitool/LD7Multitool.csproj`. Cuidados já resolvidos — **não
regrida**:

- `SQLitePCLRaw.bundle_e_sqlite3` **fixado em 2.1.11** de propósito, para
  corrigir a vulnerabilidade **NU1903 / GHSA-2m69-gcr7-jv3q**. Não abaixe.
- `Microsoft.Data.Sqlite` 9.0.0, `PdfPig` 0.1.9, `PdfSharpCore` 1.3.*.
- `MailKit` 4.* — cliente **IMAP** da coleta de despesas (o .NET não tem um).
  Traz o MimeKit junto. É também o caminho natural se um dia o envio SMTP do
  Auto-Email precisar de OAuth2.
- **DPAPI (`ProtectedData`) não precisa de pacote** — vem com o
  `net10.0-windows`. Não adicione `System.Security.Cryptography.ProtectedData`.
- Ao lidar com certificado (se algum dia voltar), use `X509CertificateLoader`
  em vez de `new X509Certificate2(...)` (SYSLIB0057).

## Git — regras do repositório

- Branch de desenvolvimento: **`claude/modular-swiss-knife-program-7v8zg8`**.
  Faça commit e push sempre nela; push com `git push -u origin <branch>` e, em
  falha de rede, retente com backoff.
- **Não abra Pull Request** a menos que o usuário peça explicitamente.
- **Não** inclua o identificador do modelo em commits, código ou qualquer
  artefato do repositório.
- Mensagens de commit em português, descritivas, explicando o **porquê**.

## Como o usuário compila/roda (na máquina Windows dele)

```bash
dotnet run --project src/LD7Multitool          # rodar
dotnet publish src/LD7Multitool -c Release -o out   # gerar executável
```

## Compilar e testar no sandbox Linux

O SDK não vem instalado e o `dot.net`/`builds.dotnet.microsoft.com` costumam
estar bloqueados pelo proxy, mas o **repositório do Ubuntu** tem o .NET 10:

```bash
apt-get update && apt-get install -y dotnet-sdk-10.0
dotnet build src/LD7Multitool -p:EnableWindowsTargeting=true   # compila (não roda)
```

`EnableWindowsTargeting` deixa compilar um projeto `net10.0-windows` no Linux;
pega erro de compilação e aviso de nulidade antes do usuário. Para **testar
lógica** (leitura de PDF/planilha, cálculos, repositórios com SQLite), crie um
console `net10.0` **fora do repositório** (scratchpad) que inclua os `.cs`
necessários com `<Compile Include="...">` — só funciona com arquivos que não
usam WinForms — e rode contra dados reais. Não versione o projeto de teste nem
o `dados.db` que ele gera.

## Dica de trabalho

O que não dá para testar aqui (tela, cliques, layout, rede para fora do proxy):
**descreva claramente ao usuário o que mudou e peça para ele rodar/testar**,
principalmente parsing de PDF, chamadas de rede (CEP/CNPJ) e ajustes visuais de
WinForms. Prefira mudanças pequenas e verificáveis a grandes refatorações não
testadas.
