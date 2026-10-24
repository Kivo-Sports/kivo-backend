using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Esporte
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Nome { get; set; }
        public string Icone { get; set; } // nome do ícone Iconify, ex: "mdi:basketball"

        public bool Ativo { get; set; } = true;
        public DateTime CriadoEm { get; set; }
    }
}
