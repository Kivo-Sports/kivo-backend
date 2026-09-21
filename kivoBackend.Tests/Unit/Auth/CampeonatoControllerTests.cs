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
/// CampeonatoController — read endpoints and admin-only/lifecycle actions not already
/// exercised by SecurityAuthorizationTests (ownership on create/edit, invalid dates,
/// duplicate/foreign invites).
/// </summary>
public class CampeonatoControllerTests
{
    private static (CampeonatoController Controller, InMemoryRepo<Campeonato> Campeonatos, InMemoryRepo<CampeonatoTime> Vinculos)
        Build(Campeonato? campeonato, Guid? userId, bool isAdmin = false, FakeUsuarioService? usuarios = null, CampeonatoTime[]? vinculos = null, Time[]? times = null, Partida[]? partidas = null)
    {
        var campeonatoRepo = campeonato == null ? new InMemoryRepo<Campeonato>() : new InMemoryRepo<Campeonato>(campeonato);
        var vinculoRepo = new InMemoryRepo<CampeonatoTime>(vinculos ?? Array.Empty<CampeonatoTime>());
        var service = new CampeonatoService(campeonatoRepo, vinculoRepo, new InMemoryRepo<Time>(times ?? Array.Empty<Time>()),
            new InMemoryRepo<Partida>(partidas ?? Array.Empty<Partida>()), new FakeCampeonatoRepository(campeonato));
        var controller = new CampeonatoController(service, new FakeStorageService(), usuarios ?? new FakeUsuarioService(), new FakeCurrentUser(userId, isAdmin));
        controller.ControllerContext = FakeHttp.ContextFor(userId, isAdmin ? "Administrador" : "");
        return (controller, campeonatoRepo, vinculoRepo);
    }

    private static CriarCampeonatoDto CriarDto(Guid esporteId, Guid organizadorId) => new()
    {
        OrganizadorCampeonatoId = organizadorId,
        EsporteId = esporteId,
        Nome = "Copa",
        DataInicio = DateTime.Today,
        DataFim = DateTime.Today.AddDays(10),
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos,
        PontosVitoria = 3,
        PontosDerrota = 0,
        PontosEmpate = 1
    };

    // ---------- GetAll / GetById ----------

    [Fact]
    public async Task GetAll_RetornaOk()
    {
        var (controller, _, _) = Build(Campeonato(Guid.NewGuid()), Guid.NewGuid());

        Assert.IsType<OkObjectResult>(await controller.GetAll());
    }

    [Fact]
    public async Task GetById_Inexistente_RetornaBadRequest()
    {
        // CampeonatoService.ObterCampeonatoPorId throws instead of returning null, so the
        // controller's own `if (campeonato == null) return NotFound(...)` branch is dead code —
        // an unknown id is reported as a generic BadRequest, not a 404.
        var (controller, _, _) = Build(null, Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_Existente_RetornaOk()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, Guid.NewGuid());

        var result = Assert.IsType<OkObjectResult>(await controller.GetById(campeonato.Id));
        Assert.Equal(campeonato.Id, Assert.IsType<ListarCampeonatoDto>(result.Value).Id);
    }

    // ---------- Post validations ----------

    [Fact]
    public async Task Post_Hibrido_SemQuantidadeTimesClassificam_RetornaBadRequest()
    {
        var (controller, _, _) = Build(null, Guid.NewGuid());
        var dto = CriarDto(Guid.NewGuid(), Guid.NewGuid());
        dto.FormatoCampeonato = EnumFormatoCampeonato.Hibrido;
        dto.QuantidadeTimesClassificam = null;

        Assert.IsType<BadRequestObjectResult>(await controller.Post(dto, null));
    }

    [Fact]
    public async Task Post_PontosCorridosSemPontuacao_RetornaBadRequest()
    {
        var (controller, _, _) = Build(null, Guid.NewGuid());
        var dto = CriarDto(Guid.NewGuid(), Guid.NewGuid());
        dto.PontosVitoria = null;

        Assert.IsType<BadRequestObjectResult>(await controller.Post(dto, null));
    }

    [Fact]
    public async Task Post_EsporteVazio_RetornaBadRequest()
    {
        var usuarios = new FakeUsuarioService();
        var userId = Guid.NewGuid();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var (controller, _, _) = Build(null, userId, usuarios: usuarios);
        var dto = CriarDto(Guid.Empty, Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.Post(dto, null));
    }

    [Fact]
    public async Task Post_UsuarioSemPerfilOrganizador_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var (controller, _, _) = Build(null, userId, usuarios: usuarios);

