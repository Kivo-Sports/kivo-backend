using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class RemoverTimeCampeonatoDTO
    {
        public Guid CampeonatoId { get; set; }
        public Guid TimeId { get; set; }
    }
}
