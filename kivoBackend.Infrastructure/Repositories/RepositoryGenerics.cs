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
    public class RepositoryGenerics<T> : IRepositoryGenerics<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _db;

        public RepositoryGenerics(AppDbContext context)
        {
            _context = context;
            _db = context.Set<T>();
        }

        public async Task<T> Add(T entity)
        {
            await _db.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _db.ToListAsync();
        }

        public async Task<T?> GetById(Guid id)
        {
            return await _db.FindAsync(id);
        }

        public async Task Remove(Guid id)
        {
            var entity = await GetById(id);

            if (entity == null)
            {
                throw new Exception($"Registro com id {id} não encontrado");
            }
            _db.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Update(T entity)
        {
            _db.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}
