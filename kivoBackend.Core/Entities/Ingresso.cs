using kivoBackend.Core.Enums;
using System;

namespace kivoBackend.Core.Entities
{
    public class Ingresso
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid IngressoLoteId { get; set; }
        public virtual IngressoLote IngressoLote { get; set; } = null!;
        public Guid UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; } = null!;
        public decimal PrecoPago { get; set; }
        public DateTime DataCompra { get; set; } = DateTime.UtcNow;
        public EnumStatusIngresso StatusIngresso { get; set; } = EnumStatusIngresso.Pendente;
        public string CodigoValidacao { get; set; } = Guid.NewGuid().ToString("N").ToUpper();
        public DateTime? DataUso { get; set; }
        public string? AsaasPaymentId { get; set; }
    }
}