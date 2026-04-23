using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ListarUsuarioDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Cpf { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;
        public DateTime DataNascimento { get; set; }
        public string Cargo { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public EnderecoDto? Endereco { get; set; }
        public ContaBancoDTO? ContaBanco { get; set; }
        public List<string>? TimesAdministrados { get; set; }
        public Guid? OrganizadorCampeonatoId { get; set; }
        public Guid? OrganizadorTimeId { get; set; }
    }
}
