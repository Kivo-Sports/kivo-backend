using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Application.Interfaces
{
    public interface IServiceGenerics<T> where T : class
    {
        Task<IEnumerable<T>> ObterTodos();
        Task<T?> ObterPorId(Guid id);
        Task<T> Adicionar(T entidade);
        Task Atualizar(T entidade);
        Task Remover(Guid id);
    }
}
