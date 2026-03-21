using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class OrganizadorTime
    {
        public Guid Id { get; set; } = new Guid();
        public Guid UsuarioId { get; set; }
        public Guid EnderecoId { get; set; }
        [JsonIgnore]
        public Usuario Usuario { get; set; }
        public Endereco Endereco { get; set; }
        public ICollection<Time> Times { get; set; }
    }
}
