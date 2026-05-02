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
    public class RepositoryCampeonato : RepositoryGenerics<Campeonato>, IRepositoryCampeonato
    {
        private readonly AppDbContext _context;

        public RepositoryCampeonato(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes()
        {
            return await _context.Campeonatos
                .Include(c => c.CampeonatoTimes)
                .ThenInclude(ct => ct.Time)
                .ToListAsync();
        }

        public async Task<Campeonato> ObterCampeonatoPorId(Guid id)
        {
            return await _context.Campeonatos
                .Include(c => c.CampeonatoTimes)
                .ThenInclude(ct => ct.Time)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
