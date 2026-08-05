using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Entities
{
    public class Ingresso
    {
        public Guid Id { get; set; }
        public Guid IngressoLoteId { get; set; }
        public virtual IngressoLote IngressoLote { get; set; }
        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }
        public decimal PrecoPago { get; set; }
        public DateTime DataCompra { get; set; } = DateTime.UtcNow;
        public EnumStatusIngresso StatusIngresso { get; set; } = EnumStatusIngresso.Pendente;
        public string CodigoValidacao { get; set; } = Guid.NewGuid().ToString("N").ToUpper();
        public DateTime DataUso { get; set; }

    }
}
