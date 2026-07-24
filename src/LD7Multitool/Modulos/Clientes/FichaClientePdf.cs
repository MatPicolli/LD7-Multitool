using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>Gera a ficha cadastral de um cliente em PDF (layout simples, em lista).</summary>
public static class FichaClientePdf
{
    /// <summary>Gera o PDF em Fichas_Cadastrais (ao lado do executável) e retorna o caminho.</summary>
    public static string Gerar(Cliente cliente, Representante? representante)
    {
        var pasta = Path.Combine(AppContext.BaseDirectory, "Fichas_Cadastrais");
        Directory.CreateDirectory(pasta);
        var caminho = Path.Combine(pasta, LimparNomeArquivo($"{cliente.Codigo} - {cliente.RazaoSocial}") + ".pdf");

        using var documento = new PdfDocument();
        var pagina = documento.AddPage();
        pagina.Size = PdfSharpCore.PageSize.A4;
        using var gfx = XGraphics.FromPdfPage(pagina);

        var fTitulo = new XFont("Segoe UI", 16, XFontStyle.Bold);
        var fSecao = new XFont("Segoe UI", 12, XFontStyle.Bold);
        var fRotulo = new XFont("Segoe UI", 10, XFontStyle.Bold);
        var fTexto = new XFont("Segoe UI", 10, XFontStyle.Regular);
        var fRodape = new XFont("Segoe UI", 8, XFontStyle.Italic);

        const double x = 40;
        const double margem = 40;
        var largura = pagina.Width.Point - margem * 2;
        var y = margem;

        void Titulo(string texto)
        {
            gfx.DrawString(texto, fTitulo, XBrushes.Black, new XRect(x, y, largura, 26), XStringFormats.TopLeft);
            y += 30;
        }

        void Secao(string texto)
        {
            y += 8;
            gfx.DrawString(texto, fSecao, XBrushes.Black, new XRect(x, y, largura, 20), XStringFormats.TopLeft);
            y += 20;
            gfx.DrawLine(XPens.LightGray, x, y, x + largura, y);
            y += 12;
        }

        void Campo(string rotulo, string valor)
        {
            gfx.DrawString(rotulo + ":", fRotulo, XBrushes.Black, new XRect(x, y, 150, 18), XStringFormats.TopLeft);
            gfx.DrawString(string.IsNullOrWhiteSpace(valor) ? "-" : valor, fTexto, XBrushes.Black,
                new XRect(x + 150, y, largura - 150, 18), XStringFormats.TopLeft);
            y += 20;
        }

        Titulo($"Ficha Cadastral — {cliente.RazaoSocial}");
        gfx.DrawString(
            $"Código: {cliente.Codigo}   |   " +
            $"{(cliente.Tipo == TipoCliente.Fisica ? "Pessoa Física" : "Pessoa Jurídica")}   |   " +
            (cliente.Ativo ? "Ativo" : "Inativo"),
            fTexto, XBrushes.Gray, new XRect(x, y, largura, 16), XStringFormats.TopLeft);
        y += 24;

        Secao("DADOS DO CLIENTE");
        Campo("Razão Social", cliente.RazaoSocial);
        Campo("Nome Fantasia", cliente.NomeFantasia);
        if (cliente.Tipo == TipoCliente.Fisica)
        {
            Campo("CPF", cliente.Cpf);
            Campo("RG", cliente.Rg);
        }
        else
        {
            Campo("CNPJ", cliente.Cnpj);
        }
        Campo("Inscrição Estadual", cliente.Ie);
        Campo("Inscrição Municipal", cliente.InscricaoMunicipal);

        if (cliente.Tipo == TipoCliente.Fisica)
        {
            Secao("DADOS PESSOAIS");
            Campo("Estado civil", cliente.EstadoCivil);
            Campo("Sexo", cliente.Sexo);
            Campo("Data de nascimento", cliente.DataNascimento?.ToString("dd/MM/yyyy") ?? "");
            Campo("Nacionalidade", cliente.Nacionalidade);
            Campo("Naturalidade", cliente.Naturalidade);
        }

        Secao("ENDEREÇO");
        Campo("Endereço", Juntar(", ", cliente.Endereco, cliente.Numero));
        Campo("Complemento", cliente.Complemento);
        Campo("CEP", cliente.Cep);
        Campo("UF / Cidade / Bairro", Juntar(" / ", cliente.Uf, cliente.Cidade, cliente.Bairro));

        Secao("CONTATO");
        Campo("E-mail(s)", Juntar("   |   ", cliente.Email1, cliente.Email2));
        Campo("Telefone / Celular", Juntar("   |   ", cliente.Telefone, cliente.Celular));
        Campo("Site", cliente.Site);
        Campo("Contato", cliente.Contato);
        Campo("E-mail do contato", cliente.ContatoEmail);
        Campo("Telefone do contato", cliente.ContatoTelefone);

        Secao("REPRESENTANTE");
        if (representante is null)
        {
            Campo("Representante", "(nenhum)");
        }
        else
        {
            Campo("Nome", representante.Nome);
            Campo("E-mail", representante.Email);
            Campo("Telefone(s)", Juntar("   |   ", representante.Telefone1, representante.Telefone2));
        }

        y += 16;
        gfx.DrawString($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss} pelo LD7 Multitool",
            fRodape, XBrushes.Gray, new XRect(x, y, largura, 16), XStringFormats.TopLeft);

        documento.Save(caminho);
        return caminho;
    }

    private static string Juntar(string separador, params string[] valores) =>
        string.Join(separador, valores.Where(v => !string.IsNullOrWhiteSpace(v)));

    private static string LimparNomeArquivo(string nome)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            nome = nome.Replace(c, '_');
        return nome;
    }
}
