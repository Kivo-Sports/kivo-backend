using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class CampeonatoTime
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CampeonatoId { get; set; }
        public Guid TimeId { get; set; }

        public EnumStatusParticipacao EnumStatusParticipacao { get; set; }

        public DateTime ConvidadoEm { get; set; }
        public DateTime? RespondidoEm { get; set; }

        public Guid? RespondidoPorOrganizadorTimeId { get; set; }

        public Campeonato Campeonato { get; set; }
        public Time Time { get; set; }
    }
}
