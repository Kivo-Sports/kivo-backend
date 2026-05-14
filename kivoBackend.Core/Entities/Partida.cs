using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Partida
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CampeonatoId { get; set; }

        public Guid? TimeCasaId { get; set; }
        public Time TimeCasa { get; set; }

        public Guid? TimeVisitanteId { get; set; }
        public Time TimeVisitante { get; set; }

        public int GolsTimeCasa { get; set; } = 0;
        public int GolsTimeVisitante { get; set; } = 0;

        public DateTime? DataHora { get; set; }
        public string Local { get; set; }
        public bool Finalizado { get; set; } = false;

        public int? Rodada { get; set; }

        public EnumFaseMataMata Fase { get; set; } = EnumFaseMataMata.Nenhuma;

        public int NumeroJogoChave { get; set; }
    }
}
