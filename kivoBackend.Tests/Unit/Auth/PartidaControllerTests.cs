using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests.Unit.Auth;

/// <summary>
/// PartidaController — read endpoints and the admin-only surface (ownership checks for the
/// organizer-facing endpoints are already covered by PartidaAuthorizationTests).
/// </summary>
public class PartidaControllerTests
{
    private static (PartidaController Controller, InMemoryRepo<Partida> Partidas, Campeonato Campeonato) NovoAdminController(Partida[]? partidas = null)
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var partidaRepo = new InMemoryRepo<Partida>(partidas ?? Array.Empty<Partida>());
        var partidaService = new PartidaService(partidaRepo, new FakeCampeonatoRepository(campeonato), new InMemoryTimeRepo(), new FakeNotificacaoService());
        var campeonatoService = new CampeonatoService(new InMemoryRepo<Campeonato>(campeonato), new InMemoryRepo<CampeonatoTime>(), new InMemoryRepo<Time>(), new InMemoryRepo<Partida>(), new FakeCampeonatoRepository(campeonato));
        var controller = new PartidaController(partidaService, campeonatoService, new FakeUsuarioService(), new FakeCurrentUser(Guid.NewGuid(), isAdmin: true));
        return (controller, partidaRepo, campeonato);
    }

    [Fact]
    public async Task GetTabela_CampeonatoExistente_RetornaOk()
    {
        var (controller, _, campeonato) = NovoAdminController();

        Assert.IsType<OkObjectResult>(await controller.GetTabela(campeonato.Id));
    }

    [Fact]
    public async Task GetChaveamento_SemPartidasMataMata_RetornaOkVazio()
    {
        var (controller, _, campeonato) = NovoAdminController();

        var result = Assert.IsType<OkObjectResult>(await controller.GetChaveamento(campeonato.Id));
        Assert.Empty((IEnumerable<object>)result.Value!);
    }

    [Fact]
    public async Task GetJogos_SemPartidas_RetornaOkVazio()
    {
        var (controller, _, campeonato) = NovoAdminController();

        var result = Assert.IsType<OkObjectResult>(await controller.GetJogos(campeonato.Id));
        Assert.Empty((IEnumerable<object>)result.Value!);
    }

    [Fact]
    public async Task GetById_Existente_RetornaOk()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), Local = "Estadio" };
        var (controller, _, _) = NovoAdminController(new[] { partida });

        Assert.IsType<OkObjectResult>(await controller.GetById(partida.Id));
    }

    [Fact]
    public async Task GetById_Inexistente_RetornaBadRequest()
    {
        var (controller, _, _) = NovoAdminController();

        Assert.IsType<BadRequestObjectResult>(await controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task Agendar_Admin_AtualizaDataELocal()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), Local = "Antigo" };
        var (controller, partidas, _) = NovoAdminController(new[] { partida });

        var novaData = DateTime.Now.AddDays(3);
        var result = await controller.Agendar(partida.Id, new AgendarPartidaDTO { DataHora = novaData, Local = "Novo Estadio" });

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Novo Estadio", (await partidas.ObterPorId(partida.Id))!.Local);
    }

    [Fact]
    public async Task Agendar_Inexistente_RetornaNotFound()
    {
        var (controller, _, _) = NovoAdminController();

        Assert.IsType<NotFoundResult>(await controller.Agendar(Guid.NewGuid(), new AgendarPartidaDTO { DataHora = DateTime.Now, Local = "X" }));
    }

    [Fact]
    public async Task CriarManual_Admin_CriaPartida()
    {
        var (controller, partidas, campeonato) = NovoAdminController();

        var dto = new CriarPartidaManualDTO { CampeonatoId = campeonato.Id, GolsTimeCasa = 0, GolsTimeVisitante = 0, Local = "Estadio X" };
        var result = Assert.IsType<OkObjectResult>(await controller.CriarManual(dto));

        Assert.Equal(1, partidas.Count);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task EditarAdmin_Existente_Atualiza()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), Local = "Antigo" };
        var (controller, partidas, _) = NovoAdminController(new[] { partida });

        var dto = new EditarPartidaAdminDTO { Local = "Novo Local", DataHora = DateTime.Now.AddDays(1) };
        Assert.IsType<OkObjectResult>(await controller.EditarAdmin(partida.Id, dto));

        Assert.Equal("Novo Local", (await partidas.ObterPorId(partida.Id))!.Local);
    }

    [Fact]
    public async Task EditarAdmin_Inexistente_RetornaBadRequest()
    {
        var (controller, _, _) = NovoAdminController();

        Assert.IsType<BadRequestObjectResult>(await controller.EditarAdmin(Guid.NewGuid(), new EditarPartidaAdminDTO()));
    }

    [Fact]
    public async Task DeletarAdmin_Existente_Remove()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid() };
        var (controller, partidas, _) = NovoAdminController(new[] { partida });

        Assert.IsType<OkObjectResult>(await controller.DeletarAdmin(partida.Id));
        Assert.Equal(0, partidas.Count);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_PontosCorridos_FinalizaPartida()
    {
        var partida = new Partida { Id = Guid.NewGuid(), CampeonatoId = Guid.NewGuid(), Fase = EnumFaseMataMata.Nenhuma };
        var (controller, partidas, _) = NovoAdminController(new[] { partida });

        var result = await controller.AtualizarPlacarAdmin(partida.Id, new AtualizarPlacarDTO { GolsTimeCasa = 2, GolsTimeVisitante = 1 });

        Assert.IsType<OkObjectResult>(result);
        Assert.True((await partidas.ObterPorId(partida.Id))!.Finalizado);
    }

    [Fact]
    public async Task AtualizarPlacarAdmin_Inexistente_RetornaBadRequest()
    {
        var (controller, _, _) = NovoAdminController();

        Assert.IsType<BadRequestObjectResult>(await controller.AtualizarPlacarAdmin(Guid.NewGuid(), new AtualizarPlacarDTO()));
    }
}
