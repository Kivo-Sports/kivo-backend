using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests.Unit.Auth;

/// <summary>
/// TimeController — the ownership/ToggleStatus rules described for "TimeService" in the
/// test plan actually live in this controller (TimeService itself is a thin ServiceGenerics
/// passthrough, see TimeServiceTests). Covered here rather than fabricated on the service.
/// </summary>
public class TimeControllerTests
{
    private static AtualizarTimeDto AtualizarDto() => new() { Nome = "Novo Nome", Cidade = "Curitiba", Estado = "PR", EsporteId = Guid.NewGuid() };

    [Fact]
    public async Task GetAll_RetornaTodosOsTimes()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var controller = new TimeController(new FakeTimeService { Current = time }, new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        var result = Assert.IsType<OkObjectResult>(await controller.GetAll());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetById_Inexistente_RetornaNotFound()
    {
        var controller = new TimeController(new FakeTimeService(), new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        Assert.IsType<NotFoundObjectResult>(await controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetById_Existente_RetornaOk()
    {
        var time = new Time { Id = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var controller = new TimeController(new FakeTimeService { Current = time }, new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        var result = Assert.IsType<OkObjectResult>(await controller.GetById(time.Id));
        var dto = Assert.IsType<ListarTimeDto>(result.Value);
        Assert.Equal(time.Id, dto.Id);
    }

    [Fact]
    public async Task GetAllOrganizador_Admin_VeTodosOsTimes()
    {
        var adminId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        var admin = UsuarioTorcedor(adminId);
        admin.EnumCargo = EnumCargo.Administrador;
        usuarios.Users[adminId] = admin;
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var controller = new TimeController(new FakeTimeService { Current = time }, usuarios,
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(adminId, isAdmin: true));
        controller.ControllerContext = FakeHttpContext(adminId, "Administrador");

        var result = Assert.IsType<OkObjectResult>(await controller.GetAllOrganizador());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetAllOrganizador_OrganizadorSemPerfil_RetornaBadRequest()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId); // sem OrganizadorTime
        var controller = new TimeController(new FakeTimeService(), usuarios,
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));
        controller.ControllerContext = FakeHttpContext(userId);

        Assert.IsType<BadRequestObjectResult>(await controller.GetAllOrganizador());
    }

    [Fact]
    public async Task GetAllOrganizador_Organizador_VeApenasSeusTimes()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var meuTime = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, Nome = "Meu", Cidade = "SP", Estado = "SP" };
        var controller = new TimeController(new FakeTimeService { Current = meuTime }, usuarios,
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));
        controller.ControllerContext = FakeHttpContext(userId);

        var result = Assert.IsType<OkObjectResult>(await controller.GetAllOrganizador());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Put_Owner_AtualizaComSucesso()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, Nome = "Antigo", Cidade = "SP", Estado = "SP" };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));

        var result = Assert.IsType<OkObjectResult>(await controller.Put(time.Id, AtualizarDto(), null));
        var dto = Assert.IsType<ListarTimeDto>(result.Value);
        Assert.Equal("Novo Nome", dto.Nome);
        Assert.True(times.UpdateCalled);
    }

    [Fact]
    public async Task Post_EsporteIdVazio_RetornaBadRequest()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, Guid.NewGuid());
        var controller = new TimeController(new FakeTimeService(), usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));

        var result = await controller.Post(new CriarTimeDto { EsporteId = Guid.Empty, Nome = "A", Cidade = "SP", Estado = "SP" }, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Put_EsporteIdVazio_MantemEsporteAtual()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var esporteOriginal = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, EsporteId = esporteOriginal, Nome = "Antigo", Cidade = "SP", Estado = "SP" };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));

        var dto = new AtualizarTimeDto { Nome = "Novo", Cidade = "Curitiba", Estado = "PR", EsporteId = Guid.Empty };
        var result = Assert.IsType<OkObjectResult>(await controller.Put(time.Id, dto, null));

        Assert.Equal(esporteOriginal, Assert.IsType<ListarTimeDto>(result.Value).EsporteId);
    }

    [Fact]
    public async Task Put_TimeInexistente_RetornaNotFound()
    {
        var controller = new TimeController(new FakeTimeService(), new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        Assert.IsType<NotFoundObjectResult>(await controller.Put(Guid.NewGuid(), AtualizarDto(), null));
    }

    [Fact]
    public async Task ToggleStatus_TimeInexistente_RetornaNotFound()
    {
        var controller = new TimeController(new FakeTimeService(), new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        Assert.IsType<NotFoundObjectResult>(await controller.ToggleStatus(Guid.NewGuid()));
    }

    [Fact]
    public async Task ToggleStatus_Owner_SemCampeonatoAtivo_Desativa()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, Nome = "A", Cidade = "SP", Estado = "SP", Ativo = true };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new InMemoryRepo<CampeonatoTime>(), new FakeCurrentUser(userId));

        var result = Assert.IsType<OkObjectResult>(await controller.ToggleStatus(time.Id));
        var dto = Assert.IsType<ListarTimeDto>(result.Value);
        Assert.False(dto.Ativo);
    }

    [Fact]
    public async Task ToggleStatus_Owner_ComCampeonatoAtivo_Bloqueia()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, Nome = "A", Cidade = "SP", Estado = "SP", Ativo = true };
        var campeonatoEmAndamento = Campeonato(Guid.NewGuid());
        campeonatoEmAndamento.EnumStatusCampeonato = EnumStatusCampeonato.InscricoesAbertas;
        campeonatoEmAndamento.DataInicio = DateTime.Today.AddDays(1);
        campeonatoEmAndamento.DataFim = DateTime.Today.AddDays(10);
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), TimeId = time.Id, CampeonatoId = campeonatoEmAndamento.Id, Campeonato = campeonatoEmAndamento };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new InMemoryRepo<CampeonatoTime>(vinculo), new FakeCurrentUser(userId));

        var result = await controller.ToggleStatus(time.Id);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(times.UpdateCalled);
    }

    [Fact]
    public async Task Delete_TimeInexistente_RetornaNotFound()
    {
        var controller = new TimeController(new FakeTimeService(), new FakeUsuarioService(),
            new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid()));

        Assert.IsType<NotFoundObjectResult>(await controller.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_Owner_RemoveComSucesso()
    {
        var userId = Guid.NewGuid();
        var orgTimeId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorTime(userId, orgTimeId);
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId, Nome = "A", Cidade = "SP", Estado = "SP" };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(userId));

        Assert.IsType<NoContentResult>(await controller.Delete(time.Id));
        Assert.True(times.RemoveCalled);
    }

    [Fact]
    public async Task Reatribuir_AtualizaOrganizadorDoTime()
    {
        var time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = Guid.NewGuid(), Nome = "A", Cidade = "SP", Estado = "SP" };
        var times = new FakeTimeService { Current = time };
        var controller = new TimeController(times, new FakeUsuarioService(), new FakeStorageService(),
            new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid(), isAdmin: true));

        var novoOrganizadorId = Guid.NewGuid();
        var result = Assert.IsType<OkObjectResult>(await controller.Reatribuir(time.Id, new ReatribuirTimeDTO { NovoOrganizadorTimeId = novoOrganizadorId }));
        var dto = Assert.IsType<ListarTimeDto>(result.Value);
        Assert.Equal(novoOrganizadorId, dto.OrganizadorTimeId);
        Assert.True(times.UpdateCalled);
    }

    [Fact]
    public async Task Reatribuir_TimeInexistente_RetornaNotFound()
    {
        var controller = new TimeController(new FakeTimeService(), new FakeUsuarioService(), new FakeStorageService(),
            new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(Guid.NewGuid(), isAdmin: true));

        Assert.IsType<NotFoundObjectResult>(await controller.Reatribuir(Guid.NewGuid(), new ReatribuirTimeDTO { NovoOrganizadorTimeId = Guid.NewGuid() }));
    }

    private static Microsoft.AspNetCore.Mvc.ControllerContext FakeHttpContext(Guid userId, params string[] roles)
    {
        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(r => new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, r)));
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) };
        return new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };
    }
}
