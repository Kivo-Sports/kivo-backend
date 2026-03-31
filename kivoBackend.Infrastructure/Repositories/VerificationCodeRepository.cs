using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace kivoBackend.Infrastructure.Repositories
{
    public class VerificationCodeRepository : RepositoryGenerics<VerificationCode>, IVerificationCodeRepository
    {
        private readonly AppDbContext _context;

        public VerificationCodeRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<VerificationCode?> ObterCodigoValidoAsync(Guid usuarioId, VerificationCodeType tipo)
        {
            return await _context.VerificationCodes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId
                    && c.Tipo == tipo
                    && !c.Usado
                    && c.ExpiraEm > DateTime.Now);
        }

        public async Task<VerificationCode?> ObterCodigoAsync(Guid usuarioId, string codigo, VerificationCodeType tipo)
        {
            return await _context.VerificationCodes
                .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId
                    && c.Codigo == codigo
                    && c.Tipo == tipo);
        }
    }
}
