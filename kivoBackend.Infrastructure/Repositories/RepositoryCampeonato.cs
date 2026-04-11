using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
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
    }
}
