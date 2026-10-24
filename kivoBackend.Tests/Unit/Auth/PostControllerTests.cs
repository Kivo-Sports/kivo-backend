using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Tests.Unit.Auth;

public class PostControllerTests
{
    private static (PostController Controller, InMemoryRepo<Post> Posts) Build(Guid? userId, bool isAdmin = false,
        Post[]? posts = null, Time[]? times = null, Campeonato[]? campeonatos = null)
    {
        var postRepo = new InMemoryRepo<Post>(posts ?? Array.Empty<Post>());
        var postService = new PostService(postRepo, new InMemoryRepo<Time>(times ?? Array.Empty<Time>()), new InMemoryRepo<Campeonato>(campeonatos ?? Array.Empty<Campeonato>()));
        var controller = new PostController(postService, new FakeStorageService(), new FakeCurrentUser(userId, isAdmin));
        return (controller, postRepo);
    }

    private static Post NovoPost(Guid autorId) => new() { Id = Guid.NewGuid(), AutorId = autorId, TipoAutorExibicao = EnumTipoAutorPost.Usuario, Autor = new Usuario { Nome = "Autor" }, Conteudo = "Ola" };

    [Fact]
    public async Task Listar_RetornaOk()
    {
        var (controller, _) = Build(null, posts: new[] { NovoPost(Guid.NewGuid()) });

        Assert.IsType<OkObjectResult>(await controller.Listar());
    }

    [Fact]
    public async Task ObterPorId_Existente_RetornaOk()
    {
        var post = NovoPost(Guid.NewGuid());
        var (controller, _) = Build(null, posts: new[] { post });

        Assert.IsType<OkObjectResult>(await controller.ObterPorId(post.Id));
    }

    [Fact]
    public async Task ObterPorId_Inexistente_RetornaNotFound()
    {
        var (controller, _) = Build(null);

        Assert.IsType<NotFoundObjectResult>(await controller.ObterPorId(Guid.NewGuid()));
    }

    [Fact]
    public async Task Criar_SemUserId_RetornaForbid()
    {
        var (controller, _) = Build(null);

        Assert.IsType<ForbidResult>(await controller.Criar(new CriarPostDto { Conteudo = "X" }, null));
    }

