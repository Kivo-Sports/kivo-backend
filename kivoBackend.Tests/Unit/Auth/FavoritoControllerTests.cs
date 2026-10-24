using kivoBackend.Application.DTO;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Tests.Unit.Auth;

public class FavoritoControllerTests
{
    private static FavoritoController Build(Guid? userId, FakeFavoritoService service) => new(service)
    {
        ControllerContext = FakeHttp.ContextFor(userId)
    };

    [Fact]
    public async Task Listar_UsuarioAutenticado_RetornaOk()
    {
        var controller = Build(Guid.NewGuid(), new FakeFavoritoService());

        Assert.IsType<OkObjectResult>(await controller.Listar());
    }

    [Fact]
    public async Task Listar_SemToken_RetornaUnauthorized()
    {
        var controller = Build(null, new FakeFavoritoService());

        Assert.IsType<UnauthorizedResult>(await controller.Listar());
    }

    [Fact]
    public async Task Timeline_UsuarioAutenticado_RetornaOk()
    {
        var controller = Build(Guid.NewGuid(), new FakeFavoritoService());

        Assert.IsType<OkObjectResult>(await controller.Timeline());
    }

    [Fact]
    public async Task Timeline_SemToken_RetornaUnauthorized()
    {
        var controller = Build(null, new FakeFavoritoService());

        Assert.IsType<UnauthorizedResult>(await controller.Timeline());
    }

    [Fact]
    public async Task Adicionar_Valido_RetornaOkEChamaServico()
    {
        var userId = Guid.NewGuid();
        var service = new FakeFavoritoService();
        var controller = Build(userId, service);

        var result = await controller.Adicionar(new FavoritoRequestDTO { Tipo = EnumTipoFavorito.Time, ItemId = Guid.NewGuid() });

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(service.Adicionados);
        Assert.Equal(userId, service.Adicionados[0].UsuarioId);
    }

    [Fact]
    public async Task Adicionar_SemToken_RetornaUnauthorized()
    {
        var controller = Build(null, new FakeFavoritoService());

        Assert.IsType<UnauthorizedResult>(await controller.Adicionar(new FavoritoRequestDTO()));
    }

    [Fact]
    public async Task Adicionar_ItemInexistente_RetornaNotFound()
    {
        var service = new FakeFavoritoService { ThrowOnAdicionar = new KeyNotFoundException("Time não encontrado.") };
        var controller = Build(Guid.NewGuid(), service);

        Assert.IsType<NotFoundObjectResult>(await controller.Adicionar(new FavoritoRequestDTO { Tipo = EnumTipoFavorito.Time, ItemId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Adicionar_TipoInvalido_RetornaBadRequest()
    {
        var service = new FakeFavoritoService { ThrowOnAdicionar = new ArgumentException("Tipo inválido.") };
        var controller = Build(Guid.NewGuid(), service);

        Assert.IsType<BadRequestObjectResult>(await controller.Adicionar(new FavoritoRequestDTO()));
    }

    [Fact]
    public async Task Remover_Valido_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var service = new FakeFavoritoService();
        var controller = Build(userId, service);

        var result = await controller.Remover(new FavoritoRequestDTO { Tipo = EnumTipoFavorito.Time, ItemId = Guid.NewGuid() });

        Assert.IsType<OkObjectResult>(result);
        Assert.Single(service.Removidos);
    }

    [Fact]
    public async Task Remover_SemToken_RetornaUnauthorized()
    {
        var controller = Build(null, new FakeFavoritoService());

        Assert.IsType<UnauthorizedResult>(await controller.Remover(new FavoritoRequestDTO()));
    }
}
