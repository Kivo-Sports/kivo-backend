using kivoBackend.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.DTO
{
    public class IngressoDetalhesDTO
    {
        public Guid Id { get; set; }
        public string NomeLote { get; set; } = string.Empty;
        public string NomePartida { get; set; } = string.Empty;
        public DateTime DataPartida { get; set; }
        public string LocalPartida { get; set; } = string.Empty;
        public decimal PrecoPago { get; set; }
        public DateTime DataCompra { get; set; }
        public EnumStatusIngresso Status { get; set; }
        public string CodigoValidacao { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
    }
}