        Assert.IsType<ForbidResult>(await controller.Post(CriarDto(Guid.NewGuid(), Guid.NewGuid()), null));
    }

    [Fact]
    public async Task Post_DadosValidos_RetornaCreated()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var (controller, campeonatos, _) = Build(null, userId, usuarios: usuarios);

        var result = Assert.IsType<CreatedAtActionResult>(await controller.Post(CriarDto(Guid.NewGuid(), Guid.NewGuid()), null));
        var dto = Assert.IsType<ListarCampeonatoDto>(result.Value);
        Assert.Equal(orgId, dto.OrganizadorCampeonatoId);
    }

    // ---------- Lifecycle ----------

    [Fact]
    public async Task AbrirInscricoes_Owner_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var campeonato = Campeonato(orgId);
        var (controller, _, _) = Build(campeonato, userId, usuarios: usuarios);

        Assert.IsType<OkObjectResult>(await controller.AbrirInscricoes(campeonato.Id));
    }

    [Fact]
    public async Task AbrirInscricoes_OutroOrganizador_RetornaForbid()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var outroUserId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[outroUserId] = UsuarioOrganizadorCampeonato(outroUserId, Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, outroUserId, usuarios: usuarios);

        Assert.IsType<ForbidResult>(await controller.AbrirInscricoes(campeonato.Id));
    }

    [Fact]
    public async Task IniciarCampeonato_SemTimesConfirmados_RetornaBadRequest()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<BadRequestObjectResult>(await controller.IniciarCampeonato(campeonato.Id));
    }

    [Fact]
    public async Task Cancelar_Admin_RetornaOk()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.Cancelar(campeonato.Id));
    }

    [Fact]
    public async Task Descancelar_CampeonatoCancelado_RetornaOk()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.Cancelado;
        var (controller, _, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.Descancelar(campeonato.Id));
    }

    [Fact]
    public async Task Descancelar_CampeonatoNaoCancelado_RetornaBadRequest()
    {
        var campeonato = Campeonato(Guid.NewGuid()); // Rascunho
        var (controller, _, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<BadRequestObjectResult>(await controller.Descancelar(campeonato.Id));
    }

    [Fact]
    public async Task Reatribuir_Admin_AtualizaOrganizador()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var (controller, campeonatos, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);
        var novoOrgId = Guid.NewGuid();

        Assert.IsType<OkObjectResult>(await controller.Reatribuir(campeonato.Id, new ReatribuirCampeonatoDTO { NovoOrganizadorCampeonatoId = novoOrgId }));
        Assert.Equal(novoOrgId, (await campeonatos.ObterPorId(campeonato.Id))!.OrganizadorCampeonatoId);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_Inexistente_RetornaBadRequest()
    {
        // Same dead-code discrepancy as GetById: the service throws rather than returning null.
        var (controller, _, _) = Build(null, Guid.NewGuid());

        Assert.IsType<BadRequestObjectResult>(await controller.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_Admin_RemoveEmQualquerEtapa()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.EmAndamento;
        var (controller, _, _) = Build(campeonato, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.Delete(campeonato.Id));
    }

    [Fact]
    public async Task Delete_OwnerRascunho_RemoveComSucesso()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var campeonato = Campeonato(orgId); // Rascunho
        var (controller, _, _) = Build(campeonato, userId, usuarios: usuarios);

        Assert.IsType<OkObjectResult>(await controller.Delete(campeonato.Id));
    }

    [Fact]
    public async Task Delete_OwnerForaDoRascunho_RetornaBadRequest()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var campeonato = Campeonato(orgId);
        campeonato.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        var (controller, _, _) = Build(campeonato, userId, usuarios: usuarios);

        Assert.IsType<BadRequestObjectResult>(await controller.Delete(campeonato.Id));
    }

    [Fact]
    public async Task Delete_OutroOrganizador_RetornaForbid()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var outroUserId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[outroUserId] = UsuarioOrganizadorCampeonato(outroUserId, Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, outroUserId, usuarios: usuarios);

        Assert.IsType<ForbidResult>(await controller.Delete(campeonato.Id));
    }

    // ---------- RemoverTime ----------

    [Fact]
    public async Task RemoverTime_Owner_RetornaNoContent()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var campeonato = Campeonato(orgId);
        var (controller, _, _) = Build(campeonato, userId, usuarios: usuarios);

        var result = await controller.RemoverTime(new RemoverTimeCampeonatoDTO { CampeonatoId = campeonato.Id, TimeId = Guid.NewGuid() });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RemoverTime_OutroOrganizador_RetornaForbid()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var outroUserId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[outroUserId] = UsuarioOrganizadorCampeonato(outroUserId, Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, outroUserId, usuarios: usuarios);

        var result = await controller.RemoverTime(new RemoverTimeCampeonatoDTO { CampeonatoId = campeonato.Id, TimeId = Guid.NewGuid() });

        Assert.IsType<ForbidResult>(result);
    }

    // ---------- Convites ----------

    [Fact]
    public async Task ObterConvitesPendentes_Admin_IgnoraOwnership()
    {
        var (controller, _, _) = Build(null, Guid.NewGuid(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.ObterConvitesPendentes(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterConvitesPendentes_OutroOrganizadorTime_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, Guid.NewGuid());
        var (controller, _, _) = Build(null, userId, usuarios: usuarios);

        Assert.IsType<ForbidResult>(await controller.ObterConvitesPendentes(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterConvitesPendentes_ProprioOrganizadorTime_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var (controller, _, _) = Build(null, userId, usuarios: usuarios);

        Assert.IsType<OkObjectResult>(await controller.ObterConvitesPendentes(orgTimeId));
    }

    [Fact]
    public async Task ObterConvitesPorCampeonato_Owner_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, orgId);
        var campeonato = Campeonato(orgId);
        var (controller, _, _) = Build(campeonato, userId, usuarios: usuarios);

        Assert.IsType<OkObjectResult>(await controller.ObterConvitesPorCampeonato(campeonato.Id));
    }

    [Fact]
    public async Task ConvidarTime_OutroOrganizador_RetornaForbid()
    {
        var campeonato = Campeonato(Guid.NewGuid());
        var outroUserId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[outroUserId] = UsuarioOrganizadorCampeonato(outroUserId, Guid.NewGuid());
        var (controller, _, _) = Build(campeonato, outroUserId, usuarios: usuarios);

        var result = await controller.ConvidarTime(new ConvidarTimeDTO { CampeonatoId = campeonato.Id, TimeId = Guid.NewGuid() });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Responder_SemPerfilOrganizadorTime_RetornaForbid()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId); // autenticado, mas sem perfil de OrganizadorTime
        var (controller, _, _) = Build(null, userId, usuarios: usuarios);

        Assert.IsType<ForbidResult>(await controller.Responder(Guid.NewGuid(), new ResponderConviteDTO { Aceito = true }));
    }
}
