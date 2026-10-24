using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class TabelaClassificacaoDTO
    {
        public int Posicao { get; set; }
        public Guid TimeId { get; set; }
        public string NomeTime { get; set; }
        public string LogoUrl { get; set; }
        public int Pontos { get; set; }
        public int Jogos { get; set; }
        public int Vitorias { get; set; }
        public int Empates { get; set; }
        public int Derrotas { get; set; }
        public int GolsFeitos { get; set; }
        public int GolsSofridos { get; set; }
        public int SaldoGols { get; set; }
    }
}
