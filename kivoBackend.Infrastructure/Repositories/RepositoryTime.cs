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
    public class RepositoryTime : RepositoryGenerics<Time>, IRepositoryTime
    {
        private readonly AppDbContext _context;

        public RepositoryTime(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public override async Task<IEnumerable<Time>> ObterTodos()
        {
            return await _context.Times
                .Include(t => t.Esporte)
                .ToListAsync();
        }

        public override async Task<Time?> ObterPorId(Guid id)
        {
            return await _context.Times
                .Include(t => t.Esporte)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}
