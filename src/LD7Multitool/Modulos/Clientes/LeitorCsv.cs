using System.Text;

namespace LD7Multitool.Modulos.Clientes;

/// <summary>
/// Leitor de CSV simples (RFC4180): campos separados por vírgula, opcionalmente
/// entre aspas (aspas duplicadas escapam uma aspa literal dentro do campo).
/// Suficiente para os arquivos gerados pelo MILITARISYS original (módulo csv do Python).
/// </summary>
public static class LeitorCsv
{
    public static List<string[]> Ler(string caminho)
    {
        var texto = File.ReadAllText(caminho, Encoding.UTF8);

        var linhas = new List<string[]>();
        var campos = new List<string>();
        var campoAtual = new StringBuilder();
        var entreAspas = false;

        void FinalizarCampo()
        {
            campos.Add(campoAtual.ToString());
            campoAtual.Clear();
        }

        void FinalizarLinha()
        {
            FinalizarCampo();
            linhas.Add(campos.ToArray());
            campos.Clear();
        }

        for (var i = 0; i < texto.Length; i++)
        {
            var c = texto[i];

            if (entreAspas)
            {
                if (c == '"')
                {
                    if (i + 1 < texto.Length && texto[i + 1] == '"')
                    {
                        campoAtual.Append('"');
                        i++; // pula a segunda aspa (escape)
                    }
                    else
                    {
                        entreAspas = false;
                    }
                }
                else
                {
                    campoAtual.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    entreAspas = true;
                    break;
                case ',':
                    FinalizarCampo();
                    break;
                case '\r':
                    break; // ignorado; a quebra real é tratada no \n
                case '\n':
                    FinalizarLinha();
                    break;
                default:
                    campoAtual.Append(c);
                    break;
            }
        }

        // Última linha, caso o arquivo não termine com quebra de linha.
        if (campoAtual.Length > 0 || campos.Count > 0)
            FinalizarLinha();

        // Descarta linhas completamente vazias (ex.: linha em branco no final do arquivo).
        return linhas.Where(l => l.Length > 1 || l[0].Length > 0).ToList();
    }
}
