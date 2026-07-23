# LD7 Multitool

Programa "canivete suíço" modular para Windows: uma janela principal com menu
lateral onde cada **módulo** é um mini-programa independente.

Interface totalmente em **português**. Dados e configurações gravados num banco
**SQLite** (`dados.db`) na **mesma pasta do executável** — o programa é
portátil: copiar a pasta leva tudo junto. (Bancos criados por versões antigas
em `%AppData%\LD7Multitool` são migrados automaticamente na primeira execução.)

## Módulos atuais

### 📧 Auto-Email
Cadastro de clientes com **código** (ex.: 5551), **nome** e **um ou mais
e-mails**. Na hora de enviar:

1. Pesquise o cliente por nome ou código (busca tolerante/fuzzy — aceita
   trechos e letras fora de sequência, sem acentos);
2. Escolha o que mandar:
   - **NF-e** — um dropdown lista **todos** os PDFs de NF-e do cliente
     (`DANFE Cliente-0000 (dd-mm-aaaa).pdf`), com o mais recente já
     selecionado; dá para escolher um documento mais antigo se quiser;
   - **NF-e e Boleto** — dois dropdowns (NF-e e Boleto,
     `BOLETO Cliente-0000 (dd-mm-aaaa).pdf`), cada um com o mais recente
     pré-selecionado;
   - **Outro** — você escolhe os arquivos manualmente;
3. Confira anexos, assunto e mensagem, e clique em Enviar.

Nas configurações (⚙) ficam o servidor **SMTP** (com botão **Testar conexão**,
que envia um e-mail de teste para o próprio remetente) e as duas **pastas de
arquivos** (Notas Fiscais e Boletos). A senha do SMTP é protegida com DPAPI
(vinculada ao usuário do Windows) — nunca fica em texto puro no banco.

Nas configurações (⚙) também dá para editar o **modelo padrão** do assunto e do
corpo do e-mail, com placeholders `{cliente}`, `{codigo}` e `{tipo}`.

A tela principal mostra ainda um **histórico dos últimos e-mails enviados**
(data/hora, destinatários, assunto e quantidade de anexos), incluindo os
enviados a partir do módulo de Boletos.

