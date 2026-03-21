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

        public async Task<T> Adicionar(T entidade)
        {
            return await _repository.Adicionar(entidade);
        }

        public async Task Atualizar(T entidade)
        {
            await _repository.Atualizar(entidade);
        }

        public async Task<T?> ObterPorId(Guid id)
        {
            return await _repository.ObterPorId(id);
        }

        public async Task<IEnumerable<T>> ObterTodos()
        {
            return await _repository.ObterTodos();
        }

        public async Task Remover(Guid id)
        {
            await _repository.Remover(id);
        }
    }
}
