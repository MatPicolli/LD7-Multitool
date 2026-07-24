using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Configurações do módulo Clientes: hoje, só a importação de CSV.</summary>
public class ClientesConfigForm : Form
{
    /// <summary>True se algo foi importado (para o chamador atualizar a grade).</summary>
    public bool HouveAlteracao { get; private set; }

    public ClientesConfigForm()
    {
        Text = "Configurações — Clientes";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(560, 430);

        var rotuloTitulo = new Label
        {
            Text = "Importar clientes de um arquivo CSV",
            Font = new Font("Segoe UI Semibold", 10.5f),
            ForeColor = Estilo.CorPrimaria,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10),
        };

        var rotuloAjuda = new Label
        {
            Text =
                "Espera um arquivo no formato do MILITARISYS antigo, com uma linha por\r\n" +
                "cliente e as colunas nesta ordem (sem cabeçalho, ou com — é detectado\r\n" +
                "automaticamente):\r\n\r\n" +
                "código, razão social, nome fantasia, CPF/CNPJ, IE, endereço, CEP, UF,\r\n" +
                "cidade, bairro, e-mail 1, e-mail 2, telefone 1, telefone 2, contato,\r\n" +
                "e-mail do contato, telefone do contato, representante\r\n\r\n" +
                "• Clientes com código já cadastrado são pulados (não sobrescreve).\r\n" +
                "• Se o código vier em branco, um novo é gerado automaticamente.\r\n" +
                "• Representantes citados pelo nome que ainda não existem são\r\n" +
                "  criados automaticamente (só com o nome; complete depois).\r\n\r\n" +
                "Uma prévia é mostrada antes de confirmar a importação.",
            ForeColor = Estilo.CorTextoSuave,
            AutoSize = true,
            Margin = new Padding(0),
            MaximumSize = new Size(510, 0),
        };

        var conteudo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16),
            BackColor = Estilo.CorSuperficie,
        };
        conteudo.Controls.Add(rotuloTitulo);
        conteudo.Controls.Add(rotuloAjuda);

        var botaoImportar = Estilo.BotaoPrimario("Selecionar arquivo CSV...");
        botaoImportar.Click += (_, _) => SelecionarEImportar();

        var botaoFechar = Estilo.BotaoPadrao("Fechar");
        botaoFechar.DialogResult = DialogResult.OK;

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 58,
            Padding = new Padding(12, 10, 12, 10),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(botaoImportar);
        painelBotoes.Controls.Add(botaoFechar);

        Controls.Add(conteudo);
        Controls.Add(painelBotoes);

        AcceptButton = botaoImportar;
        CancelButton = botaoFechar;
    }

    private void SelecionarEImportar()
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Selecionar arquivo CSV de clientes",
            Filter = "Arquivos CSV (*.csv)|*.csv|Todos os arquivos (*.*)|*.*",
        };
        if (dialogo.ShowDialog(this) != DialogResult.OK)
            return;

        using var formulario = new ImportarClientesForm(dialogo.FileName);
        if (formulario.ShowDialog(this) != DialogResult.OK)
            return;

        HouveAlteracao = true;
        MessageBox.Show(this,
            $"{formulario.ImportadosCount} cliente(s) importado(s).\n" +
            $"{formulario.RepresentantesCriadosCount} representante(s) criado(s) automaticamente.\n" +
            $"{formulario.PuladosCount} linha(s) pulada(s) (duplicada(s) ou inválida(s)).",
            "Importação concluída", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
