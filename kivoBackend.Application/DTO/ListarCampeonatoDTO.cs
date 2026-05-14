using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ListarCampeonatoDto
    {
        public Guid Id { get; set; }
        public Guid OrganizadorCampeonatoId { get; set; }
        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; }
        public int TotalTimes { get; set; }
        public List<Guid> Times { get; set; }
        public DateTime CriadoEm { get; set; }
        public int PontosVitoria { get; set; }
        public int PontosDerrota { get; set; }
        public int PontosEmpate { get; set; }
        public int QuantidadeTimesClassificam { get; set; }
        public string FormatoCampeonato { get; set; }
        public List<Guid> TimeIds { get; set; } = new();
    }
}
