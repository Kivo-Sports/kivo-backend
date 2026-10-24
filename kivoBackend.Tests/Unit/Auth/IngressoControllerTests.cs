using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests.Unit.Auth;

public class IngressoControllerTests
{
    private static IngressoController Build(FakeIngressoService? ingressos = null, FakeIngressoLoteService? lotes = null,
        FakeUsuarioService? usuarios = null, Guid? currentUserId = null, bool isAdmin = false)
    {
        var controller = new IngressoController(lotes ?? new FakeIngressoLoteService(), usuarios ?? new FakeUsuarioService(),
            new FakeCurrentUser(currentUserId, isAdmin), ingressos);
        controller.ControllerContext = FakeHttp.ContextFor(currentUserId);
        return controller;
    }

    [Fact]
    public async Task CriarLote_OrganizadorComPerfil_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var lotes = new FakeIngressoLoteService();
        var controller = Build(lotes: lotes, usuarios: usuarios, currentUserId: userId);

        Assert.IsType<OkObjectResult>(await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid() }));
        Assert.True(lotes.CreateCalled);
    }

    [Fact]
    public async Task CriarLote_SemPerfilOrganizador_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var controller = Build(usuarios: usuarios, currentUserId: userId);

        Assert.IsType<ForbidResult>(await controller.CriarLote(new CriarIngressoLoteDTO()));
    }

    [Fact]
    public async Task Comprar_UsuarioAutenticado_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var ingressos = new FakeIngressoService { CompraResult = new CompraIngressosResponseDTO { ValorTotal = 100 } };
        var controller = Build(ingressos: ingressos, currentUserId: userId);

        var result = Assert.IsType<OkObjectResult>(await controller.Comprar(new RealizarCompraDTO()));
        Assert.Equal(100, ((CompraIngressosResponseDTO)result.Value!).ValorTotal);
    }

    [Fact]
    public async Task Comprar_SemToken_RetornaUnauthorized()
    {
        var controller = Build(ingressos: new FakeIngressoService(), currentUserId: null);

        Assert.IsType<UnauthorizedObjectResult>(await controller.Comprar(new RealizarCompraDTO()));
    }

    [Fact]
    public async Task Comprar_ServicoNaoConfigurado_RetornaBadRequest()
    {
        var controller = Build(ingressos: null, currentUserId: Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.Comprar(new RealizarCompraDTO()));
    }

    [Fact]
    public async Task Comprar_ServicoLancaExcecao_RetornaBadRequest()
    {
        var ingressos = new FakeIngressoService { ThrowOnComprar = new InvalidOperationException("Lote esgotado.") };
        var controller = Build(ingressos: ingressos, currentUserId: Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.Comprar(new RealizarCompraDTO()));
    }

    [Fact]
    public async Task ObterLotesPorPartida_RetornaListaMapeada()
    {
        var lote = new IngressoLote { Id = Guid.NewGuid(), PartidaId = Guid.NewGuid(), NomeLote = "Pista", Preco = 10, QuantidadeTotal = 5, QuantidadeDisponivel = 5, Ativo = true };
        var lotes = new FakeIngressoLoteService { LotesResult = new[] { lote } };
        var controller = Build(lotes: lotes);

        var result = Assert.IsType<OkObjectResult>(await controller.ObterLotesPorPartida(lote.PartidaId));
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task ObterMeusIngressos_UsuarioAutenticado_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var ingressos = new FakeIngressoService { MeusIngressosResult = new List<IngressoDetalhesDTO> { new() { NomeLote = "Pista" } } };
        var controller = Build(ingressos: ingressos, currentUserId: userId);

        var result = Assert.IsType<OkObjectResult>(await controller.ObterMeusIngressos());
        Assert.Single((List<IngressoDetalhesDTO>)result.Value!);
    }

    [Fact]
    public async Task ObterMeusIngressos_SemToken_RetornaUnauthorized()
    {
        var controller = Build(ingressos: new FakeIngressoService(), currentUserId: null);

        Assert.IsType<UnauthorizedObjectResult>(await controller.ObterMeusIngressos());
    }

    [Fact]
    public async Task ValidarPortaria_CodigoValido_RetornaOk()
    {
        var controller = Build(ingressos: new FakeIngressoService(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.ValidarPortaria("ABC123"));
    }

    [Fact]
    public async Task ValidarPortaria_ServicoNaoConfigurado_RetornaBadRequest()
    {
        var controller = Build(ingressos: null, isAdmin: true);

        Assert.IsType<BadRequestObjectResult>(await controller.ValidarPortaria("XYZ"));
    }

    [Fact]
    public async Task ConfirmarPagamento_ServicoConfigurado_RetornaOk()
    {
        var controller = Build(ingressos: new FakeIngressoService(), currentUserId: Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.ConfirmarPagamento(Guid.NewGuid()));
    }

    [Fact]
    public async Task ConfirmarPagamento_ServicoNulo_RetornaBadRequest()
    {
        var controller = Build(ingressos: null, currentUserId: Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarPagamento(Guid.NewGuid()));
    }

    [Fact]
    public async Task AtribuirTitular_UsuarioAutenticado_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos: ingressos, currentUserId: userId);
        var ingressoId = Guid.NewGuid();

        var result = await controller.AtribuirTitular(ingressoId, new AtribuirTitularIngressoDTO { Nome = "Ana", Cpf = "12345678901" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userId, ingressos.LastAtribuirTitularCompradorId);
        Assert.Equal(ingressoId, ingressos.LastAtribuirTitularIngressoId);
    }

    [Fact]
    public async Task AtribuirTitular_SemToken_RetornaUnauthorized()
    {
        var controller = Build(ingressos: new FakeIngressoService(), currentUserId: null);

        Assert.IsType<UnauthorizedObjectResult>(await controller.AtribuirTitular(Guid.NewGuid(), new AtribuirTitularIngressoDTO { Nome = "A", Cpf = "12345678901" }));
    }
}
