using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Interfaces;

namespace kivoBackend.Application.Services
{
    public class PostService : ServiceGenerics<Post>, IPostService
    {
        private readonly IRepositoryGenerics<Post> _postRepository;

        public PostService(IRepositoryGenerics<Post> postRepository) : base(postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<IEnumerable<Post>> ListarAsync()
        {
            var posts = await _postRepository.ObterComIncludes(p => p.Autor);

            return posts
                .OrderByDescending(p => p.CriadoEm);
        }

        public async Task<Post?> ObterComDetalhesAsync(Guid id)
        {
            var posts = await _postRepository.ObterComIncludes(p => p.Autor);
            return posts.FirstOrDefault(p => p.Id == id);
        }

    }
}
