using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Infrastructure.Repositories
{
    public class RepositoryUsuario : IRepositoryUsuario
    {
        private readonly AppDbContext _context;
        public RepositoryUsuario(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario> ObterUsuarioPorCpf(string cpf)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Cpf == cpf);
        }
    }
}
