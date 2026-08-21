using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Notificacao
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UsuarioId { get; set; }
        public Usuario Usuario { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? LinkRedirecionamento { get; set; }
        public EnumTipoNotificacao Tipo { get; set; }
        public bool Lida { get; set; } = false;
        public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    }
}
