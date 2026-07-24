using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Formulário de criação/edição de um cliente, com busca automática de CEP.</summary>
public class ClienteForm : Form
{
    private readonly TextBox _campoCodigo;
    private readonly TextBox _campoRazaoSocial;
    private readonly TextBox _campoNomeFantasia;
    private readonly TextBox _campoCpfCnpj;
    private readonly TextBox _campoIe;
    private readonly TextBox _campoEndereco;
    private readonly TextBox _campoCep;
    private readonly TextBox _campoUf;
    private readonly TextBox _campoCidade;
    private readonly TextBox _campoBairro;
    private readonly TextBox _campoEmail1;
    private readonly TextBox _campoEmail2;
    private readonly TextBox _campoTelefone1;
    private readonly TextBox _campoTelefone2;
    private readonly TextBox _campoContato;
    private readonly TextBox _campoContatoEmail;
    private readonly TextBox _campoContatoTelefone;
    private readonly ComboBox _campoRepresentante;
    private readonly Button _botaoBuscarCep;

    private List<Representante> _representantes = new();

    public Cliente Cliente { get; }

    public ClienteForm(Cliente? cliente = null)
    {
        Cliente = cliente ?? new Cliente { Codigo = ClienteRepository.GerarCodigoUnico() };

        Text = cliente is null ? "Novo cliente" : "Editar cliente";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(660, 620);
        ClientSize = new Size(660, 620);

        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 12,
            Padding = new Padding(16),
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _campoCodigo = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Codigo, ReadOnly = true, BackColor = Estilo.CorFundo };
        _campoRazaoSocial = new TextBox { Dock = DockStyle.Fill, Text = Cliente.RazaoSocial };
        _campoNomeFantasia = new TextBox { Dock = DockStyle.Fill, Text = Cliente.NomeFantasia };
        _campoCpfCnpj = new TextBox { Text = Cliente.CpfCnpj };
        _campoIe = new TextBox { Text = Cliente.Ie };
        _campoEndereco = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Endereco };
        _campoCep = new TextBox { Text = Cliente.Cep };
        _campoUf = new TextBox { Text = Cliente.Uf };
        _campoCidade = new TextBox { Text = Cliente.Cidade };
        _campoBairro = new TextBox { Text = Cliente.Bairro };
        _campoEmail1 = new TextBox { Text = Cliente.Email1 };
        _campoEmail2 = new TextBox { Text = Cliente.Email2 };
        _campoTelefone1 = new TextBox { Text = Cliente.Telefone1 };
        _campoTelefone2 = new TextBox { Text = Cliente.Telefone2 };
        _campoContato = new TextBox { Text = Cliente.Contato };
        _campoContatoEmail = new TextBox { Text = Cliente.ContatoEmail };
        _campoContatoTelefone = new TextBox { Text = Cliente.ContatoTelefone };

        _campoRepresentante = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
        };
        CarregarRepresentantes();

        _botaoBuscarCep = Estilo.BotaoPadrao("Buscar 🌎");
        _botaoBuscarCep.Click += async (_, _) => await BuscarCepAsync();

        var linha = 0;
        AdicionarLinha(tabela, linha++, "Código:", _campoCodigo);
        AdicionarLinha(tabela, linha++, "Razão Social:", _campoRazaoSocial);
        AdicionarLinha(tabela, linha++, "Nome Fantasia:", _campoNomeFantasia);
        AdicionarLinha(tabela, linha++, "CPF/CNPJ / IE:", CamposLadoALado((_campoCpfCnpj, 3), (_campoIe, 1)));
        AdicionarLinha(tabela, linha++, "Endereço:", _campoEndereco);
        AdicionarLinha(tabela, linha++, "CEP:", CriarLinhaCep());
        AdicionarLinha(tabela, linha++, "UF / Cidade / Bairro:", CamposLadoALado((_campoUf, 1), (_campoCidade, 2), (_campoBairro, 2)));
        AdicionarLinha(tabela, linha++, "E-mail(s):", CamposLadoALado((_campoEmail1, 1), (_campoEmail2, 1)));
        AdicionarLinha(tabela, linha++, "Telefone(s):", CamposLadoALado((_campoTelefone1, 1), (_campoTelefone2, 1)));
        AdicionarLinha(tabela, linha++, "Contato:", CamposLadoALado((_campoContato, 1), (_campoContatoEmail, 2), (_campoContatoTelefone, 1)));
        AdicionarLinha(tabela, linha++, "Representante:", _campoRepresentante);

        var botaoSalvar = Estilo.BotaoPrimario("Salvar");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        painelBotoes.Controls.Add(botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        tabela.Controls.Add(painelBotoes, 1, linha);

        Controls.Add(tabela);
        AcceptButton = botaoSalvar;
        CancelButton = botaoCancelar;
    }

    private void CarregarRepresentantes()
    {
        _representantes = RepresentanteRepository.Listar();
        _campoRepresentante.Items.Clear();
        _campoRepresentante.Items.Add("(nenhum)");
        foreach (var r in _representantes)
            _campoRepresentante.Items.Add(r);

        var indice = Cliente.RepresentanteId is { } id
            ? _representantes.FindIndex(r => r.Id == id) + 1
            : 0;
        _campoRepresentante.SelectedIndex = Math.Max(0, indice);
    }

    private static void AdicionarLinha(TableLayoutPanel tabela, int linha, string rotulo, Control campo)
    {
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        tabela.Controls.Add(campo, 1, linha);
    }

    /// <summary>Coloca vários campos numa linha só, cada um com seu peso relativo de largura.</summary>
    private static Control CamposLadoALado(params (Control Campo, int Peso)[] itens)
    {
        var painel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = itens.Length,
            RowCount = 1,
            Margin = new Padding(0),
        };
        for (var i = 0; i < itens.Length; i++)
        {
            painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, itens[i].Peso));
            itens[i].Campo.Dock = DockStyle.Fill;
            itens[i].Campo.Margin = new Padding(i == 0 ? 0 : 4, 0, 0, 0);
            painel.Controls.Add(itens[i].Campo, i, 0);
        }
        return painel;
    }

    private Control CriarLinhaCep()
    {
        _campoCep.Dock = DockStyle.Fill;
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        painel.Controls.Add(_campoCep, 0, 0);
        _botaoBuscarCep.Margin = new Padding(4, 0, 0, 0);
        painel.Controls.Add(_botaoBuscarCep, 1, 0);
        return painel;
    }

    private async Task BuscarCepAsync()
    {
        _botaoBuscarCep.Enabled = false;
        UseWaitCursor = true;
        try
        {
            var endereco = await ServicoCep.BuscarAsync(_campoCep.Text);
            if (endereco is null)
            {
                MessageBox.Show(this, "CEP não encontrado.", "Buscar CEP",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _campoEndereco.Text = endereco.Logradouro.ToUpperInvariant();
            _campoCep.Text = endereco.Cep;
            _campoUf.Text = endereco.Uf.ToUpperInvariant();
            _campoCidade.Text = endereco.Cidade.ToUpperInvariant();
            _campoBairro.Text = endereco.Bairro.ToUpperInvariant();
        }
        finally
        {
            _botaoBuscarCep.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Salvar()
    {
        if (string.IsNullOrWhiteSpace(_campoRazaoSocial.Text))
        {
            MessageBox.Show(this, "Informe a Razão Social.", "Campo obrigatório",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Cliente.RazaoSocial = _campoRazaoSocial.Text.Trim();
        Cliente.NomeFantasia = _campoNomeFantasia.Text.Trim();
        Cliente.CpfCnpj = _campoCpfCnpj.Text.Trim();
        Cliente.Ie = _campoIe.Text.Trim();
        Cliente.Endereco = _campoEndereco.Text.Trim();
        Cliente.Cep = _campoCep.Text.Trim();
        Cliente.Uf = _campoUf.Text.Trim();
        Cliente.Cidade = _campoCidade.Text.Trim();
        Cliente.Bairro = _campoBairro.Text.Trim();
        Cliente.Email1 = _campoEmail1.Text.Trim();
        Cliente.Email2 = _campoEmail2.Text.Trim();
        Cliente.Telefone1 = _campoTelefone1.Text.Trim();
        Cliente.Telefone2 = _campoTelefone2.Text.Trim();
        Cliente.Contato = _campoContato.Text.Trim();
        Cliente.ContatoEmail = _campoContatoEmail.Text.Trim();
        Cliente.ContatoTelefone = _campoContatoTelefone.Text.Trim();
        Cliente.RepresentanteId = _campoRepresentante.SelectedIndex <= 0
            ? null
            : _representantes[_campoRepresentante.SelectedIndex - 1].Id;

        DialogResult = DialogResult.OK;
        Close();
    }
}
