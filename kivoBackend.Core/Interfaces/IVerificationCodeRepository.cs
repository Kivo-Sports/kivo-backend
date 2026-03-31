using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using System;
using System.Threading.Tasks;

namespace kivoBackend.Core.Interfaces
{
    public interface IVerificationCodeRepository : IRepositoryGenerics<VerificationCode>
    {
        /// <summary>
        /// Busca código válido (não expirado, não usado) do usuário por tipo
        /// Query eficiente direto no BD
        /// </summary>
        Task<VerificationCode?> ObterCodigoValidoAsync(Guid usuarioId, VerificationCodeType tipo);

        /// <summary>
        /// Busca código específico para validação
        /// </summary>
        Task<VerificationCode?> ObterCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo);
    }
}
