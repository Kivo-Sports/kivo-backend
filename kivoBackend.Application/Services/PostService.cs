using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;

namespace kivoBackend.Application.Services
{
    public class PostService : ServiceGenerics<Post>, IPostService
    {
        private readonly IRepositoryGenerics<Post> _postRepository;
        private readonly IRepositoryGenerics<Time> _timeRepository;
        private readonly IRepositoryGenerics<Campeonato> _campeonatoRepository;

        public PostService(
            IRepositoryGenerics<Post> postRepository,
            IRepositoryGenerics<Time> timeRepository,
            IRepositoryGenerics<Campeonato> campeonatoRepository) : base(postRepository)
        {
            _postRepository = postRepository;
            _timeRepository = timeRepository;
            _campeonatoRepository = campeonatoRepository;
        }

        public async Task<IEnumerable<Post>> ListarAsync()
        {
            var posts = await _postRepository.ObterComIncludes(p => p.Autor, p => p.TimeAutor!, p => p.CampeonatoAutor!);

            return posts
                .OrderByDescending(p => p.CriadoEm);
        }

        public async Task<Post?> ObterComDetalhesAsync(Guid id)
        {
            var posts = await _postRepository.ObterComIncludes(p => p.Autor, p => p.TimeAutor!, p => p.CampeonatoAutor!);
            return posts.FirstOrDefault(p => p.Id == id);
        }

        public async Task<bool> UsuarioPossuiTimeAsync(Guid usuarioId, Guid timeId)
        {
            var times = await _timeRepository.ObterComIncludes(t => t.OrganizadorTime);
            return times.Any(t => t.Id == timeId && t.OrganizadorTime?.UsuarioId == usuarioId);
        }

        public async Task<bool> UsuarioPossuiCampeonatoAsync(Guid usuarioId, Guid campeonatoId)
        {
            var campeonatos = await _campeonatoRepository.ObterComIncludes(c => c.OrganizadorCampeonato);
            return campeonatos.Any(c => c.Id == campeonatoId && c.OrganizadorCampeonato?.UsuarioId == usuarioId);
        }
    }
}
