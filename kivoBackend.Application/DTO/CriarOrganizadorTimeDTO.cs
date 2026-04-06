using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class CriarOrganizadorTimeDto : UsuarioDTO
    {
        public EnderecoDto Endereco { get; set; }
        // ContaBanco é aceito mas ignorado para organizador de time (apenas para campeonato)
        public ContaBancoDTO? ContaBanco { get; set; }
    }
}
