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
    public class RepositoryTime : RepositoryGenerics<Time>, IRepositoryTime
    {
        private readonly AppDbContext _context;

        public RepositoryTime(AppDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
