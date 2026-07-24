using System.Windows.Controls;
using LD7Multitool.Modulos.Clientes;

namespace LD7Multitool.Wpf.Views;

public partial class ClientesView : UserControl
{
    private List<ClienteLinha> _todos = new();

    public ClientesView()
    {
        InitializeComponent();
        Carregar();
    }

    private void Carregar()
    {
        // Mesmo repositório/banco (dados.db) do WinForms — só a UI é WPF.
        _todos = ClienteRepository.Listar().Select(ClienteLinha.De).ToList();
        Aplicar("");
    }

    private void Busca_TextChanged(object sender, TextChangedEventArgs e) => Aplicar(campoBusca.Text);

    private void Aplicar(string termo)
    {
        termo = termo.Trim().ToUpperInvariant();
        IEnumerable<ClienteLinha> filtrados = _todos;
        if (termo.Length > 0)
            filtrados = _todos.Where(c => c.Busca.Contains(termo));
        grade.ItemsSource = filtrados.ToList();
    }

    /// <summary>Projeção só-leitura de um cliente para exibição na grade.</summary>
    public sealed class ClienteLinha
    {
        public string Codigo { get; init; } = "";
        public string RazaoSocial { get; init; } = "";
        public string NomeFantasia { get; init; } = "";
        public string Documento { get; init; } = "";
        public string CidadeUf { get; init; } = "";
        public string Representante { get; init; } = "";
        public string Ativo { get; init; } = "";
        public string Busca { get; init; } = "";

        public static ClienteLinha De(Cliente c)
        {
            var documento = c.Tipo == TipoCliente.Fisica ? Mascaras.Cpf(c.Cpf) : Mascaras.Cnpj(c.Cnpj);
            var cidadeUf =
                c.Cidade.Length > 0 && c.Uf.Length > 0 ? $"{c.Cidade}/{c.Uf}" :
                c.Cidade.Length > 0 ? c.Cidade : c.Uf;

            return new ClienteLinha
            {
                Codigo = c.Codigo,
                RazaoSocial = c.RazaoSocial,
                NomeFantasia = c.NomeFantasia,
                Documento = documento,
                CidadeUf = cidadeUf,
                Representante = c.RepresentanteNome,
                Ativo = c.Ativo ? "Sim" : "Não",
                Busca = $"{c.Codigo} {c.RazaoSocial} {c.NomeFantasia} {c.Cpf} {c.Cnpj}".ToUpperInvariant(),
            };
        }
    }
}
