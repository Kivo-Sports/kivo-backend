using kivoBackend.Core.Entities;

namespace kivoBackend.Application.Interfaces
{
    public interface IPostService : IServiceGenerics<Post>
    {
        Task<IEnumerable<Post>> ListarAsync();
        Task<Post?> ObterComDetalhesAsync(Guid id);
        Task<bool> UsuarioPossuiTimeAsync(Guid usuarioId, Guid timeId);
        Task<bool> UsuarioPossuiCampeonatoAsync(Guid usuarioId, Guid campeonatoId);
    }
}
