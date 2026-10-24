using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class CriterioDesempateDTO
    {
        public Guid TimeId { get; set; }
        public int Pontos { get; set; }
        public int Vitorias { get; set; }
        public int SaldoGols { get; set; }
        public int GolsFeitos { get; set; }
    }
}
