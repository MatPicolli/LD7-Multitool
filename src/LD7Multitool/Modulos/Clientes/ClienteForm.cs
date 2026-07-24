using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>
/// Formulário de criação/edição de um cliente: layout em duas colunas
/// (Dados pessoais/Representante à esquerda, Endereço/Contato à direita),
/// com alternância Física/Jurídica e busca automática de CEP.
/// </summary>
public class ClienteForm : Form
{
    private readonly RadioButton _radioFisica;
    private readonly RadioButton _radioJuridica;
    private readonly CheckBox _campoAtivo;

    private readonly TextBox _campoCodigo;
    private readonly TextBox _campoCnpj;
    private readonly TextBox _campoCpf;
    private readonly TextBox _campoRazaoSocial;
    private readonly TextBox _campoNomeFantasia;
    private readonly TextBox _campoRg;
    private readonly TextBox _campoIe;
    private readonly TextBox _campoInscricaoMunicipal;
    private readonly ComboBox _campoEstadoCivil;
    private readonly ComboBox _campoSexo;
    private readonly DateTimePicker _campoDataNascimento;
    private readonly TextBox _campoNacionalidade;
    private readonly TextBox _campoNaturalidade;

    private readonly TextBox _campoEndereco;
    private readonly TextBox _campoNumero;
    private readonly TextBox _campoComplemento;
    private readonly TextBox _campoCep;
    private readonly TextBox _campoUf;
    private readonly TextBox _campoCidade;
    private readonly TextBox _campoBairro;
    private readonly Button _botaoBuscarCep;

    private readonly TextBox _campoTelefone;
    private readonly TextBox _campoCelular;
    private readonly TextBox _campoSite;
    private readonly TextBox _campoEmail1;
    private readonly TextBox _campoEmail2;
    private readonly TextBox _campoContato;
    private readonly TextBox _campoContatoEmail;
    private readonly TextBox _campoContatoTelefone;

    private readonly ComboBox _campoRepresentante;
    private readonly Button _botaoSalvar;

    private List<Representante> _representantes = new();

    public Cliente Cliente { get; }

