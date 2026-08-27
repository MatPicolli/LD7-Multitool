namespace LD7Multitool.Modulos.NotasEntrada;

/// <summary>
/// Lógica de organização das notas fiscais de entrada: lista o que está
/// pendente, lista as empresas já cadastradas (pastas) e move os arquivos
/// selecionados para o lugar certo, seguindo a convenção já usada pelo
/// usuário:
///
/// <c>RAIZ/Razão Social/aaaa/dd-MM-aaaa.jpg</c> — nota de uma folha só.
/// <c>RAIZ/Razão Social/aaaa/dd-MM-aaaa/01.jpg, 02.jpg, ...</c> — mais de uma.
///
/// Se já existir uma nota separada para a mesma empresa/data, os arquivos
/// novos entram como as próximas páginas (continuando a numeração) em vez de
/// travar — é comum uma folha aparecer depois das outras. Uma nota de folha
/// única que ganha uma segunda página é convertida em pasta automaticamente
/// (a antiga vira "01", preservando a extensão original).
/// </summary>
public static class ServicoSeparacao
{
    /// <summary>Nome fixo da pasta com as fotos ainda não separadas.</summary>
    public const string PastaSepararNome = "---------- PARA SEPARAR";

    public static readonly string[] ExtensoesAceitas = { ".jpg", ".jpeg" };

    public static string PastaParaSeparar(string raiz) => Path.Combine(raiz, PastaSepararNome);

