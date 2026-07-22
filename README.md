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
   - **NF-e** — anexa o PDF mais recente da pasta de Notas Fiscais cujo nome
     contém `Cliente-{código}` (formato `DANFE Cliente-0000 (dd-mm-aaaa).pdf`);
   - **NF-e e Boleto** — idem, pegando também o boleto mais recente da pasta
     de Boletos (`BOLETO Cliente-0000 (dd-mm-aaaa).pdf`);
   - **Outro** — você escolhe os arquivos manualmente;
3. Confira anexos, assunto e mensagem, e clique em Enviar.

Nas configurações (⚙) ficam o servidor **SMTP** (com botão **Testar conexão**,
que envia um e-mail de teste para o próprio remetente) e as duas **pastas de
arquivos** (Notas Fiscais e Boletos). A senha do SMTP é protegida com DPAPI
(vinculada ao usuário do Windows) — nunca fica em texto puro no banco.

> Dica: para Gmail, use uma [senha de app](https://support.google.com/accounts/answer/185833)
> com servidor `smtp.gmail.com`, porta `587` e SSL/TLS habilitado.

### 🧾 Boletos
Gerenciador de boletos com os campos:
- Nome
- Valor
- Validade
- Nosso número
- NF-e referente
- Estado: **Aberto**, **Pago** ou **Cancelado**

Recursos: filtro por estado, atalhos "Marcar como pago" / "Cancelar boleto",
destaque de boletos vencidos (vermelho) e total em aberto no rodapé.

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
