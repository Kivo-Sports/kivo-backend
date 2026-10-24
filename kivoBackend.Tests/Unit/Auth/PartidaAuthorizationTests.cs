using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace kivoBackend.Tests.Unit.Auth;

/// <summary>
/// Partida ownership: only the campeonato's own OrganizadorCampeonato (or an
/// admin) may generate/edit/schedule its matches — mirrors the pattern already
/// covered for Time and Campeonato in SecurityAuthorizationTests.
/// </summary>
public class PartidaAuthorizationTests
{
    private readonly Guid _orgCampA = Guid.NewGuid();
    private readonly Guid _orgCampB = Guid.NewGuid();
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();

    private (PartidaController controller, Partida partida) NovoControllerParaUserB(Campeonato campeonatoA)
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userB] = TestEntities.UsuarioOrganizadorCampeonato(_userB, _orgCampB);
        var partida = new Partida { CampeonatoId = campeonatoA.Id };
        var partidaRepo = new InMemoryRepo<Partida>(partida);
        var partidaService = new PartidaService(partidaRepo, new FakeCampeonatoRepository(campeonatoA), new InMemoryTimeRepo(), new FakeNotificacaoService());
        var campeonatoService = new CampeonatoService(new InMemoryRepo<Campeonato>(campeonatoA), new InMemoryRepo<CampeonatoTime>(), new InMemoryRepo<Time>(), new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(campeonatoA));
        var controller = new PartidaController(partidaService, campeonatoService, usuarios, new FakeCurrentUser(_userB));
        return (controller, partida);
    }

    [Fact]
    public async Task GerarTabela_OrganizadorDeOutroCampeonato_RetornaForbidden()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var (controller, _) = NovoControllerParaUserB(campeonatoA);

        var result = await controller.Gerar(campeonatoA.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AtualizarPlacar_OrganizadorDeOutroCampeonato_RetornaForbidden()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var (controller, partida) = NovoControllerParaUserB(campeonatoA);

        var result = await controller.AtualizarPlacar(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 1, GolsTimeVisitante = 0 });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Agendar_OrganizadorDeOutroCampeonato_RetornaForbidden()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var (controller, partida) = NovoControllerParaUserB(campeonatoA);

        var result = await controller.Agendar(partida.Id, new AgendarPartidaDTO { DataHora = DateTime.Now.AddDays(1), Local = "Estadio" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AtualizarPlacar_OrganizadorDonoDoCampeonato_Permite()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var partida = new Partida { CampeonatoId = campeonatoA.Id, DataHora = DateTime.Now.AddMinutes(-10), TimeCasaId = Guid.NewGuid(), TimeVisitanteId = Guid.NewGuid() };
        var partidaRepo = new InMemoryRepo<Partida>(partida);
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = TestEntities.UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var partidaService = new PartidaService(partidaRepo, new FakeCampeonatoRepository(campeonatoA), new InMemoryTimeRepo(), new FakeNotificacaoService());
        var campeonatoService = new CampeonatoService(new InMemoryRepo<Campeonato>(campeonatoA), new InMemoryRepo<CampeonatoTime>(), new InMemoryRepo<Time>(), new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(campeonatoA));
        var controller = new PartidaController(partidaService, campeonatoService, usuarios, new FakeCurrentUser(_userA));

        var result = await controller.AtualizarPlacar(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 1 });

        Assert.IsType<OkObjectResult>(result);
        Assert.True(partida.Finalizado);
    }

    [Fact]
    public async Task AtualizarPlacar_PartidaJaFinalizada_RetornaBadRequestSemChamarService()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var partida = new Partida { CampeonatoId = campeonatoA.Id, Finalizado = true, DataHora = DateTime.Now.AddMinutes(-10) };
        var partidaRepo = new InMemoryRepo<Partida>(partida);
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = TestEntities.UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var partidaService = new PartidaService(partidaRepo, new FakeCampeonatoRepository(campeonatoA), new InMemoryTimeRepo(), new FakeNotificacaoService());
        var campeonatoService = new CampeonatoService(new InMemoryRepo<Campeonato>(campeonatoA), new InMemoryRepo<CampeonatoTime>(), new InMemoryRepo<Time>(), new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(campeonatoA));
        var controller = new PartidaController(partidaService, campeonatoService, usuarios, new FakeCurrentUser(_userA));

        var result = await controller.AtualizarPlacar(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AtualizarPlacar_PartidaAindaNaoOcorreu_RetornaBadRequest()
    {
        var campeonatoA = TestEntities.Campeonato(_orgCampA);
        var partida = new Partida { CampeonatoId = campeonatoA.Id, DataHora = DateTime.Now.AddDays(1) };
        var partidaRepo = new InMemoryRepo<Partida>(partida);
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = TestEntities.UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var partidaService = new PartidaService(partidaRepo, new FakeCampeonatoRepository(campeonatoA), new InMemoryTimeRepo(), new FakeNotificacaoService());
        var campeonatoService = new CampeonatoService(new InMemoryRepo<Campeonato>(campeonatoA), new InMemoryRepo<CampeonatoTime>(), new InMemoryRepo<Time>(), new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(campeonatoA));
        var controller = new PartidaController(partidaService, campeonatoService, usuarios, new FakeCurrentUser(_userA));

        var result = await controller.AtualizarPlacar(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 1 });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
