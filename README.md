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

### 👥 Clientes
Cadastro de clientes (reescrita do antigo MILITARISYS, agora integrado ao
SQLite do Multitool em vez de arquivos `.csv`), com tela em duas colunas
inspirada em ERPs de gestão comercial:

- **Código** de 4 dígitos gerado automaticamente (único, não editável) e
  marcador **Ativo/Inativo**;
- Alternância **Pessoa Física / Jurídica**: mostra CPF (Física) ou CNPJ
  (Jurídica) conforme a escolha, e habilita os dados pessoais (RG, estado
  civil, sexo, data de nascimento, nacionalidade, naturalidade) só para
  Física. O CPF/CNPJ é **formatado automaticamente enquanto você digita**
  (`123.456.789-00` / `12.345.678/0001-90`); no banco fica gravado só com
  dígitos;
- **Busca por CNPJ** (Jurídica): o botão 🌐 ao lado do campo consulta
  **várias fontes públicas ao mesmo tempo** (BrasilAPI, ReceitaWS, CNPJ.ws e
  Minha Receita) e abre um **comparativo** mostrando, lado a lado, o que cada
  uma retornou, o **tempo de resposta** de cada fonte e a **concordância**
  (semelhança) por campo e média geral. Você preenche o cadastro com os dados
  **consolidados** (valor da maioria) ou escolhendo uma fonte específica;
- Razão social/nome, nome fantasia, inscrições estadual e municipal;
- Endereço completo (logradouro, número, complemento, bairro) com **busca
  automática de CEP** (ViaCEP) — preenche logradouro, cidade, UF e bairro com
  um clique no botão 🌐;
- Telefone, celular, site, dois e-mails e contato (nome/e-mail/telefone);
- **Representante** vinculado, escolhido de um cadastro próprio de
  representantes (nome, e-mail, dois telefones), gerenciável pelo botão
  "Representantes" na barra superior;
- Botão **"Gravar (F8)"** salva o cadastro (atalho de teclado incluso). Se o
  CPF/CNPJ já pertencer a outro cliente, um aviso mostra o **código** e a razão
  social do cadastro existente e o salvamento é bloqueado (evita duplicados).

A pesquisa aceita código, razão social, nome fantasia ou CPF/CNPJ (tolerante a
acentos/maiúsculas). O botão **"Gerar ficha (PDF)"** cria a ficha cadastral do
cliente selecionado (dados do cliente, endereço, contato e representante) em
`Fichas_Cadastrais`, ao lado do executável.

**Importar CSV:** nas configurações (⚙) dá para importar de uma vez os
cadastros de um arquivo `.csv` no formato do MILITARISYS antigo (18 colunas,
sem cabeçalho — mas com cabeçalho também funciona, é detectado sozinho). Uma
prévia mostra o que vai ser importado antes de confirmar: clientes com código
já existente são pulados (nunca sobrescreve), código em branco gera um novo
automaticamente, e representantes citados pelo nome que ainda não existem são
criados na hora (só com o nome — complete os dados depois).

### 💡 Despesas
Controle das **despesas fixas da loja** (água, luz, telefone, internet,
condomínio, cartões, impostos): para cada item, onde buscar a conta, com quais
dados de acesso, e qual foi a **última conta** que chegou.

A tela principal mostra **uma linha por item** com a última competência,
vencimento, valor e situação. As cores contam a história de relance:

- **laranja** — ainda não chegou a conta deste mês (falta buscar);
- **vermelho** — a última conta está em aberto e já venceu;
- **verde** — paga; **cinza** — item inativo ou conta cancelada.

O rodapé resume quantos itens estão sem a conta do mês e o total em aberto.
Os filtros permitem ver só "Falta a conta do mês", "Em aberto", "Vencidos" ou
"Com coleta automática".

**Na primeira abertura**, o módulo já vem com os 25 itens do relatório de
despesas da loja (nome, fornecedor, endereço do portal e as instruções de como
tirar a segunda via). Por segurança, **nenhuma senha, CPF/CNPJ ou número de
contrato vem preenchido** — esses dados são digitados uma vez em "Editar item"
e ficam só no `dados.db` da sua máquina (a senha é gravada cifrada com DPAPI).

**Cadastro de cada item:** nome, fornecedor, como a conta costuma vir (portal /
e-mail / alguém pega / telefone), dia do vencimento, endereço do portal,
identificador (unidade consumidora, matrícula, código da conta), CPF/CNPJ,
login, senha e observações. Pelo clique-direito dá para **abrir o portal no
navegador**, **copiar o login**, **copiar a senha** ou **copiar a linha
digitável** da última conta — sem precisar procurar num papel.

**Lançar conta:** o botão "Lançar conta" abre a conta do mês (competência,
vencimento, valor, linha digitável, situação). O botão **"Ler de um PDF..."**
lê o boleto baixado e preenche valor e vencimento sozinho, pela linha digitável
(padrão FEBRABAN — vale para qualquer banco). "Contas do item" mostra todo o
histórico daquele item, com marcar como pago, editar e abrir o PDF.

**Buscar contas do mês (coleta automática):** o botão da barra superior varre
de uma vez todos os itens que têm coleta ligada e mostra um relatório do que
encontrou. Cada item escolhe seu método em "Editar item" → aba "Coleta
automática":

- **Pasta de downloads** — o programa procura na pasta configurada (⚙) os PDFs
  que casam com uma máscara (ex.: `celesc*haras*.pdf`) e lança valor e
  vencimento lidos da linha digitável. **É o caminho recomendado para portais
  com login complicado**: você baixa a segunda via como sempre fez e o resto é
  automático;
- **E-mail (IMAP)** — lê a caixa de entrada configurada no ⚙, filtra pelo
  remetente e/ou assunto, salva os anexos em PDF e lança as contas. Cobre os
  itens que "vêm por e-mail" (Generation, Embratel, Claro internet, FGTS,
  previdência...);
- **Portal (HTTP)** — consulta o site seguindo uma "receita" em JSON gravada no
  próprio item (endereço, campos do formulário e expressões de extração).
  Serve para portais simples, de formulário aberto. Sites com captcha,
  token/SMS ou aplicativo **não funcionam por aqui** — nesses, use a pasta de
  downloads.

A mesma conta nunca é cadastrada duas vezes: cada lançamento guarda uma chave
de origem (de preferência a própria linha digitável).

> Para o IMAP no Gmail/Outlook com verificação em duas etapas é preciso uma
> **senha de aplicativo** — a senha normal da conta é recusada.

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
│   ├── Segredo.cs              # cifra as senhas gravadas no banco (DPAPI)
│   └── ConfiguracaoRepository.cs
└── Modulos/
    ├── AutoEmail/              # módulo de envio de e-mails
    ├── Boletos/                # módulo gerenciador de boletos
    ├── Clientes/               # módulo de cadastro de clientes
    └── Despesas/               # módulo das despesas fixas da loja
```
