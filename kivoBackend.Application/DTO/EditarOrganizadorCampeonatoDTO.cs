using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class EditarOrganizadorCampeonatoDTO : EditarUsuarioDTO
    {
        public ContaBancoDTO? ContaBanco { get; set; }
    }
}
