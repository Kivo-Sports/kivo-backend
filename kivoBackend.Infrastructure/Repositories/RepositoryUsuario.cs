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

        public async Task<Usuario?> ObterUsuarioPorCpf(string cpf)
        {
            return await _context.Usuarios
                .Include(u => u.Torcedor).ThenInclude(t => t.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Times)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.Endereco)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.ContaBanco)
                .FirstOrDefaultAsync(u => u.Cpf == cpf);
        }

        public async Task<Usuario?> ObterUsuarioPorEmail(string email)
        {
            return await _context.Usuarios
                .Include(u => u.Torcedor).ThenInclude(t => t.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Times)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.Endereco)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.ContaBanco)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Usuario?> ObterUsuarioPorId(Guid id)
        {
            return await _context.Usuarios
                .Include(u => u.Torcedor).ThenInclude(t => t.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Times)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.Endereco)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.ContaBanco)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<Usuario>> ObterTodosUsuarios()
        {
            return await _context.Usuarios
                .Where(u => u.Ativo)
                .Include(u => u.Torcedor).ThenInclude(t => t.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Endereco)
                .Include(u => u.OrganizadorTime).ThenInclude(ot => ot.Times)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.Endereco)
                .Include(u => u.OrganizadorCampeonato).ThenInclude(oc => oc.ContaBanco)
                .ToListAsync();
        }
    }
}
