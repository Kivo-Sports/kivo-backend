using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class CriarOrganizadorCampeonatoDto : UsuarioDTO
    {
        public EnderecoDto Endereco { get; set; }
        public ContaBancoDto ContaBanco { get; set; }
    }
}
