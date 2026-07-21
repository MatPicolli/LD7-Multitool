namespace LD7Multitool.Modulos.AutoEmail;

/// <summary>Localiza os PDFs mais recentes de um cliente nas pastas configuradas.</summary>
public static class LocalizadorArquivos
{
    /// <summary>
    /// Retorna o PDF criado mais recentemente na pasta cujo nome contém
    /// "Cliente-{codigo}" (ex.: "DANFE Cliente-2614 (21-07-2026).pdf"),
    /// ou null se a pasta não existir ou não houver arquivo do cliente.
    /// </summary>
    public static string? UltimoPdfDoCliente(string pasta, string codigo)
    {
        if (string.IsNullOrWhiteSpace(pasta) ||
            string.IsNullOrWhiteSpace(codigo) ||
            !Directory.Exists(pasta))
        {
            return null;
        }

        return Directory.EnumerateFiles(pasta, "*.pdf", SearchOption.TopDirectoryOnly)
            .Where(caminho => Path.GetFileName(caminho)
                .Contains($"Cliente-{codigo}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetCreationTime)
            .FirstOrDefault();
    }
}
