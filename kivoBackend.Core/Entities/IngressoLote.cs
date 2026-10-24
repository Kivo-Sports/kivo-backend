using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class IngressoLote
    {
        public Guid Id { get; set; } = new Guid();
        public Guid PartidaId { get; set; }
        public string NomeLote { get; set; }
        public decimal Preco { get; set; }
        public int QuantidadeTotal { get; set; }
        public int QuantidadeDisponivel { get; set; }
        public bool Ativo { get; set; }
        public DateTime CriadoEm { get; set; } = DateTime.Now;
        public Partida Partida { get; set; }

    }
}
