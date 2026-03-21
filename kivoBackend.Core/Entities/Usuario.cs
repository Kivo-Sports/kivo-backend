using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Usuario
    {
        public Guid Id { get; set; } = new Guid();
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Cpf { get; set; }
        public string Telefone { get; set; }
        public DateTime DataNascimento { get; set; }
        public EnumCargo EnumCargo { get; set; }
        public bool Ativo { get; set; }
        public DateTime CriadoEm { get; set; }

        [JsonIgnore]
        public Torcedor Torcedor { get; set; }
        [JsonIgnore]
        public OrganizadorTime OrganizadorTime { get; set; }
        [JsonIgnore]
        public OrganizadorCampeonato OrganizadorCampeonato { get; set; }
        [JsonIgnore]
        public Administrador Administrador { get; set; }

    }
}
