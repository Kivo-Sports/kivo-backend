using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class GerarAgendaDTO
    {
        public Guid CampeonatoId { get; set; }
        public EnumFormatoCampeonato FormatoCampeonato { get; set; }
        public DateTime DataInicioPrimeiraRodada { get; set; }

    }
}
