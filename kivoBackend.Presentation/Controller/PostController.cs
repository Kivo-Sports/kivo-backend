using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Core.Entities;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Presentation.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostController : ControllerBase
    {
        private const long TamanhoMaximoImagem = 5 * 1024 * 1024;
        private readonly IPostService _postService;
        private readonly IStorageService _storageService;
        private readonly ICurrentUserService _currentUser;

        public PostController(
            IPostService postService,
            IStorageService storageService,
            ICurrentUserService currentUser)
        {
            _postService = postService;
            _storageService = storageService;
            _currentUser = currentUser;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Listar()
        {
            var posts = await _postService.ListarAsync();
            return Ok(posts.Select(MapearParaDto));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(Guid id)
        {
            var post = await _postService.ObterComDetalhesAsync(id);
            return post == null ? NotFound("Post não encontrado.") : Ok(MapearParaDto(post));
        }

        [HttpPost]
        [Authorize(Roles = "OrganizadorTime,OrganizadorCampeonato,Administrador")]
        public async Task<IActionResult> Criar([FromForm] CriarPostDto dto, IFormFile? imagem)
        {
            if (!_currentUser.UserId.HasValue)
                return Forbid();

            var erro = ValidarPost(dto.Titulo, dto.Conteudo, imagem, possuiImagemAtual: false);
            if (erro != null)
                return BadRequest(erro);

            string? imagemUrl = null;
            if (imagem != null)
            {
                using var stream = imagem.OpenReadStream();
                imagemUrl = await _storageService.UploadFileAsync(stream, imagem.FileName, imagem.ContentType, "posts");
            }

            var post = new Post
            {
                AutorId = _currentUser.UserId.Value,
                Titulo = LimparTexto(dto.Titulo),
                Conteudo = LimparTexto(dto.Conteudo),
                ImagemUrl = imagemUrl
            };

            var criado = await _postService.Adicionar(post);
            var resultado = await _postService.ObterComDetalhesAsync(criado.Id) ?? criado;
            return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, MapearParaDto(resultado));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Editar(Guid id, [FromForm] EditarPostDto dto, IFormFile? imagem)
        {
            var post = await _postService.ObterComDetalhesAsync(id);
            if (post == null)
                return NotFound("Post não encontrado.");

            if (!PodeAlterar(post))
                return Forbid();

            var manterImagemAtual = !dto.RemoverImagem && imagem == null && !string.IsNullOrWhiteSpace(post.ImagemUrl);
            var erro = ValidarPost(dto.Titulo, dto.Conteudo, imagem, manterImagemAtual);
            if (erro != null)
                return BadRequest(erro);

            if (imagem != null)
            {
                using var stream = imagem.OpenReadStream();
                post.ImagemUrl = await _storageService.UploadFileAsync(stream, imagem.FileName, imagem.ContentType, "posts");
            }
            else if (dto.RemoverImagem)
            {
                post.ImagemUrl = null;
            }

            post.Titulo = LimparTexto(dto.Titulo);
            post.Conteudo = LimparTexto(dto.Conteudo);
            post.AtualizadoEm = DateTime.UtcNow;
            await _postService.Atualizar(post);

            var resultado = await _postService.ObterComDetalhesAsync(id) ?? post;
            return Ok(MapearParaDto(resultado));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Excluir(Guid id)
        {
            var post = await _postService.ObterComDetalhesAsync(id);
            if (post == null)
                return NotFound("Post não encontrado.");

            if (!PodeAlterar(post))
                return Forbid();

            await _postService.Remover(id);
            return NoContent();
        }

        private bool PodeAlterar(Post post) =>
            _currentUser.IsAdmin || (_currentUser.UserId.HasValue && post.AutorId == _currentUser.UserId.Value);

        private static string? ValidarPost(
            string? titulo,
            string? conteudo,
            IFormFile? imagem,
            bool possuiImagemAtual)
        {
            if (titulo?.Trim().Length > 160)
                return "O título pode ter no máximo 160 caracteres.";

            if (conteudo?.Trim().Length > 5000)
                return "O conteúdo pode ter no máximo 5000 caracteres.";

            if (imagem != null)
            {
                if (imagem.Length == 0 || imagem.Length > TamanhoMaximoImagem)
                    return "A imagem deve ter entre 1 byte e 5 MB.";

                if (string.IsNullOrWhiteSpace(imagem.ContentType) || !imagem.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return "Envie um arquivo de imagem válido.";
            }

            if (string.IsNullOrWhiteSpace(conteudo) && imagem == null && !possuiImagemAtual)
                return "Informe um conteúdo ou envie uma imagem para o post.";

            return null;
        }

        private static string? LimparTexto(string? valor) =>
            string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        private static ListarPostDto MapearParaDto(Post post) => new()
        {
            Id = post.Id,
            AutorId = post.AutorId,
            AutorNome = post.Autor?.Nome ?? string.Empty,
            Titulo = post.Titulo,
            Conteudo = post.Conteudo,
            ImagemUrl = post.ImagemUrl,
            CriadoEm = post.CriadoEm,
            AtualizadoEm = post.AtualizadoEm
        };
    }
}
