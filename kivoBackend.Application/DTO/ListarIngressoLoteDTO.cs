using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class ListarIngressoLoteDTO
    {
        public Guid Id { get; set; }    
        public Guid PartidaId { get; set; }
        public string NomeLote { get; set; }
        public decimal Preco { get; set; }
        public int QuantidadeTotal { get; set; }
        public int QuantidadeDisponivel { get; set; }
        public bool Ativo { get; set; }
    }
}
