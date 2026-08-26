using kivoBackend.Application.DTO.Asaas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IAsaasService
    {
        Task<string> ObterOuCriarClienteAsync(string nome, string cpf, string email);
        Task<AsaasCobrancaResponseDTO> CriarCobrancaPixAsync(string customerId, decimal valor, string descricao, string externalReference);
        Task<AsaasQRCodePixResponseDTO> ObterQrCodePixAsync(string paymentId);
        Task<string> ConsultarStatusCobrancaAsync(string paymentId);
    }
}
