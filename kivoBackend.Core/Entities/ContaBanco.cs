using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class ContaBanco
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizadorCampeonatoId { get; set; }

        public string Banco { get; set; }
        public string Agencia { get; set; }
        public string Conta { get; set; }
        public string Tipo { get; set; }
        public string ChavePix { get; set; }

        public OrganizadorCampeonato OrganizadorCampeonato { get; set; }
    }
}
