using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class CriarTorcedorDto : UsuarioDTO
    {
        public EnderecoDto Endereco { get; set; }
        public ContaBancoDTO? ContaBanco { get; set; }
    }
}
