using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

public class PostServiceTests
{
    private static PostService CriarServico(Post[]? posts = null, Time[]? times = null, Campeonato[]? campeonatos = null)
        => new(new InMemoryRepo<Post>(posts ?? Array.Empty<Post>()),
               new InMemoryRepo<Time>(times ?? Array.Empty<Time>()),
               new InMemoryRepo<Campeonato>(campeonatos ?? Array.Empty<Campeonato>()));

    private static Post NovoPost(DateTime criadoEm) => new()
    {
        Id = Guid.NewGuid(),
        AutorId = Guid.NewGuid(),
        TipoAutorExibicao = EnumTipoAutorPost.Usuario,
        Autor = new Usuario { Id = Guid.NewGuid(), Nome = "Autor" },
        Titulo = "T",
        Conteudo = "C",
        CriadoEm = criadoEm
    };

    [Fact]
    public async Task ListarAsync_OrdenaPorMaisRecentePrimeiro()
    {
        var antigo = NovoPost(DateTime.UtcNow.AddDays(-2));
        var recente = NovoPost(DateTime.UtcNow);
        var service = CriarServico(posts: new[] { antigo, recente });

        var resultado = (await service.ListarAsync()).ToList();

        Assert.Equal(recente.Id, resultado[0].Id);
        Assert.Equal(antigo.Id, resultado[1].Id);
    }

    [Fact]
    public async Task ObterComDetalhesAsync_Existente_Retorna()
    {
        var post = NovoPost(DateTime.UtcNow);
        var service = CriarServico(posts: new[] { post });

        var resultado = await service.ObterComDetalhesAsync(post.Id);

        Assert.NotNull(resultado);
        Assert.Equal(post.Id, resultado!.Id);
    }

    [Fact]
    public async Task ObterComDetalhesAsync_Inexistente_RetornaNull()
    {
        var service = CriarServico();

        Assert.Null(await service.ObterComDetalhesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UsuarioPossuiTimeAsync_DonoDoTime_RetornaTrue()
    {
        var usuarioId = Guid.NewGuid();
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = usuarioId } };
        var service = CriarServico(times: new[] { time });

        Assert.True(await service.UsuarioPossuiTimeAsync(usuarioId, time.Id));
    }

    [Fact]
    public async Task UsuarioPossuiTimeAsync_OutroUsuario_RetornaFalse()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP", OrganizadorTime = new OrganizadorTime { UsuarioId = Guid.NewGuid() } };
        var service = CriarServico(times: new[] { time });

        Assert.False(await service.UsuarioPossuiTimeAsync(Guid.NewGuid(), time.Id));
    }

    [Fact]
    public async Task UsuarioPossuiCampeonatoAsync_DonoDoCampeonato_RetornaTrue()
    {
        var usuarioId = Guid.NewGuid();
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = usuarioId };
        var service = CriarServico(campeonatos: new[] { campeonato });

        Assert.True(await service.UsuarioPossuiCampeonatoAsync(usuarioId, campeonato.Id));
    }

    [Fact]
    public async Task UsuarioPossuiCampeonatoAsync_OutroUsuario_RetornaFalse()
    {
        var campeonato = TestEntities.Campeonato(Guid.NewGuid());
        campeonato.OrganizadorCampeonato = new OrganizadorCampeonato { UsuarioId = Guid.NewGuid() };
        var service = CriarServico(campeonatos: new[] { campeonato });

        Assert.False(await service.UsuarioPossuiCampeonatoAsync(Guid.NewGuid(), campeonato.Id));
    }
}
