using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ResponderConviteDTO
    {
        public Guid OrganizadorTimeId { get; set; }
        public bool Aceito { get; set; }
    }
}
