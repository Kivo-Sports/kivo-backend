using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Administrador
    {
       public Guid Id { get; set; } = Guid.NewGuid();
       public Guid UsuarioId { get; set; }
        [JsonIgnore]
       public Usuario Usuario { get; set; }
    }
}
