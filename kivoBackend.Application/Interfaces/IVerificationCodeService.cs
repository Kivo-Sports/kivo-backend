using kivoBackend.Core.Enums;
using System;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IVerificationCodeService
    {
        /// <summary>
        /// Gera um código de verificação genérico
        /// </summary>
        Task<string> GerarCodigoAsync(Guid usuarioId, VerificationCodeType tipo, int duracao = 5);

        /// <summary>
        /// Valida um código
        /// </summary>
        Task<bool> ValidarCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo);

        /// <summary>
        /// Marca código como usado e o invalida
        /// </summary>
        Task MarcarComoUsadoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo);

        /// <summary>
        /// Obtém descrição amigável do tipo
        /// </summary>
        string ObterDescricaoTipo(VerificationCodeType tipo);
    }
}
