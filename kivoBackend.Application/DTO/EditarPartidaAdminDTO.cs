using kivoBackend.Core.Enums;
using System;

namespace kivoBackend.Application.DTO
{
    public class EditarPartidaAdminDTO
    {
        public Guid? TimeCasaId { get; set; }
        public Guid? TimeVisitanteId { get; set; }
        public DateTime? DataHora { get; set; }
        public string? Local { get; set; }
        public int? Rodada { get; set; }
        public EnumFaseMataMata Fase { get; set; } = EnumFaseMataMata.Nenhuma;
        public int NumeroJogoChave { get; set; }
    }
}
