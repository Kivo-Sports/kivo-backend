using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ChaveamentoDTO
    {
        public string Fase { get; set; } 
        public List<TabelaClassificacaoMataMataDTO> Partidas { get; set; }
    }
}
