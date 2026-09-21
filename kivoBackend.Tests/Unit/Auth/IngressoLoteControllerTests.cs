using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests.Unit.Auth;

public class IngressoLoteControllerTests
{
    [Fact]
    public async Task CriarLote_OrganizadorComPerfil_Cria()
    {
        var userId = Guid.NewGuid();
        var orgCampId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgCampId);
        var lotes = new FakeIngressoLoteService();
        var controller = new IngressoLoteController(lotes, usuarios, new FakeCurrentUser(userId));

        var result = await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid(), NomeLote = "Pista", Preco = 10, QuantidadeTotal = 5 });

        Assert.IsType<OkObjectResult>(result);
        Assert.True(lotes.CreateCalled);
        Assert.Equal(orgCampId, lotes.LastOrganizadorCampeonatoId);
        Assert.False(lotes.LastEhAdmin);
    }

    [Fact]
    public async Task CriarLote_UsuarioSemPerfilOrganizador_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var lotes = new FakeIngressoLoteService();
        var controller = new IngressoLoteController(lotes, usuarios, new FakeCurrentUser(userId));

        var result = await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid() });

        Assert.IsType<ForbidResult>(result);
        Assert.False(lotes.CreateCalled);
    }

    [Fact]
    public async Task CriarLote_Admin_IgnoraFaltaDePerfil()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var lotes = new FakeIngressoLoteService();
        var controller = new IngressoLoteController(lotes, usuarios, new FakeCurrentUser(userId, isAdmin: true));

        var result = await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid() });

        Assert.IsType<OkObjectResult>(result);
        Assert.True(lotes.LastEhAdmin);
    }

    [Fact]
    public async Task CriarLote_ServicoLancaUnauthorized_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var lotes = new FakeIngressoLoteService { ThrowOnCriarLote = new UnauthorizedAccessException() };
        var controller = new IngressoLoteController(lotes, usuarios, new FakeCurrentUser(userId));

        Assert.IsType<ForbidResult>(await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task CriarLote_ServicoLancaExcecaoGenerica_RetornaBadRequest()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var lotes = new FakeIngressoLoteService { ThrowOnCriarLote = new Exception("Partida não encontrada.") };
        var controller = new IngressoLoteController(lotes, usuarios, new FakeCurrentUser(userId));

        Assert.IsType<BadRequestObjectResult>(await controller.CriarLote(new CriarIngressoLoteDTO { PartidaId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task ObterLotesPorPartida_RetornaListaMapeada()
    {
        var lote = new IngressoLote { Id = Guid.NewGuid(), PartidaId = Guid.NewGuid(), NomeLote = "Pista", Preco = 20, QuantidadeTotal = 10, QuantidadeDisponivel = 8, Ativo = true };
        var lotes = new FakeIngressoLoteService { LotesResult = new[] { lote } };
        var controller = new IngressoLoteController(lotes, new FakeUsuarioService(), new FakeCurrentUser(Guid.NewGuid()));

        var result = Assert.IsType<OkObjectResult>(await controller.ObterLotesPorPartida(lote.PartidaId));
        var dtoList = Assert.IsAssignableFrom<System.Collections.IEnumerable>(result.Value).Cast<object>().ToList();
        Assert.Single(dtoList);
    }
}
