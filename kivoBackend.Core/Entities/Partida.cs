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

        // Relacionamentos
        public Guid? TimeCasaId { get; set; }
        public Time TimeCasa { get; set; }

        public Guid? TimeVisitanteId { get; set; }
        public Time TimeVisitante { get; set; }

        // Placar
        public int GolsTimeCasa { get; set; } = 0;
        public int GolsTimeVisitante { get; set; } = 0;

        // Metadados do Jogo
        public DateTime? DataHora { get; set; }
        public string Local { get; set; }
        public bool Finalizado { get; set; } = false;

        // Para Pontos Corridos
        public int? Rodada { get; set; }

        // Para Mata-Mata
        public EnumFaseMataMata Fase { get; set; } = EnumFaseMataMata.Nenhuma;

        // Identificador para o chaveamento (ex: Jogo 1 da Semi)
        public int NumeroJogoChave { get; set; }
    }
}