    public ClienteForm(Cliente? cliente = null)
    {
        var novo = cliente is null;
        Cliente = cliente ?? new Cliente
        {
            Codigo = ClienteRepository.GerarCodigoUnico(),
            Nacionalidade = "BRASILEIRA",
        };

        Text = novo ? "Novo cliente" : "Editar cliente";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(960, 760);
        ClientSize = new Size(960, 760);
        KeyPreview = true;

        // --- Cabeçalho: modo + Ativo ----------------------------------------
        var cabecalho = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 2,
            BackColor = Estilo.CorFundo,
            Padding = new Padding(20, 12, 20, 0),
        };
        cabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cabecalho.Controls.Add(new Label
        {
            Text = novo ? "Inserindo" : "Editando",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12f),
        }, 0, 0);

        _campoAtivo = new CheckBox { Text = "Ativo", AutoSize = true, Checked = Cliente.Ativo, Anchor = AnchorStyles.Right };
        cabecalho.Controls.Add(_campoAtivo, 1, 0);

        // --- Campos: Dados pessoais ------------------------------------------
        _campoCodigo = new TextBox { Text = Cliente.Codigo, ReadOnly = true, BackColor = Estilo.CorFundo };

        _radioFisica = new RadioButton { Text = "Física", AutoSize = true, Checked = Cliente.Tipo == TipoCliente.Fisica };
        _radioJuridica = new RadioButton { Text = "Jurídica", AutoSize = true, Checked = Cliente.Tipo == TipoCliente.Juridica, Margin = new Padding(16, 0, 0, 0) };
        _radioFisica.CheckedChanged += (_, _) => AtualizarTipoCliente();
        _radioJuridica.CheckedChanged += (_, _) => AtualizarTipoCliente();
        var painelTipo = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        painelTipo.Controls.Add(_radioFisica);
        painelTipo.Controls.Add(_radioJuridica);

        _campoCnpj = new TextBox { Text = Cliente.Cnpj };
        _campoCpf = new TextBox { Text = Cliente.Cpf };
        _campoRazaoSocial = new TextBox { Dock = DockStyle.Fill, Text = Cliente.RazaoSocial };
        _campoNomeFantasia = new TextBox { Dock = DockStyle.Fill, Text = Cliente.NomeFantasia };
        _campoRg = new TextBox { Text = Cliente.Rg };
        _campoIe = new TextBox { Text = Cliente.Ie };
        _campoInscricaoMunicipal = new TextBox { Dock = DockStyle.Fill, Text = Cliente.InscricaoMunicipal };

        _campoEstadoCivil = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, FlatStyle = FlatStyle.Flat, Text = Cliente.EstadoCivil };
        _campoEstadoCivil.Items.AddRange(new object[] { "Solteiro(a)", "Casado(a)", "Divorciado(a)", "Viúvo(a)", "União Estável" });

        _campoSexo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        _campoSexo.Items.AddRange(new object[] { "", "Masculino", "Feminino" });
        _campoSexo.SelectedIndex = Math.Max(0, _campoSexo.Items.IndexOf(Cliente.Sexo));

        _campoDataNascimento = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            ShowCheckBox = true,
            Checked = Cliente.DataNascimento.HasValue,
            Value = Cliente.DataNascimento ?? DateTime.Today,
        };

        _campoNacionalidade = new TextBox { Text = Cliente.Nacionalidade };
        _campoNaturalidade = new TextBox { Text = Cliente.Naturalidade };

        var tabelaDadosPessoais = NovaTabelaCampos();
        AdicionarLinha(tabelaDadosPessoais, "Código:", _campoCodigo);
        AdicionarLinha(tabelaDadosPessoais, "Tipo de cliente:", painelTipo);
        AdicionarLinha(tabelaDadosPessoais, "CNPJ / CPF:", CamposLadoALado((_campoCnpj, 1), (_campoCpf, 1)));
        AdicionarLinha(tabelaDadosPessoais, "Razão Social *:", _campoRazaoSocial);
        AdicionarLinha(tabelaDadosPessoais, "Nome fantasia:", _campoNomeFantasia);
        AdicionarLinha(tabelaDadosPessoais, "RG / Insc. Estadual:", CamposLadoALado((_campoRg, 1), (_campoIe, 1)));
        AdicionarLinha(tabelaDadosPessoais, "Insc. Municipal:", _campoInscricaoMunicipal);
        AdicionarLinha(tabelaDadosPessoais, "Estado civil / Sexo / Nasc.:",
            CamposLadoALado((_campoEstadoCivil, 2), (_campoSexo, 1), (_campoDataNascimento, 1)));
        AdicionarLinha(tabelaDadosPessoais, "Nacionalidade / Naturalidade:",
            CamposLadoALado((_campoNacionalidade, 1), (_campoNaturalidade, 1)));

        // --- Campos: Representante -------------------------------------------
        _campoRepresentante = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        CarregarRepresentantes();
        var tabelaRepresentante = NovaTabelaCampos();
        AdicionarLinha(tabelaRepresentante, "Representante:", _campoRepresentante);

        // --- Campos: Endereço --------------------------------------------------
        _campoUf = new TextBox { Text = Cliente.Uf, MaxLength = 2, CharacterCasing = CharacterCasing.Upper };
        _campoCidade = new TextBox { Text = Cliente.Cidade };
        _campoCep = new TextBox { Text = Cliente.Cep };
        _botaoBuscarCep = Estilo.BotaoPadrao("📍");
        _botaoBuscarCep.Click += async (_, _) => await BuscarCepAsync();
        _campoEndereco = new TextBox { Text = Cliente.Endereco };
        _campoNumero = new TextBox { Text = Cliente.Numero };
        _campoComplemento = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Complemento };
        _campoBairro = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Bairro };

        var tabelaEndereco = NovaTabelaCampos();
        AdicionarLinha(tabelaEndereco, "UF * / Município *:", CamposLadoALado((_campoUf, 1), (_campoCidade, 3)));
        AdicionarLinha(tabelaEndereco, "CEP *:", CriarLinhaCep());
        AdicionarLinha(tabelaEndereco, "Endereço / Número:", CamposLadoALado((_campoEndereco, 4), (_campoNumero, 1)));
        AdicionarLinha(tabelaEndereco, "Complemento:", _campoComplemento);
        AdicionarLinha(tabelaEndereco, "Bairro:", _campoBairro);

        // --- Campos: Contato -----------------------------------------------
        _campoTelefone = new TextBox { Text = Cliente.Telefone };
        _campoCelular = new TextBox { Text = Cliente.Celular };
        _campoSite = new TextBox { Text = Cliente.Site };
        _campoEmail1 = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Email1 };
        _campoEmail2 = new TextBox { Dock = DockStyle.Fill, Text = Cliente.Email2 };
        _campoContato = new TextBox { Text = Cliente.Contato };
        _campoContatoEmail = new TextBox { Text = Cliente.ContatoEmail };
        _campoContatoTelefone = new TextBox { Text = Cliente.ContatoTelefone };

        var tabelaContato = NovaTabelaCampos();
        AdicionarLinha(tabelaContato, "Telefone / Celular / Site:",
            CamposLadoALado((_campoTelefone, 1), (_campoCelular, 1), (_campoSite, 1)));
        AdicionarLinha(tabelaContato, "E-mail 1:", _campoEmail1);
        AdicionarLinha(tabelaContato, "E-mail 2:", _campoEmail2);
        AdicionarLinha(tabelaContato, "Contato:",
            CamposLadoALado((_campoContato, 1), (_campoContatoEmail, 2), (_campoContatoTelefone, 1)));

        // --- Monta as colunas ---------------------------------------------------
        var colunaEsquerda = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20, 12, 10, 12) };
        colunaEsquerda.Controls.Add(CriarSecao("Representante", tabelaRepresentante));
        colunaEsquerda.Controls.Add(CriarSecao("Dados pessoais", tabelaDadosPessoais));

        var colunaDireita = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(10, 12, 20, 12) };
        colunaDireita.Controls.Add(CriarSecao("Contato", tabelaContato));
        colunaDireita.Controls.Add(CriarSecao("Endereço", tabelaEndereco));

        var conteudo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        conteudo.Controls.Add(colunaEsquerda, 0, 0);
        conteudo.Controls.Add(colunaDireita, 1, 0);

        // --- Botões ---------------------------------------------------------
        _botaoSalvar = Estilo.BotaoPrimario("Gravar (F8)");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        _botaoSalvar.Click += (_, _) => Salvar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(20, 8, 20, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(_botaoSalvar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(conteudo);
        Controls.Add(painelBotoes);
        Controls.Add(cabecalho);

        AcceptButton = _botaoSalvar;
        CancelButton = botaoCancelar;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F8)
                Salvar();
        };

        AtualizarTipoCliente();
    }

    /// <summary>Habilita CPF ou CNPJ e os campos de pessoa física conforme o tipo escolhido.</summary>
    private void AtualizarTipoCliente()
    {
        var fisica = _radioFisica.Checked;
        _campoCpf.Enabled = fisica;
        _campoCnpj.Enabled = !fisica;

        foreach (var campo in new Control[]
        {
            _campoRg, _campoEstadoCivil, _campoSexo, _campoDataNascimento, _campoNacionalidade, _campoNaturalidade,
        })
        {
            campo.Enabled = fisica;
        }
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

    private static TableLayoutPanel NovaTabelaCampos() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        ColumnCount = 2,
        Margin = new Padding(0),
        ColumnStyles = { new ColumnStyle(SizeType.Absolute, 170), new ColumnStyle(SizeType.Percent, 100) },
    };

    private static void AdicionarLinha(TableLayoutPanel tabela, string rotulo, Control campo)
    {
        var linha = tabela.RowStyles.Count;
        tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        tabela.RowCount = tabela.RowStyles.Count;
        tabela.Controls.Add(new Label
        {
            Text = rotulo,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Estilo.CorTextoSuave,
        }, 0, linha);
        campo.Dock = campo.Dock == DockStyle.None ? DockStyle.Fill : campo.Dock;
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

    /// <summary>Título em destaque + separador + a tabela de campos da seção.</summary>
    private static Control CriarSecao(string titulo, TableLayoutPanel tabelaCampos)
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0, 0, 0, 24),
        };
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        container.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        container.Controls.Add(new Label
        {
            Text = titulo,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10.5f),
            ForeColor = Estilo.CorPrimaria,
            Margin = new Padding(0),
        }, 0, 0);
        container.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            Height = 1,
            BackColor = Estilo.CorBorda,
            Margin = new Padding(0, 4, 0, 0),
        }, 0, 1);
        container.Controls.Add(tabelaCampos, 0, 2);

        return container;
    }

    private Control CriarLinhaCep()
    {
        _campoCep.Dock = DockStyle.Fill;
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        painel.Controls.Add(_campoCep, 0, 0);
        _botaoBuscarCep.AutoSize = false;
        _botaoBuscarCep.MinimumSize = Size.Empty;
        _botaoBuscarCep.Size = new Size(34, 29);
        _botaoBuscarCep.Padding = new Padding(0);
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

        Cliente.Tipo = _radioFisica.Checked ? TipoCliente.Fisica : TipoCliente.Juridica;
        Cliente.Ativo = _campoAtivo.Checked;
        Cliente.Cnpj = _campoCnpj.Text.Trim();
        Cliente.Cpf = _campoCpf.Text.Trim();
        Cliente.RazaoSocial = _campoRazaoSocial.Text.Trim();
        Cliente.NomeFantasia = _campoNomeFantasia.Text.Trim();
        Cliente.Rg = _campoRg.Text.Trim();
        Cliente.Ie = _campoIe.Text.Trim();
        Cliente.InscricaoMunicipal = _campoInscricaoMunicipal.Text.Trim();
        Cliente.EstadoCivil = _campoEstadoCivil.Text.Trim();
        Cliente.Sexo = _campoSexo.SelectedIndex <= 0 ? "" : (string)_campoSexo.SelectedItem!;
        Cliente.DataNascimento = _campoDataNascimento.Checked ? _campoDataNascimento.Value.Date : null;
        Cliente.Nacionalidade = _campoNacionalidade.Text.Trim();
        Cliente.Naturalidade = _campoNaturalidade.Text.Trim();

        Cliente.Endereco = _campoEndereco.Text.Trim();
        Cliente.Numero = _campoNumero.Text.Trim();
        Cliente.Complemento = _campoComplemento.Text.Trim();
        Cliente.Cep = _campoCep.Text.Trim();
        Cliente.Uf = _campoUf.Text.Trim();
        Cliente.Cidade = _campoCidade.Text.Trim();
        Cliente.Bairro = _campoBairro.Text.Trim();

        Cliente.Telefone = _campoTelefone.Text.Trim();
        Cliente.Celular = _campoCelular.Text.Trim();
        Cliente.Site = _campoSite.Text.Trim();
        Cliente.Email1 = _campoEmail1.Text.Trim();
        Cliente.Email2 = _campoEmail2.Text.Trim();
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
