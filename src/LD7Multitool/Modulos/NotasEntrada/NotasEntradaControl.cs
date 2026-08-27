using System.Diagnostics;
using System.Drawing.Drawing2D;
using LD7Multitool.Core;

namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Tela do módulo: à esquerda, uma galeria com as miniaturas das fotos ainda
/// não separadas; à direita, o painel para escolher a empresa (pasta) e a
/// data de emissão e mandar mover a seleção para o lugar certo.
///
/// A ordem de clique nas miniaturas vira a ordem das páginas — clicando
/// primeiro na folha 1, depois na folha 2 etc. — o selo numerado em cada
/// miniatura mostra essa ordem.
/// </summary>
public class NotasEntradaControl : UserControl
{
    private const int LadoMiniatura = 118;

    private readonly FlowLayoutPanel _galeria;
    private readonly TextBox _campoPesquisa;
    private readonly Label _resumo;

    private readonly ComboBox _campoEmpresa;
    private readonly DateTimePicker _campoData;
    private readonly Label _rotuloPreview;
    private readonly Label _rotuloContagem;
    private readonly ListBox _listaSelecionadas;
    private readonly Button _botaoSeparar;
    private readonly Button _botaoSubirPagina;
    private readonly Button _botaoDescerPagina;
    private readonly Button _botaoRemoverPagina;

    private readonly Dictionary<string, MiniaturaControl> _controles = new();
    private readonly List<string> _ordemSelecao = new();

    private CancellationTokenSource? _cancelamentoMiniaturas;

    public NotasEntradaControl()
    {
        Dock = DockStyle.Fill;
        Font = Estilo.FontePadrao;
        BackColor = Estilo.CorFundo;

        // --- Barra superior ----------------------------------------------------
        var barraSuperior = Estilo.CriarBarraSuperior();
        var botaoAtualizar = Estilo.BotaoPrimario("Atualizar");
        var botaoAbrirPasta = Estilo.BotaoPadrao("Abrir pasta \"para separar\"");
        var botaoConfiguracoes = Estilo.BotaoIcone("⚙", "Configurações (pasta raiz)");

        botaoAtualizar.Click += (_, _) => Recarregar();
        botaoAbrirPasta.Click += (_, _) => AbrirPastaParaSeparar();
        botaoConfiguracoes.Click += (_, _) => AbrirConfiguracoes();

        var fluxoAcoes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        fluxoAcoes.Controls.Add(botaoAtualizar);
        fluxoAcoes.Controls.Add(botaoAbrirPasta);

        var painelEngrenagem = new Panel { Dock = DockStyle.Right, Width = 40, Padding = new Padding(0, 2, 0, 2) };
        botaoConfiguracoes.Dock = DockStyle.Fill;
        painelEngrenagem.Controls.Add(botaoConfiguracoes);

        barraSuperior.Controls.Add(fluxoAcoes);
        barraSuperior.Controls.Add(painelEngrenagem);

        // --- Barra de pesquisa ---------------------------------------------------
        _campoPesquisa = new TextBox
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Filtrar pelas fotos pelo nome do arquivo...",
        };
        _campoPesquisa.TextChanged += (_, _) => AplicarFiltro();

