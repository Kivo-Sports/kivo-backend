using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Tests.Unit.Auth;

public class NotificacaoControllerTests
{
    private static NotificacaoController Build(Guid userId, FakeNotificacaoService service) => new(service)
    {
        ControllerContext = FakeHttp.ContextFor(userId)
    };

    [Fact]
    public async Task ListarMinhasNotificacoes_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var service = new FakeNotificacaoService();
        var controller = Build(userId, service);

        var result = Assert.IsType<OkObjectResult>(await controller.ListarMinhasNotificacoes());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ObterNaoLidas_RetornaContador()
    {
        var userId = Guid.NewGuid();
        var service = new FakeNotificacaoService();
        var controller = Build(userId, service);

        var result = Assert.IsType<OkObjectResult>(await controller.ObterNaoLidas());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task MarcarLida_RetornaNoContent()
    {
        var controller = Build(Guid.NewGuid(), new FakeNotificacaoService());

        Assert.IsType<NoContentResult>(await controller.MarcarLida(Guid.NewGuid()));
    }

    [Fact]
    public async Task MarcarTodasLidas_RetornaNoContent()
    {
        var controller = Build(Guid.NewGuid(), new FakeNotificacaoService());

        Assert.IsType<NoContentResult>(await controller.MarcarTodasLidas());
    }
}
