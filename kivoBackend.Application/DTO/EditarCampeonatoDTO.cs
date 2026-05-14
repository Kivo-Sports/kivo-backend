using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class EditarCampeonatoDto
    {
        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int PontosVitoria { get; set; }
        public int PontosDerrota { get; set; }
        public int PontosEmpate { get; set; }
        public EnumFormatoCampeonato FormatoCampeonato{ get; set; }
        public int? QuantidadeTimesClassificam { get; set; }
    }
}
