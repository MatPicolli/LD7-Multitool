using LD7Multitool.Core;

namespace LD7Multitool.Modulos.ConsultaFiscal;

/// <summary>
/// Tela do consultor: o cursor fica no campo da chave; ao bipar (44 dígitos),
/// consulta a SEFAZ e — se configurado — imprime o comprovante no verso,
/// sem precisar clicar em nada.
/// </summary>
public class ConsultaFiscalControl : UserControl
{
    private const string InstrucaoBipar = "Bipe (ou digite) a chave de acesso da nota:";
    private const string InstrucaoImprimir = "Insira a nota na impressora e pressione ENTER para imprimir";

    private readonly TextBox _campoChave;
    private readonly Label _instrucao;
    private readonly Label _situacao;
    private readonly TextBox _detalhes;
    private ResultadoConsulta? _ultimo;
    private NotaFiscalDetalhada? _ultimaNota;
    private ChaveAcesso? _ultimaChave;
    private bool _consultando;
    private bool _aguardandoImpressao;
    private DateTime _consultaFinalizadaEm;

    public ConsultaFiscalControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior (só engrenagem + reimprimir) --------------------
        var barra = Estilo.CriarBarraSuperior();
        var botaoReimprimir = Estilo.BotaoPadrao("Imprimir novamente");
        botaoReimprimir.Click += (_, _) => Reimprimir();
        var botaoConfig = Estilo.BotaoIcone("⚙", "Configurações (certificado, impressora)");
        botaoConfig.Click += (_, _) =>
        {
            using var f = new ConsultaFiscalConfigForm();
            f.ShowDialog(this);
            _campoChave.Focus();
        };
        var fluxo = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0) };
        fluxo.Controls.Add(botaoReimprimir);
        var painelEng = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfig.Dock = DockStyle.Fill;
        painelEng.Controls.Add(botaoConfig);
        barra.Controls.Add(fluxo);
        barra.Controls.Add(painelEng);

        // --- Área central ---------------------------------------------------
        _instrucao = new Label
        {
            Text = InstrucaoBipar,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12),
            ForeColor = Estilo.CorTexto,
        };

        _campoChave = new TextBox
        {
            Dock = DockStyle.Top,
            Font = new Font("Consolas", 16),
            TextAlign = HorizontalAlignment.Center,
            Margin = new Padding(40),
        };
        _campoChave.KeyDown += (sender, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            if (ChaveAcesso.TryParse(_campoChave.Text, out _))
            {
                TentarConsultar();           // Enter com chave completa: consulta
            }
            else if (_aguardandoImpressao &&
                     (DateTime.Now - _consultaFinalizadaEm).TotalMilliseconds > 1200)
            {
                // Ignora o ENTER que o próprio leitor manda logo após a chave;
                // só imprime num ENTER "tardio" (depois de inserir a nota).
                ImprimirPendente();
            }
        };
        // Leitores que não mandam Enter: dispara ao juntar 44 dígitos.
        _campoChave.TextChanged += (_, _) =>
        {
            var digitos = _campoChave.Text.Count(char.IsDigit);
            if (digitos >= 44)
                TentarConsultar();
        };

        _situacao = new Label
        {
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 14),
        };

        _detalhes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Estilo.CorSuperficie,
            Font = new Font("Consolas", 10),
        };

        var painelCentro = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40, 20, 40, 20) };
        painelCentro.Controls.Add(_detalhes);
        painelCentro.Controls.Add(_situacao);
        painelCentro.Controls.Add(_campoChave);
        painelCentro.Controls.Add(_instrucao);

        Controls.Add(painelCentro);
        Controls.Add(barra);

        // Mantém o foco no campo para o próximo "bipe".
        Load += (_, _) => _campoChave.Focus();
    }

    private async void TentarConsultar()
    {
        if (_consultando)
            return;
        if (!ChaveAcesso.TryParse(_campoChave.Text, out var chave))
        {
            // Ainda incompleta/ inválida: aguarda o restante da leitura.
            return;
        }

        _consultando = true;
        _aguardandoImpressao = false;
        _ultimaNota = null;
        _ultimaChave = chave;
        _instrucao.Text = InstrucaoBipar;
        _campoChave.Enabled = false;
        DefinirSituacao("Consultando...", Estilo.CorTextoSuave);
        _detalhes.Text = $"Chave: {chave.Formatada}\r\nTipo: {chave.TipoDescricao}   UF: {chave.UfSigla}";

        try
        {
            var config = ConsultaFiscalConfig.Carregar();
            var servico = new ConsultaFiscalService(config);
            var resultado = await servico.ConsultarAsync(chave);
            _ultimo = resultado;
            MostrarResultado(resultado);

            // Busca o conteúdo completo da nota (emit/dest/produtos) para o
            // layout detalhado, quando habilitado e for NF-e.
            if (resultado.Sucesso && config.BuscarDetalhes &&
                chave.Modelo is ModeloDocumento.NFe or ModeloDocumento.NFCe)
            {
                _ultimaNota = await new DistribuicaoDfeService(config).ObterNotaAsync(chave);
                if (_ultimaNota is not null)
                    _detalhes.Text += "\r\n\r\n✔ Nota completa carregada para impressão " +
                        $"({_ultimaNota.Produtos.Count} produto(s)).";
                else
                    _detalhes.Text += "\r\n\r\nNota completa indisponível — será impresso o " +
                        "comprovante simples (situação/protocolo).";
            }

            if (resultado.Sucesso)
            {
                if (config.ImprimirAutomaticamente)
                    Imprimir();
                else
                {
                    // Espera o usuário colocar a nota na impressora e apertar Enter.
                    _aguardandoImpressao = true;
                    _consultaFinalizadaEm = DateTime.Now;
                    _instrucao.Text = InstrucaoImprimir;
                }
            }
        }
        finally
        {
            _consultando = false;
            _campoChave.Enabled = true;
            _campoChave.Clear();
            _campoChave.Focus();
        }
    }

    private void ImprimirPendente()
    {
        if (_ultimo is not { Sucesso: true })
            return;
        _aguardandoImpressao = false;
        _instrucao.Text = InstrucaoBipar;
        Imprimir();
        _campoChave.Focus();
    }

    private void MostrarResultado(ResultadoConsulta r)
    {
        if (!r.Sucesso)
        {
            DefinirSituacao("Falha na consulta", Estilo.CorPerigo);
            _detalhes.Text += "\r\n\r\nErro: " + r.Erro;
            return;
        }

        DefinirSituacao(r.Resumo, r.Autorizada ? Color.FromArgb(30, 130, 76) : Estilo.CorPerigo);
        _detalhes.Text = string.Join("\r\n", new[]
        {
            $"Documento: {r.TipoDocumento}",
            $"Chave: {r.ChaveFormatada}",
            $"Situação: {r.XMotivo} ({r.CStat})",
            $"Protocolo: {(r.Protocolo.Length > 0 ? r.Protocolo : "-")}",
            $"Data/hora: {(r.DataHora.Length > 0 ? r.DataHora : "-")}",
            $"Ambiente: {r.Ambiente}",
        });
    }

    private void DefinirSituacao(string texto, Color cor)
    {
        _situacao.Text = texto;
        _situacao.ForeColor = cor;
    }

    private void Reimprimir()
    {
        if (_ultimo is { Sucesso: true })
            Imprimir();
        else
            MessageBox.Show(this, "Nenhuma consulta bem-sucedida para imprimir.", "Reimprimir",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>Imprime a nota completa se disponível; senão, o comprovante simples.</summary>
    private void Imprimir()
    {
        var impressora = ConsultaFiscalConfig.Carregar().Impressora;
        try
        {
            if (_ultimaNota is not null && _ultimaChave is not null)
                ImpressaoConsulta.ImprimirNota(_ultimaNota, _ultimaChave, impressora);
            else if (_ultimo is { Sucesso: true } r)
                ImpressaoConsulta.Imprimir(r, impressora);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Falha ao imprimir:\n\n" + ex.Message, "Impressão",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
