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
  sandbox Linux **sem** SDK .NET nem Windows Forms — **não dá para compilar,
  rodar nem renderizar a UI aqui**. Valide a lógica por leitura (e por
  simulação em Python/regex quando fizer sentido) e seja honesto com o usuário
  sobre isso. Quem compila e testa é o usuário, na máquina Windows dele.

## Arquitetura modular (o ponto central)

Os módulos são descobertos **automaticamente por reflexão** — não há registro
manual. Para existir, um módulo só precisa de uma classe pública que implemente
`LD7Multitool.Core.IModulo`:

```csharp
public interface IModulo
{
    string Nome { get; }          // rótulo no menu lateral
    int Ordem { get; }            // posição no menu (1, 2, 3, ...)
    Control CriarControle();      // o UserControl/Control do módulo
}
```

`MainForm` varre o assembly, instancia todos os `IModulo`, ordena por `Ordem` e
monta o menu. **Para adicionar um módulo:** crie uma pasta em
`src/LD7Multitool/Modulos/<NomeDoModulo>/`, um `UserControl` com a tela, e a
classe `IModulo`. Se precisar de tabelas, adicione o `CREATE TABLE IF NOT
EXISTS ...` em `Core/Database.cs` (ver abaixo). Nada mais.

### Módulos atuais

| Ordem | Pasta        | O que faz |
|-------|--------------|-----------|
| 1     | `AutoEmail/` | Cadastro de clientes/e-mails e envio por SMTP com anexos (NF-e / NF-e+Boleto / Outro), modelos editáveis, histórico de envios. |
| 2     | `Boletos/`   | CRUD de boletos com estados (Aberto/Pago/Cancelado/Protestado), importação de PDF (linha digitável FEBRABAN), vínculo automático de NF-e por número, alerta de vencimento com envio por e-mail. |
| 3     | `Clientes/`  | Cadastro estilo ERP (reescrita do antigo "MILITARISYS"), Pessoa Física/Jurídica, busca de CEP (ViaCEP) e de CNPJ (BrasilAPI/ReceitaWS), importação de CSV, ficha em PDF, cadastro de representantes. |

> Já existiu um módulo **Consulta NF-e/CT-e** (SEFAZ com certificado A1). Foi
> **removido a pedido do usuário** por não funcionar bem. **Não reintroduza**
> sem ele pedir explicitamente.

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
- **`IModulo.cs`** — o contrato descrito acima.

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
  (CDNs recusam requisição sem ele), têm timeout e **retornam erro legível**
  em vez de engolir exceção. `ServicoCnpj` tenta BrasilAPI e cai para
  ReceitaWS como reserva.
- **Leitura de PDF**: `PdfPig` (boletos/NF-e). **Geração de PDF**:
  `PdfSharpCore` (ficha do cliente) — escolhido por ser MIT e não depender de
  GDI+.

## Dependências (NuGet) e por que estão fixadas

Ver `src/LD7Multitool/LD7Multitool.csproj`. Cuidados já resolvidos — **não
regrida**:

- `SQLitePCLRaw.bundle_e_sqlite3` **fixado em 2.1.11** de propósito, para
  corrigir a vulnerabilidade **NU1903 / GHSA-2m69-gcr7-jv3q**. Não abaixe.
- `Microsoft.Data.Sqlite` 9.0.0, `PdfPig` 0.1.9, `PdfSharpCore` 1.3.*.
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

## Dica de trabalho

Como não é possível testar aqui, **descreva claramente ao usuário o que mudou e
peça para ele rodar/testar**, principalmente em: parsing de PDF, chamadas de
rede (CEP/CNPJ) e ajustes visuais de WinForms. Prefira mudanças pequenas e
verificáveis a grandes refatorações não testadas.
