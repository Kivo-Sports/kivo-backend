using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kivoBackend.Core.Interfaces
{
    public interface IRepositoryGenerics<T> where T : class
    {
        Task<IEnumerable<T>> GetAll();
        Task<T?> GetById(Guid id);
        Task<T> Add(T entity);
        Task Update(T entity);
        Task Remove(Guid id);
    }
}
