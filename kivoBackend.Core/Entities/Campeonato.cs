using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Campeonato
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizadorCampeonatoId { get; set; }

        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }

        public int PontosVitoria { get; set; }
        public int PontosDerrota { get; set; }
        public int PontosEmpate { get; set; }

        public EnumStatusCampeonato EnumStatusCampeonato { get; set; }
        public DateTime CriadoEm { get; set; }

        public OrganizadorCampeonato OrganizadorCampeonato { get; set; }

        public ICollection<CampeonatoTime> CampeonatoTimes { get; set; }
    }
}
