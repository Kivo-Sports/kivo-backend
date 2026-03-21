using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class RecuperacaoSenha
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }

        public string Token { get; set; }
        public bool Usado { get; set; }

        public DateTime ExpiraEm { get; set; }
        public DateTime CriadoEm { get; set; }

        public Usuario Usuario { get; set; }
    }
}
