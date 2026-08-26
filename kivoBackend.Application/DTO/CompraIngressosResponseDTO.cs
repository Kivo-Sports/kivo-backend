using System;
using System.Collections.Generic;

namespace kivoBackend.Application.DTO
{
    public class CompraIngressosResponseDTO
    {
        public string AsaasPaymentId { get; set; } = string.Empty;
        public decimal ValorTotal { get; set; }
        public string PixCopiaCola { get; set; } = string.Empty;
        public string QrCodeBase64 { get; set; } = string.Empty;
        public List<IngressoDetalhesDTO> Ingressos { get; set; } = new();
    }
}