    /// <summary>Fotos ainda não separadas, na pasta raiz configurada.</summary>
    public static List<string> ListarPendentes(string raiz)
    {
        var pasta = PastaParaSeparar(raiz);
        if (raiz.Length == 0 || !Directory.Exists(pasta))
            return new List<string>();

        return Directory.EnumerateFiles(pasta, "*.*", SearchOption.TopDirectoryOnly)
            .Where(arquivo => ExtensoesAceitas.Contains(Path.GetExtension(arquivo).ToLowerInvariant()))
            .OrderBy(arquivo => arquivo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Pastas de empresa já existentes na raiz (tudo, exceto a pasta "para separar").</summary>
    public static List<string> ListarEmpresas(string raiz)
    {
        if (raiz.Length == 0 || !Directory.Exists(raiz))
            return new List<string>();

        // Lambda em vez de grupo de método: Path.GetFileName tem uma sobrecarga
        // para ReadOnlySpan<char> que deixa o Select ambíguo (mesmo caso do
        // ContentOrderTextExtractor.GetText nos coletores de despesas).
        return Directory.EnumerateDirectories(raiz)
            .Select(caminho => Path.GetFileName(caminho))
            .Where(nome => nome is { Length: > 0 } && !nome.Equals(PastaSepararNome, StringComparison.OrdinalIgnoreCase))
            .Select(nome => nome!)
            .OrderBy(nome => nome, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Tira só os caracteres que o Windows não aceita num nome de pasta — mantém o resto exatamente como digitado.</summary>
    public static string NomeDePastaSeguro(string nome)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var seguro = string.Concat(nome.Select(c => invalidos.Contains(c) ? '_' : c)).Trim();
        // Windows não aceita pasta terminando em ponto ou espaço.
        return seguro.TrimEnd('.', ' ');
    }

    public sealed record ResultadoSeparacao(bool Sucesso, string Mensagem, string? CaminhoDestino);

    private static ResultadoSeparacao Falha(string mensagem) => new(false, mensagem, null);
    private static ResultadoSeparacao Sucesso(string mensagem, string caminho) => new(true, mensagem, caminho);

    /// <summary>
    /// Move os arquivos informados (na ordem em que devem virar páginas) para
    /// o destino da empresa/data. Não faz nada pela metade: se algum passo
    /// falhar, os arquivos já movidos voltam para o lugar original.
    /// </summary>
    public static ResultadoSeparacao Separar(
        string raiz, string empresa, DateTime emissao, IReadOnlyList<string> arquivosEmOrdem)
    {
        if (arquivosEmOrdem.Count == 0)
            return Falha("Nenhum arquivo selecionado.");

        var faltando = arquivosEmOrdem.FirstOrDefault(a => !File.Exists(a));
        if (faltando is not null)
            return Falha($"O arquivo não existe mais (foi movido ou apagado?):\n{faltando}");

        var pastaAno = Path.Combine(raiz, empresa, emissao.Year.ToString());
        var nomeData = emissao.ToString("dd-MM-yyyy");
        var pastaMultipla = Path.Combine(pastaAno, nomeData);

        var arquivoUnicoExistente = Directory.Exists(pastaAno)
            ? Directory.EnumerateFiles(pastaAno, nomeData + ".*").FirstOrDefault()
            : null;
        var pastaMultiplaExiste = Directory.Exists(pastaMultipla);

        if (arquivoUnicoExistente is not null && pastaMultiplaExiste)
        {
            return Falha(
                $"Já existe uma nota de folha única E uma pasta de várias folhas para {nomeData} " +
                $"nesta empresa — isso não deveria acontecer. Resolva manualmente pelo Explorer antes " +
                "de separar mais páginas desta data.");
        }

        // Monta a lista de (origem, destino) sem mover nada ainda — só depois
        // de decidir o cenário é que sabemos os nomes finais de cada arquivo.
        var planos = new List<(string Origem, string Destino)>();
        string caminhoFinal;

        if (pastaMultiplaExiste)
        {
            // Já é uma nota de várias folhas: as novas entram como as próximas páginas.
            var proximoIndice = Directory.GetFiles(pastaMultipla).Length + 1;
            foreach (var arquivo in arquivosEmOrdem)
                planos.Add((arquivo, Path.Combine(pastaMultipla, ProximoNome(proximoIndice++, arquivo))));
            caminhoFinal = pastaMultipla;
        }
        else if (arquivoUnicoExistente is not null)
        {
            // Já existe uma folha única para esta data: vira pasta — a antiga
            // some como "01" (mantendo a extensão dela) e as novas continuam.
            planos.Add((arquivoUnicoExistente, Path.Combine(pastaMultipla, ProximoNome(1, arquivoUnicoExistente))));
            var indice = 2;
            foreach (var arquivo in arquivosEmOrdem)
                planos.Add((arquivo, Path.Combine(pastaMultipla, ProximoNome(indice++, arquivo))));
            caminhoFinal = pastaMultipla;
        }
        else if (arquivosEmOrdem.Count == 1)
        {
            // Nada ainda para esta data: uma folha só vira arquivo direto.
            var ext = Path.GetExtension(arquivosEmOrdem[0]).ToLowerInvariant();
            var destino = Path.Combine(pastaAno, nomeData + ext);
            planos.Add((arquivosEmOrdem[0], destino));
            caminhoFinal = destino;
        }
        else
        {
            // Nada ainda e é mais de uma folha: já nasce como pasta.
            var indice = 1;
            foreach (var arquivo in arquivosEmOrdem)
                planos.Add((arquivo, Path.Combine(pastaMultipla, ProximoNome(indice++, arquivo))));
            caminhoFinal = pastaMultipla;
        }

        return ExecutarPlanos(planos, caminhoFinal);
    }

    private static string ProximoNome(int indice, string arquivoOrigem) =>
        indice.ToString("00") + Path.GetExtension(arquivoOrigem).ToLowerInvariant();

    /// <summary>Cria as pastas necessárias e move tudo; desfaz o que já tinha movido se algo falhar no meio.</summary>
    private static ResultadoSeparacao ExecutarPlanos(List<(string Origem, string Destino)> planos, string caminhoParaMostrar)
    {
        var movidos = new List<(string Origem, string Destino)>();
        try
        {
            foreach (var pasta in planos.Select(p => Path.GetDirectoryName(p.Destino)!).Distinct())
                Directory.CreateDirectory(pasta);

            foreach (var plano in planos)
            {
                if (File.Exists(plano.Destino))
                    throw new IOException($"Já existe um arquivo em \"{plano.Destino}\".");

                File.Move(plano.Origem, plano.Destino);
                movidos.Add(plano);
            }
        }
        catch (Exception ex)
        {
            // Desfaz na ordem inversa — melhor esforço: um erro ao devolver
            // não pode mascarar o motivo da falha original.
            for (var i = movidos.Count - 1; i >= 0; i--)
            {
                try { File.Move(movidos[i].Destino, movidos[i].Origem); }
                catch { /* o arquivo fica no destino parcial; o usuário precisa olhar */ }
            }
            return Falha($"Não foi possível separar: {ex.Message}\nNada foi alterado.");
        }

        var plural = planos.Count == 1 ? "folha" : $"{planos.Count} folhas";
        return Sucesso($"Nota separada ({plural}).", caminhoParaMostrar);
    }
}
