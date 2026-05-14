using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class TabelaClassificacaoMataMataDTO
    {
        public Guid Id { get; set; }
        public int NumeroJogoChave { get; set; }
        public string TimeCasa { get; set; }
        public string TimeVisitante { get; set; }
        public int GolsCasa { get; set; }
        public int GolsVisitante { get; set; }
        public bool Finalizado { get; set; }
    }
}
