using LD7Multitool.Core;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>
/// Importa clientes de um arquivo CSV no formato do MILITARISYS original (18
/// colunas, sem cabeçalho): código, razão social, nome fantasia, CPF/CNPJ, IE,
/// endereço, CEP, UF, cidade, bairro, e-mail(2), telefone(2), contato,
/// e-mail/telefone do contato e nome do representante.
/// </summary>
public class ImportarClientesForm : Form
{
    // Ordem das colunas exatamente como o MILITARISYS original gravava no CSV.
    private const int ColCodigo = 0, ColRazaoSocial = 1, ColNomeFantasia = 2, ColCpfCnpj = 3, ColIe = 4,
        ColEndereco = 5, ColCep = 6, ColUf = 7, ColCidade = 8, ColBairro = 9,
        ColEmail1 = 10, ColEmail2 = 11, ColTelefone1 = 12, ColTelefone2 = 13,
        ColContato = 14, ColContatoEmail = 15, ColContatoTelefone = 16, ColRepresentante = 17;
    private const int TotalColunas = 18;

    private sealed class Candidato
    {
        public required string[] Colunas { get; init; }
        public string Status { get; set; } = "";
        public bool Importavel { get; set; }
    }

    private readonly DataGridView _grade;
    private readonly Label _resumoTopo;
    private readonly Button _botaoImportar;
    private readonly List<Candidato> _candidatos = new();

    public int ImportadosCount { get; private set; }
    public int RepresentantesCriadosCount { get; private set; }
    public int PuladosCount { get; private set; }

    public ImportarClientesForm(string caminhoCsv)
    {
        Text = "Importar clientes de CSV";
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorSuperficie;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 520);
        ClientSize = new Size(820, 520);

        List<string[]> linhas;
        try
        {
            linhas = LeitorCsv.Ler(caminhoCsv);
        }
        catch (Exception ex)
        {
            linhas = new List<string[]>();
            MessageBox.Show(this, "Falha ao ler o arquivo:\n\n" + ex.Message, "Erro ao ler CSV",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // A primeira linha pode ser um cabeçalho (ex.: "codigo,razao_social,...").
        // Detecta pelo primeiro campo: um código de verdade é só dígitos.
        if (linhas.Count > 0 && linhas[0].Length > 0 &&
            linhas[0][ColCodigo].Trim().Length > 0 &&
            !linhas[0][ColCodigo].Trim().All(char.IsDigit))
        {
            linhas.RemoveAt(0);
        }

        MontarCandidatos(linhas);

        // --- Layout ----------------------------------------------------------
        _resumoTopo = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(12, 10, 12, 0),
        };
        AtualizarResumo();

        _grade = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            Margin = new Padding(12),
        };
        Estilo.EstilizarGrade(_grade);
        _grade.Columns.Add("status", "Status");
        _grade.Columns.Add("codigo", "Código");
        _grade.Columns.Add("razaoSocial", "Razão Social");
        _grade.Columns.Add("cpfCnpj", "CPF/CNPJ");
        _grade.Columns.Add("representante", "Representante");
        _grade.Columns["status"]!.FillWeight = 60;
        _grade.Columns["codigo"]!.FillWeight = 40;

        foreach (var candidato in _candidatos)
        {
            var indice = _grade.Rows.Add(
                candidato.Status,
                candidato.Colunas[ColCodigo],
                candidato.Colunas[ColRazaoSocial],
                candidato.Colunas[ColCpfCnpj],
                candidato.Colunas[ColRepresentante]);
            var linha = _grade.Rows[indice];
            linha.Tag = candidato;
            if (!candidato.Importavel)
                linha.DefaultCellStyle.ForeColor = Estilo.CorTextoSuave;
        }

