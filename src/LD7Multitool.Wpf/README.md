# LD7 Multitool — protótipo WPF (experimental)

Este é um **protótipo** para avaliar o visual/esforço de uma versão da interface
em **WPF** em vez de WinForms. Ele **não substitui** o programa atual: é um
projeto separado, lado a lado, que **reaproveita a mesma camada de dados**
(`Core/Database.cs`, `Cliente.cs`, `ClienteRepository.cs`, `Mascaras.cs` são
apenas *linkados* — nenhum arquivo do WinForms foi alterado).

Escopo do protótipo (**Shell + Clientes**):

- Janela principal com o **menu lateral** (Auto-Email, Boletos, Clientes);
- Auto-Email e Boletos são só telas de espaço reservado (placeholder);
- **Clientes** é uma tela real: lista os clientes do **mesmo `dados.db`** do
  WinForms, com busca por código/razão social/nome fantasia/CPF/CNPJ.

## Como rodar

```bash
dotnet run --project src/LD7Multitool.Wpf
```

Ele lê o `dados.db` que estiver ao lado do executável do protótipo. Para ver os
seus clientes reais, copie o seu `dados.db` para a pasta de saída, ou rode o
WinForms uma vez na mesma pasta.

## Como reverter (remover o experimento)

Como nada do WinForms foi tocado, basta **apagar a pasta**
`src/LD7Multitool.Wpf/` (e o commit correspondente). O programa principal
continua idêntico.
