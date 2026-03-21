using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class OrganizadorCampeonato
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public Guid EnderecoId { get; set; }

        [JsonIgnore]
        public Usuario Usuario { get; set; }
        public Endereco Endereco { get; set; }
        public ContaBanco ContaBanco { get; set; }
        public ICollection<Campeonato> Campeonatos { get; set; }
    }
}