        var painelGrade = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12) };
        painelGrade.Controls.Add(_grade);

        _botaoImportar = Estilo.BotaoPrimario($"Importar {_candidatos.Count(c => c.Importavel)} cliente(s)");
        var botaoCancelar = Estilo.BotaoPadrao("Cancelar");
        botaoCancelar.DialogResult = DialogResult.Cancel;
        _botaoImportar.Enabled = _candidatos.Any(c => c.Importavel);
        _botaoImportar.Click += (_, _) => Importar();

        var painelBotoes = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = Estilo.CorSuperficie,
        };
        painelBotoes.Controls.Add(_botaoImportar);
        painelBotoes.Controls.Add(botaoCancelar);

        Controls.Add(painelGrade);
        Controls.Add(painelBotoes);
        Controls.Add(_resumoTopo);
        CancelButton = botaoCancelar;
    }

    private void MontarCandidatos(List<string[]> linhas)
    {
        var codigosExistentes = new HashSet<string>(
            ClienteRepository.Listar().Select(c => c.Codigo), StringComparer.OrdinalIgnoreCase);

        foreach (var colunasBrutas in linhas)
        {
            var colunas = NormalizarColunas(colunasBrutas);
            var candidato = new Candidato { Colunas = colunas };

            var codigo = colunas[ColCodigo].Trim();
            var razaoSocial = colunas[ColRazaoSocial].Trim();

            if (razaoSocial.Length == 0)
            {
                candidato.Status = "Inválido (sem razão social) — pulado";
            }
            else if (codigo.Length > 0 && codigosExistentes.Contains(codigo))
            {
                candidato.Status = "Já existe (código duplicado) — pulado";
            }
            else
            {
                candidato.Status = codigo.Length == 0 ? "Novo (código será gerado)" : "Novo";
                candidato.Importavel = true;
                if (codigo.Length > 0)
                    codigosExistentes.Add(codigo); // evita duplicar dentro do próprio arquivo
            }

            _candidatos.Add(candidato);
        }
    }

    /// <summary>Garante exatamente 18 colunas, preenchendo com "" as que faltarem.</summary>
    private static string[] NormalizarColunas(string[] colunas)
    {
        if (colunas.Length == TotalColunas)
            return colunas;
        var normalizado = new string[TotalColunas];
        for (var i = 0; i < TotalColunas; i++)
            normalizado[i] = i < colunas.Length ? colunas[i] : "";
        return normalizado;
    }

    private void AtualizarResumo()
    {
        var novos = _candidatos.Count(c => c.Importavel);
        var pulados = _candidatos.Count - novos;
        _resumoTopo.Text = _candidatos.Count == 0
            ? "Nenhuma linha encontrada no arquivo."
            : $"{_candidatos.Count} linha(s) lida(s) — {novos} novo(s) para importar, {pulados} pulado(s).";
    }

    private void Importar()
    {
        var representantesPorNome = RepresentanteRepository.Listar()
            .ToDictionary(r => r.Nome, r => r, StringComparer.OrdinalIgnoreCase);

        foreach (var candidato in _candidatos.Where(c => c.Importavel))
        {
            var colunas = candidato.Colunas;

            var cliente = new Cliente
            {
                Codigo = colunas[ColCodigo].Trim().Length > 0
                    ? colunas[ColCodigo].Trim()
                    : ClienteRepository.GerarCodigoUnico(),
                RazaoSocial = colunas[ColRazaoSocial].Trim(),
                NomeFantasia = colunas[ColNomeFantasia].Trim(),
                CpfCnpj = colunas[ColCpfCnpj].Trim(),
                Ie = colunas[ColIe].Trim(),
                Endereco = colunas[ColEndereco].Trim(),
                Cep = colunas[ColCep].Trim(),
                Uf = colunas[ColUf].Trim(),
                Cidade = colunas[ColCidade].Trim(),
                Bairro = colunas[ColBairro].Trim(),
                Email1 = colunas[ColEmail1].Trim(),
                Email2 = colunas[ColEmail2].Trim(),
                Telefone1 = colunas[ColTelefone1].Trim(),
                Telefone2 = colunas[ColTelefone2].Trim(),
                Contato = colunas[ColContato].Trim(),
                ContatoEmail = colunas[ColContatoEmail].Trim(),
                ContatoTelefone = colunas[ColContatoTelefone].Trim(),
            };

            var nomeRepresentante = colunas[ColRepresentante].Trim();
            if (nomeRepresentante.Length > 0)
            {
                if (!representantesPorNome.TryGetValue(nomeRepresentante, out var representante))
                {
                    representante = new Representante { Nome = nomeRepresentante };
                    RepresentanteRepository.Inserir(representante);
                    representantesPorNome[nomeRepresentante] = representante;
                    RepresentantesCriadosCount++;
                }
                cliente.RepresentanteId = representante.Id;
            }

            ClienteRepository.Inserir(cliente);
            ImportadosCount++;
        }

        PuladosCount = _candidatos.Count - ImportadosCount;

        DialogResult = DialogResult.OK;
        Close();
    }
}
