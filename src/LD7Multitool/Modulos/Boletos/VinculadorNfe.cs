namespace LD7Multitool.Modulos.Boletos;

/// <summary>Vincula boletos à NF-e de mesmo número (pelo topo do DANFE).</summary>
public static class VinculadorNfe
{
    /// <summary>
    /// Tenta preencher CaminhoNfe de um único boleto usando um índice já
    /// montado (número da NF-e → caminho). Não grava no banco.
    /// </summary>
    public static void Vincular(Boleto boleto, Dictionary<string, string> indiceNfe)
    {
        if (boleto.NfeReferente.Length == 0 || indiceNfe.Count == 0)
            return;
        var numero = LeitorNfePdf.Normalizar(boleto.NfeReferente);
        if (numero.Length > 0 && indiceNfe.TryGetValue(numero, out var caminho))
            boleto.CaminhoNfe = caminho;
    }

    /// <summary>
    /// Percorre a pasta de NF-e e vincula todos os boletos já cadastrados que
    /// ainda não têm NF-e. Grava as ligações encontradas no banco.
    /// Retorna quantos boletos foram vinculados.
    /// </summary>
    public static int VincularExistentes(string pastaNfe)
    {
        var indice = LeitorNfePdf.IndexarPasta(pastaNfe);
        if (indice.Count == 0)
            return 0;

        var vinculados = 0;
        foreach (var boleto in BoletoRepository.Listar())
        {
            if (boleto.CaminhoNfe.Length > 0 || boleto.NfeReferente.Length == 0)
                continue;

            var numero = LeitorNfePdf.Normalizar(boleto.NfeReferente);
            if (numero.Length > 0 && indice.TryGetValue(numero, out var caminho))
            {
                BoletoRepository.AtualizarCaminhoNfe(boleto.Id, caminho);
                vinculados++;
            }
        }
        return vinculados;
    }
}
