using kivoBackend.Application.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IIngressoService
    {
        Task <List<IngressoDetalhesDTO>> ComprarIngressosAsync(Guid usuarioId, RealizarCompraDTO compraDTO);
        Task <List<IngressoDetalhesDTO>> ObterMeusIngressosAsync(Guid usuarioId);
        Task<bool> ValidarIngressosNaPortariaAsync(string codigoValidacao);
        Task<bool> ConfirmarPagamentoAsync(Guid ingressoId);
        Task<bool> ProcessarWebhookAsaasAsync(string asaasPaymentId, string evento);
    }
}
