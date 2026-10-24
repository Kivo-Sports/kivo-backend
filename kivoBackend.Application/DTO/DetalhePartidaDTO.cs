using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class DetalhePartidaDTO
    {
        public Guid Id { get; set; }
        public Guid CampeonatoId { get; set; }
        public int? Rodada { get; set; }
        public string Fase { get; set; }
        public string NomeTimeCasa { get; set; }
        public string LogoTimeCasa { get; set; }
        public string NomeTimeVisitante { get; set; }
        public string LogoTimeVisitante { get; set; }
        public int GolsTimeCasa { get; set; }
        public int GolsTimeVisitante { get; set; }
        public DateTime? DataHora { get; set; }
        public string Local { get; set; }
        public bool Finalizado { get; set; }
    }
}
