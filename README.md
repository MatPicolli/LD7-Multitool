# LD7 Multitool

Programa "canivete suíço" modular para Windows: uma janela principal com menu
lateral onde cada **módulo** é um mini-programa independente.

Interface totalmente em **português**. Dados gravados em banco **SQLite** local
(`%AppData%\LD7Multitool\dados.db`).

## Módulos atuais

### 📧 Auto-Email
Cadastros de envio de e-mail, cada um com:
- **Vários destinatários** (não apenas um por cadastro)
- **Arquivos anexados** (caminhos salvos; anexados na hora do envio)
- Assunto e corpo da mensagem

O envio usa um servidor **SMTP configurável** (botão *Configurar SMTP*). A senha
do SMTP é protegida com DPAPI (vinculada ao usuário do Windows) — nunca fica em
texto puro no banco.

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
