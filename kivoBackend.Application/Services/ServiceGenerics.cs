using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Services
{
    public class ServiceGenerics<T> : IServiceGenerics<T> where T : class
    {
        private readonly IRepositoryGenerics<T> _repository;

        public ServiceGenerics(IRepositoryGenerics<T> repository)
        {
            _repository = repository;
        }

        public async Task<T> Add(T entity)
        {
            return await _repository.Add(entity);
        }

        public async Task<IEnumerable<T>> GetAll()
        {
            return await _repository.GetAll();
        }

        public async Task<T?> GetById(Guid id)
        {
            return await _repository.GetById(id);
        }

        public Task Remove(Guid id)
        {
            return _repository.Remove(id);
        }

        public async Task Update(T entity)
        {
            await _repository.Update(entity);
        }
    }
}