        var barraFiltro = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(12, 6, 12, 6),
        };
        barraFiltro.Controls.Add(_campoPesquisa);

        // --- Galeria (esquerda) ---------------------------------------------------
        _galeria = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Estilo.CorFundo,
            Padding = new Padding(10),
        };

        _resumo = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            BackColor = Estilo.CorSuperficie,
            ForeColor = Estilo.CorTextoSuave,
        };

        var painelEsquerda = new Panel { Dock = DockStyle.Fill };
        painelEsquerda.Controls.Add(_galeria);
        painelEsquerda.Controls.Add(_resumo);
        painelEsquerda.Controls.Add(barraFiltro);

        // --- Painel direito: empresa, data, seleção e ação ------------------------
        (_campoEmpresa, _campoData, _rotuloPreview, _rotuloContagem, _listaSelecionadas, _botaoSeparar,
                _botaoSubirPagina, _botaoDescerPagina, _botaoRemoverPagina, var painelDireita) = MontarPainelDireito();
        AtualizarBotoesReordenar();

        // Fill primeiro, depois os docks de borda (convenção do projeto).
        Controls.Add(painelEsquerda);
        Controls.Add(painelDireita);
        Controls.Add(barraSuperior);

        Recarregar();
    }

    // --- Montagem do painel direito -------------------------------------------

    private (ComboBox Empresa, DateTimePicker Data, Label Preview, Label Contagem, ListBox Lista, Button Separar,
            Button Subir, Button Descer, Button Remover, Panel Painel)
        MontarPainelDireito()
    {
        var painel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 300,
            BackColor = Estilo.CorSuperficie,
            Padding = new Padding(14, 12, 14, 12),
        };
        const int larguraInterna = 272;

        var campoEmpresa = new ComboBox
        {
            Width = larguraInterna,
            DropDownStyle = ComboBoxStyle.DropDown,
            FlatStyle = FlatStyle.Flat,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.CustomSource,
        };
        campoEmpresa.TextChanged += (_, _) => AtualizarPreview();

        var campoData = new DateTimePicker
        {
            Width = larguraInterna,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
        };
        campoData.ValueChanged += (_, _) => AtualizarPreview();

        var rotuloPreview = new Label
        {
            Width = larguraInterna,
            Height = 48,
            AutoSize = false,
            ForeColor = Estilo.CorTextoSuave,
            Margin = new Padding(0, 6, 0, 12),
        };

        var rotuloContagem = new Label
        {
            Width = larguraInterna,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 9.75f),
            ForeColor = Estilo.CorPrimaria,
            Margin = new Padding(0, 0, 0, 6),
        };

        var botaoSubir = Estilo.BotaoPadrao("▲");
        var botaoDescer = Estilo.BotaoPadrao("▼");
        var botaoRemover = Estilo.BotaoPadrao("Remover");
        botaoSubir.Click += (_, _) => MoverSelecao(-1);
        botaoDescer.Click += (_, _) => MoverSelecao(1);
        botaoRemover.Click += (_, _) => RemoverDaSelecao();

        var fluxoReordenar = new FlowLayoutPanel { Width = larguraInterna, Height = 40, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
        fluxoReordenar.Controls.Add(botaoSubir);
        fluxoReordenar.Controls.Add(botaoDescer);
        fluxoReordenar.Controls.Add(botaoRemover);

        var topo = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        topo.Controls.Add(new Label
        {
            Text = "Separar seleção",
            Width = larguraInterna,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12f),
            Margin = new Padding(0, 0, 0, 12),
        });
        topo.Controls.Add(ComRotulo("Empresa (pasta)", campoEmpresa, larguraInterna));
        topo.Controls.Add(ComRotulo("Data de emissão", campoData, larguraInterna));
        topo.Controls.Add(rotuloPreview);
        topo.Controls.Add(rotuloContagem);
        topo.Controls.Add(fluxoReordenar);

        var lista = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        lista.SelectedIndexChanged += (_, _) => AtualizarBotoesReordenar();

        var botaoSeparar = Estilo.BotaoPrimario("Separar (mover)");
        var botaoLimpar = Estilo.BotaoPadrao("Limpar seleção");
        botaoSeparar.Click += (_, _) => Separar();
        botaoLimpar.Click += (_, _) => LimparSelecao();

        var rodape = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            ColumnCount = 1,
            RowCount = 2,
            Height = Estilo.AlturaBotao * 2 + 20,
        };
        rodape.RowStyles.Add(new RowStyle(SizeType.Absolute, Estilo.AlturaBotao));
        rodape.RowStyles.Add(new RowStyle(SizeType.Absolute, Estilo.AlturaBotao));
        botaoSeparar.Dock = DockStyle.Fill;
        botaoSeparar.Margin = new Padding(0, 0, 0, 8);
        botaoLimpar.Dock = DockStyle.Fill;
        botaoLimpar.Margin = new Padding(0);
        rodape.Controls.Add(botaoSeparar, 0, 0);
        rodape.Controls.Add(botaoLimpar, 0, 1);

        // Ordem de docking: Fill (lista) primeiro, depois Bottom, depois Top —
        // convenção já usada no resto do programa para não errar o empilhamento.
        painel.Controls.Add(lista);
        painel.Controls.Add(rodape);
        painel.Controls.Add(topo);

        return (campoEmpresa, campoData, rotuloPreview, rotuloContagem, lista, botaoSeparar,
            botaoSubir, botaoDescer, botaoRemover, painel);
    }

    private static Control ComRotulo(string rotulo, Control campo, int largura)
    {
        var painel = new TableLayoutPanel
        {
            Width = largura,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 12),
        };
        painel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        painel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        painel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        painel.Controls.Add(new Label
        {
            Text = rotulo,
            AutoSize = true,
            ForeColor = Estilo.CorTextoSuave,
            Margin = new Padding(0, 0, 0, 4),
        }, 0, 0);
        campo.Margin = new Padding(0);
        painel.Controls.Add(campo, 0, 1);
        return painel;
    }

    // --- Carregamento da galeria -----------------------------------------------

    private void Recarregar()
    {
        _cancelamentoMiniaturas?.Cancel();
        _controles.Clear();
        _ordemSelecao.Clear();
        _galeria.Controls.Clear();
        AtualizarPainelSelecao();

        var raiz = NotasEntradaConfigForm.PastaRaiz;
        if (raiz.Length == 0)
        {
            _resumo.Text = "Configure a pasta raiz no botão ⚙ para começar.";
            CarregarEmpresas(raiz);
            return;
        }
        if (!Directory.Exists(ServicoSeparacao.PastaParaSeparar(raiz)))
        {
            _resumo.Text = $"Não encontrei a pasta \"{ServicoSeparacao.PastaSepararNome}\" dentro da pasta raiz configurada.";
            CarregarEmpresas(raiz);
            return;
        }

        var pendentes = ServicoSeparacao.ListarPendentes(raiz);
        foreach (var caminho in pendentes)
        {
            var miniatura = new MiniaturaControl(caminho);
            miniatura.Alternado += (remetente, _) => AlternarSelecao((MiniaturaControl)remetente!);
            miniatura.ZoomSolicitado += (_, _) => VisualizadorImagemForm.Exibir(FindForm() ?? (IWin32Window)this, this, caminho);
            _controles[caminho] = miniatura;
            _galeria.Controls.Add(miniatura);
        }

        AplicarFiltro();
        _resumo.Text = $"{pendentes.Count} foto(s) pendente(s) de separação.";
        CarregarEmpresas(raiz);

        var cancelamento = new CancellationTokenSource();
        _cancelamentoMiniaturas = cancelamento;
        Task.Run(() => CarregarMiniaturasAsync(pendentes, cancelamento.Token), cancelamento.Token);
    }

    private void CarregarEmpresas(string raiz)
    {
        var empresas = ServicoSeparacao.ListarEmpresas(raiz);
        _campoEmpresa.AutoCompleteCustomSource.Clear();
        _campoEmpresa.AutoCompleteCustomSource.AddRange(empresas.ToArray());
        _campoEmpresa.Items.Clear();
        _campoEmpresa.Items.AddRange(empresas.ToArray());
    }

    private void CarregarMiniaturasAsync(List<string> arquivos, CancellationToken cancelamento)
    {
        foreach (var caminho in arquivos)
        {
            if (cancelamento.IsCancellationRequested)
                return;

            var miniatura = GerarMiniatura(caminho);

            if (cancelamento.IsCancellationRequested || IsDisposed)
            {
                miniatura?.Dispose();
                return;
            }

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (!cancelamento.IsCancellationRequested && _controles.TryGetValue(caminho, out var controle))
                        controle.DefinirImagem(miniatura);
                    else
                        miniatura?.Dispose();
                }));
            }
            catch (InvalidOperationException)
            {
                // Controle foi fechado/recarregado enquanto a miniatura carregava.
                miniatura?.Dispose();
                return;
            }
        }
    }

    /// <summary>Gera uma miniatura quadrada preservando a proporção; null se o arquivo não abrir.</summary>
    private static Bitmap? GerarMiniatura(string caminho)
    {
        try
        {
            using var original = Image.FromFile(caminho);
            var miniatura = new Bitmap(LadoMiniatura, LadoMiniatura);
            using var tela = Graphics.FromImage(miniatura);
            tela.InterpolationMode = InterpolationMode.HighQualityBicubic;
            tela.Clear(Estilo.CorFundo);

            var escala = Math.Min((float)LadoMiniatura / original.Width, (float)LadoMiniatura / original.Height);
            var largura = Math.Max(1, (int)(original.Width * escala));
            var altura = Math.Max(1, (int)(original.Height * escala));
            tela.DrawImage(original, (LadoMiniatura - largura) / 2, (LadoMiniatura - altura) / 2, largura, altura);
            return miniatura;
        }
        catch
        {
            return null;
        }
    }

    private void AplicarFiltro()
    {
        var termo = _campoPesquisa.Text.Trim();
        foreach (var (caminho, controle) in _controles)
            controle.Visible = termo.Length == 0 ||
                Path.GetFileName(caminho).Contains(termo, StringComparison.OrdinalIgnoreCase);
    }

    // --- Seleção -----------------------------------------------------------

    private void AlternarSelecao(MiniaturaControl controle)
    {
        if (!_ordemSelecao.Remove(controle.Caminho))
            _ordemSelecao.Add(controle.Caminho);

        foreach (var (caminho, item) in _controles)
        {
            var posicao = _ordemSelecao.IndexOf(caminho);
            item.AtualizarSelecao(posicao >= 0, posicao >= 0 ? posicao + 1 : null);
        }

        AtualizarPainelSelecao();
    }

    private void RemoverDaSelecao()
    {
        if (_listaSelecionadas.SelectedIndex < 0)
            return;
        var caminho = _ordemSelecao[_listaSelecionadas.SelectedIndex];
        _ordemSelecao.RemoveAt(_listaSelecionadas.SelectedIndex);
        if (_controles.TryGetValue(caminho, out var controle))
            controle.AtualizarSelecao(false, null);

        RenumerarBadges();
        AtualizarPainelSelecao();
    }

    private void MoverSelecao(int deslocamento)
    {
        var indice = _listaSelecionadas.SelectedIndex;
        var destino = indice + deslocamento;
        if (indice < 0 || destino < 0 || destino >= _ordemSelecao.Count)
            return;

        (_ordemSelecao[indice], _ordemSelecao[destino]) = (_ordemSelecao[destino], _ordemSelecao[indice]);
        RenumerarBadges();
        AtualizarPainelSelecao();
        _listaSelecionadas.SelectedIndex = destino;
    }

    private void LimparSelecao()
    {
        foreach (var caminho in _ordemSelecao)
            if (_controles.TryGetValue(caminho, out var controle))
                controle.AtualizarSelecao(false, null);
        _ordemSelecao.Clear();
        AtualizarPainelSelecao();
    }

    private void RenumerarBadges()
    {
        for (var i = 0; i < _ordemSelecao.Count; i++)
            if (_controles.TryGetValue(_ordemSelecao[i], out var controle))
                controle.AtualizarSelecao(true, i + 1);
    }

    private void AtualizarPainelSelecao()
    {
        var selecaoAnterior = _listaSelecionadas.SelectedIndex;
        _listaSelecionadas.Items.Clear();
        for (var i = 0; i < _ordemSelecao.Count; i++)
            _listaSelecionadas.Items.Add($"{i + 1:00} — {Path.GetFileName(_ordemSelecao[i])}");
        if (selecaoAnterior >= 0 && selecaoAnterior < _listaSelecionadas.Items.Count)
            _listaSelecionadas.SelectedIndex = selecaoAnterior;

        _rotuloContagem.Text = _ordemSelecao.Count == 0
            ? "Nenhuma foto selecionada"
            : $"{_ordemSelecao.Count} foto(s) selecionada(s)";
        _botaoSeparar.Enabled = _ordemSelecao.Count > 0;

        AtualizarBotoesReordenar();
        AtualizarPreview();
    }

    private void AtualizarBotoesReordenar()
    {
        var indice = _listaSelecionadas.SelectedIndex;
        _botaoSubirPagina.Enabled = indice > 0;
        _botaoDescerPagina.Enabled = indice >= 0 && indice < _listaSelecionadas.Items.Count - 1;
        _botaoRemoverPagina.Enabled = indice >= 0;
    }

    private void AtualizarPreview()
    {
        if (_ordemSelecao.Count == 0)
        {
            _rotuloPreview.Text = "";
            return;
        }

        var empresa = _campoEmpresa.Text.Trim();
        if (empresa.Length == 0)
        {
            _rotuloPreview.Text = "Informe a empresa (pasta) de destino.";
            return;
        }

        var nomeData = _campoData.Value.ToString("dd-MM-yyyy");
        _rotuloPreview.Text = _ordemSelecao.Count == 1
            ? $"→ {empresa}\\{_campoData.Value.Year}\\{nomeData}.jpg"
            : $"→ {empresa}\\{_campoData.Value.Year}\\{nomeData}\\ ({_ordemSelecao.Count} folhas)";
    }

    // --- Ação de separar --------------------------------------------------

    private void Separar()
    {
        var raiz = NotasEntradaConfigForm.PastaRaiz;
        if (raiz.Length == 0 || !Directory.Exists(raiz))
        {
            MessageBox.Show(this, "Configure primeiro a pasta raiz no botão de engrenagem (⚙).",
                "Pasta não configurada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            AbrirConfiguracoes();
            return;
        }
        if (_ordemSelecao.Count == 0)
            return;

        var digitado = _campoEmpresa.Text.Trim();
        if (digitado.Length == 0)
        {
            MessageBox.Show(this, "Informe a empresa (pasta) de destino.", "Empresa não informada",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _campoEmpresa.Focus();
            return;
        }

        var existentes = ServicoSeparacao.ListarEmpresas(raiz);
        var existente = existentes.FirstOrDefault(e => e.Equals(digitado, StringComparison.OrdinalIgnoreCase));
        string empresa;

        if (existente is not null)
        {
            empresa = existente;
        }
        else
        {
            empresa = ServicoSeparacao.NomeDePastaSeguro(digitado);
            if (empresa.Length == 0)
            {
                MessageBox.Show(this, "Nome de empresa inválido.", "Nome inválido",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resposta = MessageBox.Show(this,
                $"A pasta \"{empresa}\" ainda não existe. Criar agora?",
                "Nova empresa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (resposta != DialogResult.Yes)
                return;
        }

        UseWaitCursor = true;
        ServicoSeparacao.ResultadoSeparacao resultado;
        try
        {
            resultado = ServicoSeparacao.Separar(raiz, empresa, _campoData.Value.Date, _ordemSelecao.ToList());
        }
        finally
        {
            UseWaitCursor = false;
        }

        if (!resultado.Sucesso)
        {
            MessageBox.Show(this, resultado.Mensagem, "Não foi possível separar",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        foreach (var caminho in _ordemSelecao)
        {
            if (_controles.Remove(caminho, out var controle))
            {
                _galeria.Controls.Remove(controle);
                controle.Dispose();
            }
        }
        _ordemSelecao.Clear();
        AtualizarPainelSelecao();
        CarregarEmpresas(raiz);

        _resumo.Text = $"{resultado.Mensagem} → {resultado.CaminhoDestino}    |    " +
                       $"{_controles.Count} foto(s) pendente(s) de separação.";
    }

    // --- Utilidades ---------------------------------------------------------

    private static void AbrirPastaParaSeparar()
    {
        var raiz = NotasEntradaConfigForm.PastaRaiz;
        var pasta = ServicoSeparacao.PastaParaSeparar(raiz);
        if (raiz.Length == 0 || !Directory.Exists(pasta))
        {
            MessageBox.Show(
                "Configure a pasta raiz no botão ⚙ e confira se a pasta \"" +
                ServicoSeparacao.PastaSepararNome + "\" existe dentro dela.",
                "Pasta não encontrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        Process.Start(new ProcessStartInfo(pasta) { UseShellExecute = true });
    }

    private void AbrirConfiguracoes()
    {
        using var formulario = new NotasEntradaConfigForm();
        if (formulario.ShowDialog(this) == DialogResult.OK)
            Recarregar();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _cancelamentoMiniaturas?.Cancel();
        base.Dispose(disposing);
    }
}