    [Fact]
    public async Task Criar_TituloMuitoLongo_RetornaBadRequest()
    {
        var (controller, _) = Build(Guid.NewGuid(), isAdmin: true);

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Usuario, Titulo = new string('a', 161), Conteudo = "X" };
        Assert.IsType<BadRequestObjectResult>(await controller.Criar(dto, null));
    }

    [Fact]
    public async Task Criar_SemConteudoNemImagem_RetornaBadRequest()
    {
        var (controller, _) = Build(Guid.NewGuid(), isAdmin: true);

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Usuario };
        Assert.IsType<BadRequestObjectResult>(await controller.Criar(dto, null));
    }

    [Fact]
    public async Task Criar_ComoUsuarioSemSerAdmin_RetornaBadRequest()
    {
        var (controller, _) = Build(Guid.NewGuid(), isAdmin: false);

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Usuario, Conteudo = "Ola" };
        Assert.IsType<BadRequestObjectResult>(await controller.Criar(dto, null));
    }

    [Fact]
    public async Task Criar_Admin_ComoUsuario_RetornaCreated()
    {
        var (controller, posts) = Build(Guid.NewGuid(), isAdmin: true);

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Usuario, Conteudo = "Ola mundo" };
        var result = Assert.IsType<CreatedAtActionResult>(await controller.Criar(dto, null));
        Assert.Equal(1, posts.Count);
    }

    [Fact]
    public async Task Criar_PorTimeQueOrganiza_RetornaCreated()
    {
        var userId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = userId } };
        var (controller, posts) = Build(userId, times: new[] { time });

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Time, EntidadeAutorId = time.Id, Conteudo = "Vamos time!" };
        Assert.IsType<CreatedAtActionResult>(await controller.Criar(dto, null));
        Assert.Equal(1, posts.Count);
    }

    [Fact]
    public async Task Criar_PorTimeQueNaoOrganiza_RetornaBadRequest()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = Guid.NewGuid() } };
        var (controller, _) = Build(Guid.NewGuid(), times: new[] { time });

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Time, EntidadeAutorId = time.Id, Conteudo = "X" };
        Assert.IsType<BadRequestObjectResult>(await controller.Criar(dto, null));
    }

    [Fact]
    public async Task Criar_TimeSemEntidadeSelecionada_RetornaBadRequest()
    {
        var (controller, _) = Build(Guid.NewGuid());

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Time, Conteudo = "X" };
        Assert.IsType<BadRequestObjectResult>(await controller.Criar(dto, null));
    }

    [Fact]
    public async Task Criar_PorCampeonatoQueOrganiza_RetornaCreated()
    {
        var userId = Guid.NewGuid();
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = userId };
        var (controller, posts) = Build(userId, campeonatos: new[] { campeonato });

        var dto = new CriarPostDto { TipoAutorExibicao = EnumTipoAutorPost.Campeonato, EntidadeAutorId = campeonato.Id, Conteudo = "Vem pra copa!" };
        Assert.IsType<CreatedAtActionResult>(await controller.Criar(dto, null));
        Assert.Equal(1, posts.Count);
    }

    [Fact]
    public async Task Editar_Inexistente_RetornaNotFound()
    {
        var (controller, _) = Build(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(await controller.Editar(Guid.NewGuid(), new EditarPostDto { Conteudo = "X" }, null));
    }

    [Fact]
    public async Task Editar_AutorDiferenteSemAdmin_RetornaForbid()
    {
        var post = NovoPost(Guid.NewGuid());
        var (controller, _) = Build(Guid.NewGuid(), posts: new[] { post });

        Assert.IsType<ForbidResult>(await controller.Editar(post.Id, new EditarPostDto { Conteudo = "X" }, null));
    }

    [Fact]
    public async Task Editar_Autor_AtualizaComSucesso()
    {
        var autorId = Guid.NewGuid();
        var post = NovoPost(autorId);
        var (controller, posts) = Build(autorId, posts: new[] { post });

        var result = Assert.IsType<OkObjectResult>(await controller.Editar(post.Id, new EditarPostDto { Conteudo = "Novo conteudo" }, null));
        Assert.Equal("Novo conteudo", (await posts.ObterPorId(post.Id))!.Conteudo);
    }

    [Fact]
    public async Task Editar_Admin_PodeEditarPostDeOutro()
    {
        var post = NovoPost(Guid.NewGuid());
        var (controller, _) = Build(Guid.NewGuid(), isAdmin: true, posts: new[] { post });

        Assert.IsType<OkObjectResult>(await controller.Editar(post.Id, new EditarPostDto { Conteudo = "Editado pelo admin" }, null));
    }

    [Fact]
    public async Task Editar_RemoverImagemSemNova_LimpaImagemUrl()
    {
        var autorId = Guid.NewGuid();
        var post = NovoPost(autorId);
        post.ImagemUrl = "http://img.test/x.png";
        var (controller, posts) = Build(autorId, posts: new[] { post });

        await controller.Editar(post.Id, new EditarPostDto { Conteudo = "X", RemoverImagem = true }, null);

        Assert.Null((await posts.ObterPorId(post.Id))!.ImagemUrl);
    }

    [Fact]
    public async Task Excluir_Inexistente_RetornaNotFound()
    {
        var (controller, _) = Build(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(await controller.Excluir(Guid.NewGuid()));
    }

    [Fact]
    public async Task Excluir_AutorDiferenteSemAdmin_RetornaForbid()
    {
        var post = NovoPost(Guid.NewGuid());
        var (controller, _) = Build(Guid.NewGuid(), posts: new[] { post });

        Assert.IsType<ForbidResult>(await controller.Excluir(post.Id));
    }

    [Fact]
    public async Task Excluir_Autor_RemoveComSucesso()
    {
        var autorId = Guid.NewGuid();
        var post = NovoPost(autorId);
        var (controller, posts) = Build(autorId, posts: new[] { post });

        Assert.IsType<NoContentResult>(await controller.Excluir(post.Id));
        Assert.Equal(0, posts.Count);
    }
}
