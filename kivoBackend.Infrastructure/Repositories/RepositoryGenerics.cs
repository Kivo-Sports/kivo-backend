using kivoBackend.Core.Interfaces;
using kivoBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Infrastructure.Repositories
{
    public class RepositoryGenerics<T> : IRepositoryGenerics<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _db;

        public RepositoryGenerics(AppDbContext context)
        {
            _context = context;
            _db = context.Set<T>();
        }

        public async Task<T> Adicionar(T entidade)
        {
            await _db.AddAsync(entidade);
            await _context.SaveChangesAsync();
            return entidade;
        }

        public async Task<IEnumerable<T>> ObterTodos()
        {
            return await _db.ToListAsync();
        }

        public async Task<T?> ObterPorId(Guid id)
        {
            return await _db.FindAsync(id);
        }

        public async Task Remover(Guid id)
        {
            var entidade = await ObterPorId(id);

            if (entidade == null)
            {
                throw new Exception($"Registro com id {id} não encontrado");
            }
            _db.Remove(entidade);
            await _context.SaveChangesAsync();
        }

        public async Task Atualizar(T entidade)
        {
            _db.Update(entidade);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> ObterTodosComIncludes(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _db;

            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }

            return await query.ToListAsync();
        }
    }
}
