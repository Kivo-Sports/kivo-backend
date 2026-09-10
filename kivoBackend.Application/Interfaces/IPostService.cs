using kivoBackend.Core.Entities;

namespace kivoBackend.Application.Interfaces
{
    public interface IPostService : IServiceGenerics<Post>
    {
        Task<IEnumerable<Post>> ListarAsync();
        Task<Post?> ObterComDetalhesAsync(Guid id);
    }
}
