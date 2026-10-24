using kivoBackend.Core.Enums;
using System;

namespace kivoBackend.Application.DTO
{
    public class CriarPartidaManualDTO
    {
        public Guid CampeonatoId { get; set; }
        public Guid? TimeCasaId { get; set; }
        public Guid? TimeVisitanteId { get; set; }
        public int GolsTimeCasa { get; set; }
        public int GolsTimeVisitante { get; set; }
        public DateTime? DataHora { get; set; }
        public string? Local { get; set; }
        public bool Finalizado { get; set; }
        public int? Rodada { get; set; }
        public EnumFaseMataMata Fase { get; set; } = EnumFaseMataMata.Nenhuma;
        public int NumeroJogoChave { get; set; }
    }
}
