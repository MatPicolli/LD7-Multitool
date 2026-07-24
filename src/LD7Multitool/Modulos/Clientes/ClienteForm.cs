using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>
/// Formulário de criação/edição de um cliente: layout em duas colunas com
/// caixas (GroupBox) por seção, alternância Física/Jurídica e busca de CEP.
/// </summary>
public class ClienteForm : Form
{
    private const int AlturaLinha = 30;

    private readonly RadioButton _radioFisica;
    private readonly RadioButton _radioJuridica;
    private readonly CheckBox _campoAtivo;

    private readonly TextBox _campoCodigo;
    private readonly TextBox _campoCnpj;
    private readonly TextBox _campoCpf;
    private readonly Label _botaoBuscarCnpj;
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
    private readonly Label _botaoBuscarCep;

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
    private bool _formatandoDocumento;

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
        MinimumSize = new Size(1000, 660);
        ClientSize = new Size(1000, 660);
        KeyPreview = true;

        // --- Campos ----------------------------------------------------------
        _campoCodigo = new TextBox { Text = Cliente.Codigo, ReadOnly = true, BackColor = Estilo.CorFundo };

        _radioFisica = new RadioButton { Text = "Física", AutoSize = true, Checked = Cliente.Tipo == TipoCliente.Fisica, Margin = new Padding(0, 4, 0, 0) };
        _radioJuridica = new RadioButton { Text = "Jurídica", AutoSize = true, Checked = Cliente.Tipo == TipoCliente.Juridica, Margin = new Padding(20, 4, 0, 0) };
        _radioFisica.CheckedChanged += (_, _) => AtualizarTipoCliente();
        _radioJuridica.CheckedChanged += (_, _) => AtualizarTipoCliente();
        var painelTipo = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), WrapContents = false };
        painelTipo.Controls.Add(_radioFisica);
        painelTipo.Controls.Add(_radioJuridica);

        _campoCnpj = new TextBox { Text = Mascaras.Cnpj(Cliente.Cnpj) };
        _campoCpf = new TextBox { Text = Mascaras.Cpf(Cliente.Cpf) };
        _campoCnpj.TextChanged += (_, _) => AplicarMascara(_campoCnpj, Mascaras.Cnpj);
        _campoCpf.TextChanged += (_, _) => AplicarMascara(_campoCpf, Mascaras.Cpf);
        _botaoBuscarCnpj = CriarLinkEmoji("🌐", "Buscar dados da empresa pelo CNPJ (Receita)");
        _botaoBuscarCnpj.Click += async (_, _) => await BuscarCnpjAsync();
        _campoRazaoSocial = new TextBox { Text = Cliente.RazaoSocial };
        _campoNomeFantasia = new TextBox { Text = Cliente.NomeFantasia };
        _campoRg = new TextBox { Text = Cliente.Rg };
        _campoIe = new TextBox { Text = Cliente.Ie };
        _campoInscricaoMunicipal = new TextBox { Text = Cliente.InscricaoMunicipal };

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
            MinimumSize = new Size(120, 0),
        };

        _campoNacionalidade = new TextBox { Text = Cliente.Nacionalidade };
        _campoNaturalidade = new TextBox { Text = Cliente.Naturalidade };

        _campoRepresentante = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
        CarregarRepresentantes();

        _campoUf = new TextBox { Text = Cliente.Uf, MaxLength = 2, CharacterCasing = CharacterCasing.Upper };
        _campoCidade = new TextBox { Text = Cliente.Cidade };
        _campoCep = new TextBox { Text = Cliente.Cep };
        _botaoBuscarCep = CriarLinkEmoji("🌐", "Buscar endereço pelo CEP na internet");
        _botaoBuscarCep.Click += async (_, _) => await BuscarCepAsync();
        _campoEndereco = new TextBox { Text = Cliente.Endereco };
        _campoNumero = new TextBox { Text = Cliente.Numero };
        _campoComplemento = new TextBox { Text = Cliente.Complemento };
        _campoBairro = new TextBox { Text = Cliente.Bairro };

        _campoTelefone = new TextBox { Text = Cliente.Telefone };
        _campoCelular = new TextBox { Text = Cliente.Celular };
        _campoSite = new TextBox { Text = Cliente.Site };
        _campoEmail1 = new TextBox { Text = Cliente.Email1 };
        _campoEmail2 = new TextBox { Text = Cliente.Email2 };
        _campoContato = new TextBox { Text = Cliente.Contato };
        _campoContatoEmail = new TextBox { Text = Cliente.ContatoEmail };
        _campoContatoTelefone = new TextBox { Text = Cliente.ContatoTelefone };

        // --- Grupos ----------------------------------------------------------
        var gbDados = CriarGrupo("Dados pessoais", new (string, Control)[]
        {
            ("Código", _campoCodigo),
            ("Tipo de cliente", painelTipo),
            ("CNPJ / CPF", CriarLinhaDocumento()),
            ("Razão Social *", _campoRazaoSocial),
            ("Nome fantasia", _campoNomeFantasia),
            ("RG / Insc. Estadual", CamposLadoALado((_campoRg, 1), (_campoIe, 1))),
            ("Insc. Municipal", _campoInscricaoMunicipal),
            ("Estado civil", _campoEstadoCivil),
            ("Sexo / Nascimento", CamposLadoALado((_campoSexo, 1), (_campoDataNascimento, 1))),
            ("Nacionalidade", _campoNacionalidade),
            ("Naturalidade", _campoNaturalidade),
        });
        var gbRepresentante = CriarGrupo("Representante", new (string, Control)[]
        {
            ("Representante", _campoRepresentante),
        });
        var gbEndereco = CriarGrupo("Endereço", new (string, Control)[]
        {
            ("UF * / Município *", CamposLadoALado((_campoUf, 1), (_campoCidade, 4))),
            ("CEP *", CriarLinhaCep()),
            ("Endereço / Nº", CamposLadoALado((_campoEndereco, 4), (_campoNumero, 1))),
            ("Complemento", _campoComplemento),
            ("Bairro", _campoBairro),
        });
        var gbContato = CriarGrupo("Contato", new (string, Control)[]
        {
            ("Telefone / Celular / Site", CamposLadoALado((_campoTelefone, 1), (_campoCelular, 1), (_campoSite, 1))),
            ("E-mail 1", _campoEmail1),
            ("E-mail 2", _campoEmail2),
            ("Contato", CamposLadoALado((_campoContato, 1), (_campoContatoEmail, 2), (_campoContatoTelefone, 1))),
        });

        var colunaEsquerda = CriarColuna(new Padding(16, 8, 8, 8), (gbDados, 11), (gbRepresentante, 1));
        var colunaDireita = CriarColuna(new Padding(8, 8, 16, 8), (gbEndereco, 5), (gbContato, 4));

        var conteudo = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        conteudo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        conteudo.Controls.Add(colunaEsquerda, 0, 0);
        conteudo.Controls.Add(colunaDireita, 1, 0);

        // --- Cabeçalho ------------------------------------------------------
        var cabecalho = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Estilo.CorFundo, Padding = new Padding(20, 12, 20, 0) };
        cabecalho.Controls.Add(new Label
        {
            Text = novo ? "Inserindo" : "Editando",
            AutoSize = true,
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI Semibold", 12f),
        });
        _campoAtivo = new CheckBox { Text = "Ativo", AutoSize = true, Checked = Cliente.Ativo, Dock = DockStyle.Right };
        cabecalho.Controls.Add(_campoAtivo);

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
        _botaoBuscarCnpj.Enabled = !fisica;

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

    /// <summary>Caixa (GroupBox) com título e uma tabela de linhas rótulo/campo.</summary>
    private static GroupBox CriarGrupo(string titulo, (string Rotulo, Control Campo)[] linhas)
    {
        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Font = Estilo.FontePadrao,
            ForeColor = Estilo.CorTexto,
        };
        // Coluna do rótulo em AutoSize: acomoda o texto inteiro em qualquer
        // DPI/fonte (evita cortes tipo "RG / Insc." no lugar de "RG / Insc.
        // Estadual" que aconteciam com largura fixa). MinimumSize garante uma
        // largura mínima para os campos ficarem alinhados entre as linhas.
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var i = 0; i < linhas.Length; i++)
        {
            tabela.RowStyles.Add(new RowStyle(SizeType.Absolute, AlturaLinha));
            tabela.Controls.Add(new Label
            {
                Text = linhas[i].Rotulo,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Estilo.CorTextoSuave,
                Margin = new Padding(0, 0, 12, 0),
                MinimumSize = new Size(132, 0),
            }, 0, i);

            var campo = linhas[i].Campo;
            campo.Dock = DockStyle.Fill;
            campo.Margin = new Padding(0, 3, 0, 3);
            tabela.Controls.Add(campo, 1, i);
        }
        tabela.RowCount = linhas.Length;

        var gb = new GroupBox
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            ForeColor = Estilo.CorPrimaria,
            Font = new Font("Segoe UI Semibold", 9.75f),
            Padding = new Padding(12, 6, 12, 10),
        };
        gb.Controls.Add(tabela);
        return gb;
    }

    /// <summary>Altura fixa de uma caixa com <paramref name="linhas"/> linhas.</summary>
    private static int AlturaGrupo(int linhas) => linhas * AlturaLinha + 48;

    /// <summary>Coluna com as caixas empilhadas (alturas fixas) e espaço entre elas.</summary>
    private static TableLayoutPanel CriarColuna(Padding padding, params (GroupBox Caixa, int Linhas)[] grupos)
    {
        var coluna = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoScroll = true,
            Padding = padding,
        };
        coluna.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var linha = 0;
        foreach (var (caixa, linhas) in grupos)
        {
            coluna.RowStyles.Add(new RowStyle(SizeType.Absolute, AlturaGrupo(linhas)));
            coluna.Controls.Add(caixa, 0, linha++);
            coluna.RowStyles.Add(new RowStyle(SizeType.Absolute, 14)); // espaço entre caixas
            linha++;
        }
        coluna.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // empurra tudo para cima
        coluna.RowCount = coluna.RowStyles.Count;
        return coluna;
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
            itens[i].Campo.Margin = new Padding(i == 0 ? 0 : 6, 0, 0, 0);
            painel.Controls.Add(itens[i].Campo, i, 0);
        }
        return painel;
    }

    /// <summary>Emoji clicável (sem cara de botão) para as buscas na internet.</summary>
    private static Label CriarLinkEmoji(string emoji, string dica)
    {
        var link = new Label
        {
            Text = emoji,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Font = new Font(Estilo.FontePadrao.FontFamily, 14f),
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        new ToolTip().SetToolTip(link, dica);
        return link;
    }

    /// <summary>Linha do documento: campo CNPJ + link de consulta + campo CPF.</summary>
    private Control CriarLinhaDocumento()
    {
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _campoCnpj.Dock = DockStyle.Fill;
        _campoCnpj.Margin = new Padding(0, 3, 0, 3);
        _botaoBuscarCnpj.Dock = DockStyle.Fill;
        _campoCpf.Dock = DockStyle.Fill;
        _campoCpf.Margin = new Padding(6, 3, 0, 3);

        painel.Controls.Add(_campoCnpj, 0, 0);
        painel.Controls.Add(_botaoBuscarCnpj, 1, 0);
        painel.Controls.Add(_campoCpf, 2, 0);
        return painel;
    }

    private Control CriarLinhaCep()
    {
        _campoCep.Dock = DockStyle.Fill;
        var painel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        painel.Controls.Add(_campoCep, 0, 0);
        _botaoBuscarCep.Dock = DockStyle.Fill;
        painel.Controls.Add(_botaoBuscarCep, 1, 0);
        return painel;
    }

    private static string SomenteDigitos(string texto) =>
        new(texto.Where(char.IsDigit).ToArray());

    /// <summary>Reaplica a máscara ao digitar, mantendo o cursor no fim.</summary>
    private void AplicarMascara(TextBox campo, Func<string, string> mascara)
    {
        if (_formatandoDocumento)
            return;
        _formatandoDocumento = true;
        var formatado = mascara(campo.Text);
        if (formatado != campo.Text)
        {
            campo.Text = formatado;
            campo.SelectionStart = campo.Text.Length;
        }
        _formatandoDocumento = false;
    }

    private async Task BuscarCnpjAsync()
    {
        var digitos = new string(_campoCnpj.Text.Where(char.IsDigit).ToArray());
        if (digitos.Length != 14)
        {
            MessageBox.Show(this, "Informe um CNPJ com 14 dígitos para consultar.", "Consultar CNPJ",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _botaoBuscarCnpj.Enabled = false;
        UseWaitCursor = true;
        IReadOnlyList<RespostaFonte> respostas;
        try
        {
            respostas = await ServicoCnpj.ConsultarAsync(digitos);
        }
        finally
        {
            _botaoBuscarCnpj.Enabled = _radioJuridica.Checked;
            UseWaitCursor = false;
        }

        var comSucesso = respostas.Where(r => r.Dados is not null).ToList();
        if (comSucesso.Count == 0)
        {
            var detalhes = string.Join("\n", respostas.Select(r => $"• {r.Fonte}: {r.Erro} ({r.TempoMs} ms)"));
            MessageBox.Show(this,
                "Nenhuma fonte retornou dados para esse CNPJ.\n\n" + detalhes,
                "Consultar CNPJ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var comparativo = new ComparativoCnpjForm(_campoCnpj.Text.Trim(), respostas);
        if (comparativo.ShowDialog(this) != DialogResult.OK || comparativo.DadosEscolhidos is null)
            return;

        PreencherComDadosCnpj(comparativo.DadosEscolhidos);
    }

    /// <summary>Joga os dados escolhidos no comparativo nos campos do cadastro.</summary>
    private void PreencherComDadosCnpj(DadosCnpj dados)
    {
        _campoRazaoSocial.Text = dados.RazaoSocial;
        _campoNomeFantasia.Text = dados.NomeFantasia;
        _campoEndereco.Text = dados.Logradouro.ToUpperInvariant();
        _campoNumero.Text = dados.Numero;
        _campoComplemento.Text = dados.Complemento.ToUpperInvariant();
        _campoBairro.Text = dados.Bairro.ToUpperInvariant();
        if (dados.Cep.Length > 0)
            _campoCep.Text = dados.Cep;
        _campoCidade.Text = dados.Municipio.ToUpperInvariant();
        _campoUf.Text = dados.Uf.ToUpperInvariant();
        if (dados.Telefone.Length > 0)
            _campoTelefone.Text = dados.Telefone;
        if (dados.Email.Length > 0)
            _campoEmail1.Text = dados.Email;
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
        Cliente.Cnpj = SomenteDigitos(_campoCnpj.Text);
        Cliente.Cpf = SomenteDigitos(_campoCpf.Text);

        // Avisa se o CPF/CNPJ já pertence a outro cadastro (ignora o próprio ao editar).
        var documento = Cliente.Tipo == TipoCliente.Fisica ? Cliente.Cpf : Cliente.Cnpj;
        if (documento.Length > 0)
        {
            var existente = ClienteRepository.BuscarPorDocumento(documento, Cliente.Id);
            if (existente is not null)
            {
                var rotulo = Cliente.Tipo == TipoCliente.Fisica ? "CPF" : "CNPJ";
                MessageBox.Show(this,
                    $"Já existe um cliente cadastrado com esse {rotulo}.\n\n" +
                    $"Código: {existente.Codigo}\n" +
                    $"Razão social: {existente.RazaoSocial}",
                    "Cadastro duplicado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

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