> Dica: para Gmail, use uma [senha de app](https://support.google.com/accounts/answer/185833)
> com servidor `smtp.gmail.com`, porta `587` e SSL/TLS habilitado.

### 🧾 Boletos
Gerenciador de boletos com os campos:
- Nome
- Valor
- Vencimento
- Nosso número
- NF-e referente
- Estado: **Aberto**, **Pago**, **Cancelado** ou **Protestado**

Recursos: **pesquisa** por nome, valor, validade, nosso número ou NF-e
referente; filtro por estado (o filtro **Aberto** também inclui os
**Protestados**, por ainda serem dívidas em aberto; escolhendo **Protestado**
mostra apenas esses); ordenação por qualquer coluna clicando no cabeçalho
(validade ordena por data e valor por número, não pelo texto);
atalhos "Marcar como pago" / "protestado" / "Cancelar boleto"; destaque por
estado (pago em verde, protestado em lilás, cancelado em cinza) e boletos
vencidos em vermelho; total em aberto no rodapé.

**Vínculo automático de NF-e:** nas configurações (⚙) há também a pasta das
Notas Fiscais. Ao cadastrar (novo ou importado) um boleto, o programa lê os
PDFs de NF-e e vincula automaticamente aquele cujo número (lido do topo do
DANFE, "Nº ...") bate com o "NF-e referente" do boleto. Para vincular os
boletos que **já estavam cadastrados** antes, use o botão **"Vincular NF-es
agora"** nas configurações (⚙) — ele varre a pasta e liga todos os que ainda
não têm NF-e.

**Alerta de vencimento:** boletos em aberto vencendo em até 2 dias (ou já
vencidos) mostram um ícone ⚠ na coluna Vencimento. Clicando no ícone abre uma
janela para **enviar o boleto por e-mail** — escolhendo um endereço já
cadastrado (buscável por código ou nome) ou digitando um e-mail manualmente.
O PDF do boleto vai como anexo e, se houver uma **NF-e vinculada**, há a opção
de anexá-la junto. Pelo menu de clique-direito dá para abrir o PDF do boleto
ou a NF-e vinculada.

O cadastro escolhido no envio fica **memorizado pelo nome do boleto**: ao enviar
outro boleto do mesmo pagador, os e-mails daquele cadastro já vêm
pré-selecionados. O assunto e o corpo padrão do e-mail de boleto também são
editáveis nas configurações (⚙), com placeholders `{nome}`, `{valor}` e
`{vencimento}`.

**Importação de PDFs:** configure a pasta onde ficam os PDFs dos boletos no
botão de engrenagem (⚙) e use **Importar boletos** — cada PDF novo da pasta é
lido e os dados são **extraídos automaticamente**. Um PDF com **várias parcelas**
(uma por página) gera um boleto para cada parcela, cada um com seu próprio valor,
vencimento e nosso número:

- **Valor** e **vencimento** são lidos da linha digitável (padrão FEBRABAN,
  funciona com boleto de qualquer banco);
- **Nome do pagador**, **nosso número** e **NF-e referente** (nº do documento)
  são lidos por heurísticas do layout do boleto — se algum não for encontrado,
  o campo fica em branco para completar manualmente.

O botão **Abrir PDF** abre o arquivo vinculado ao boleto selecionado.

### 🔎 Consulta NF-e/CT-e
Consulta a **situação** de uma NF-e/CT-e direto no web service da SEFAZ, usando
seu **certificado digital A1**, para imprimir o comprovante de autenticidade no
**verso da nota** — sem navegar no site.

Fluxo sem botões: o cursor fica no campo da chave; ao **bipar** (ou digitar) a
chave de acesso de 44 dígitos, o programa detecta o tipo (NF-e modelo 55 / CT-e
modelo 57) e a UF pela própria chave, consulta a SEFAZ e — se a impressão
automática estiver ligada — imprime a situação, o protocolo e a data.

Nas configurações (⚙): caminho do certificado **.pfx/.p12** e senha (protegida
com DPAPI), ambiente (Produção/Homologação), impressora e as **URLs dos
endpoints** (em branco usam os padrões SVRS, que atendem SC — ajustáveis para
outras UFs/autorizadores).

> Observação: os endpoints e o comportamento do certificado dependem do
> ambiente real da SEFAZ e do Windows; podem precisar de ajuste fino na
> primeira configuração.

## Requisitos

- Windows
- [.NET SDK 10](https://dotnet.microsoft.com/download) (o projeto usa
  `net10.0-windows` com Windows Forms)

## Como rodar

```bash
dotnet run --project src/LD7Multitool
```

Para gerar um executável:

```bash
dotnet publish src/LD7Multitool -c Release -o out
```

## Como adicionar um módulo novo

A janela principal descobre os módulos automaticamente por reflexão. Para criar
um mini-programa novo:

1. Crie uma pasta em `src/LD7Multitool/Modulos/MeuModulo/`.
2. Crie um `UserControl` com a interface do módulo.
3. Crie uma classe que implemente `LD7Multitool.Core.IModulo`:

```csharp
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.MeuModulo;

public class MeuModulo : IModulo
{
    public string Nome => "Meu módulo";      // nome no menu lateral
    public int Ordem => 3;                   // posição no menu
    public Control CriarControle() => new MeuModuloControl();
}
```

Pronto — na próxima execução o módulo aparece no menu lateral. Se precisar de
tabelas próprias, adicione o `CREATE TABLE IF NOT EXISTS ...` em
`Core/Database.cs`.

## Estrutura do projeto

```
src/LD7Multitool/
├── Program.cs                  # ponto de entrada
├── MainForm.cs                 # janela principal + menu lateral
├── Core/
│   ├── IModulo.cs              # contrato dos módulos
│   ├── Database.cs             # SQLite + criação do esquema
│   └── ConfiguracaoRepository.cs
└── Modulos/
    ├── AutoEmail/              # módulo de envio de e-mails
    └── Boletos/                # módulo gerenciador de boletos
```
