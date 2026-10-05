using System.Linq.Expressions;
using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Presentation.Auth;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests;

public class SecurityAuthorizationTests
{
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly Guid _orgTimeA = Guid.NewGuid();
    private readonly Guid _orgTimeB = Guid.NewGuid();
    private readonly Guid _orgCampA = Guid.NewGuid();
    private readonly Guid _orgCampB = Guid.NewGuid();

    [Fact]
    public async Task Usuario_UserA_EditUserB_ReturnsForbidden()
    {
        var controller = new UsuarioController(
            new FakeUsuarioService(),
            new EmptyRepo<OrganizadorCampeonato>(),
            new EmptyRepo<OrganizadorTime>(),
            new FakeCurrentUser(_userA));

        var result = await controller.EditarTorcedor(_userB, EditarUsuarioDto());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Usuario_UserA_EditOwnProfile_Succeeds()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioTorcedor(_userA);
        var controller = new UsuarioController(
            usuarios,
            new EmptyRepo<OrganizadorCampeonato>(),
            new EmptyRepo<OrganizadorTime>(),
            new FakeCurrentUser(_userA));

        var result = await controller.EditarTorcedor(_userA, EditarUsuarioDto());

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Usuario_UserA_DeleteUserB_ReturnsForbidden()
    {
        var service = new FakeUsuarioService();
        var controller = new UsuarioController(
            service,
            new EmptyRepo<OrganizadorCampeonato>(),
            new EmptyRepo<OrganizadorTime>(),
            new FakeCurrentUser(_userA));

        var result = await controller.Delete(_userB);

        Assert.IsType<ForbidResult>(result);
        Assert.False(service.RemoveCalled);
    }

    [Fact]
    public async Task Time_CreateWithOwnerB_UsesOwnerAFromToken()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorTime(_userA, _orgTimeA);
        var times = new FakeTimeService();
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(_userA));

        var result = await controller.Post(new CriarTimeDto
        {
            OrganizadorTimeId = _orgTimeB,
            EsporteId = Guid.NewGuid(),
            Nome = "Cruzeiro",
            Cidade = "Belo Horizonte",
            Estado = "MG",
            LogoUrl = "https://example.test/logo.png"
        }, null);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ListarTimeDto>(created.Value);
        Assert.Equal(_orgTimeA, dto.OrganizadorTimeId);
        Assert.Equal(_orgTimeA, times.Added!.OrganizadorTimeId);
    }

    [Fact]
    public async Task Time_OrganizerB_CannotMutateTimeA()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userB] = UsuarioOrganizadorTime(_userB, _orgTimeB);
        var timeA = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = _orgTimeA, EsporteId = Guid.NewGuid(), Nome = "Time A", Cidade = "Curitiba", Estado = "PR", Ativo = true };
        var times = new FakeTimeService { Current = timeA };
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(_userB));

        Assert.IsType<ForbidResult>(await controller.Put(timeA.Id, AtualizarTimeDto(), null));
        Assert.IsType<ForbidResult>(await controller.ToggleStatus(timeA.Id));
        Assert.IsType<ForbidResult>(await controller.Delete(timeA.Id));
        Assert.False(times.UpdateCalled);
        Assert.False(times.RemoveCalled);
    }

    [Fact]
    public async Task Campeonato_CreateWithOwnerB_UsesOwnerAFromToken()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var campeonatos = new FakeCampeonatoService();
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userA));

        var result = await controller.Post(CriarCampeonatoDto(_orgCampB), null);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ListarCampeonatoDto>(created.Value);
        Assert.Equal(_orgCampA, dto.OrganizadorCampeonatoId);
        Assert.Equal(_orgCampA, campeonatos.Added!.OrganizadorCampeonatoId);
    }

    [Fact]
    public async Task Time_CreateWithoutLogo_AllowsNullLogo()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorTime(_userA, _orgTimeA);
        var times = new FakeTimeService();
        var controller = new TimeController(times, usuarios, new FakeStorageService(), new EmptyRepo<CampeonatoTime>(), new FakeCurrentUser(_userA));

        var result = await controller.Post(new CriarTimeDto
        {
            OrganizadorTimeId = _orgTimeA,
            EsporteId = Guid.NewGuid(),
            Nome = "Palmeiras",
            Cidade = "Sao Paulo",
            Estado = "SP"
        }, null);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ListarTimeDto>(created.Value);
        Assert.Null(dto.LogoUrl);
        Assert.Null(times.Added!.LogoUrl);
    }

    [Fact]
    public async Task Campeonato_CreateWithInvalidDates_ReturnsBadRequestAndDoesNotPersist()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var campeonatos = new FakeCampeonatoService();
        var dto = CriarCampeonatoDto(_orgCampA);
        dto.DataInicio = DateTime.Today.AddDays(20);
        dto.DataFim = DateTime.Today.AddDays(5);
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userA));

        var result = await controller.Post(dto, null);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Null(campeonatos.Added);
    }

    [Fact]
    public async Task Campeonato_EditWithInvalidDates_ReturnsBadRequestAndDoesNotPersist()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorCampeonato(_userA, _orgCampA);
        var campA = Campeonato(_orgCampA);
        var campeonatos = new FakeCampeonatoService { Current = campA };
        var dto = EditarCampeonatoDto();
        dto.DataInicio = DateTime.Today.AddDays(20);
        dto.DataFim = DateTime.Today.AddDays(5);
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userA));

        var result = await controller.Put(campA.Id, dto, null);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.False(campeonatos.Mutated);
    }

    [Fact]
    public async Task Campeonato_OrganizerB_CannotMutateCampeonatoA()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userB] = UsuarioOrganizadorCampeonato(_userB, _orgCampB);
        var campA = Campeonato(_orgCampA);
        var campeonatos = new FakeCampeonatoService { Current = campA };
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userB));

        Assert.IsType<ForbidResult>(await controller.Put(campA.Id, EditarCampeonatoDto(), null));
        Assert.IsType<ForbidResult>(await controller.AbrirInscricoes(campA.Id));
        Assert.IsType<ForbidResult>(await controller.IniciarCampeonato(campA.Id));
        Assert.IsType<ForbidResult>(await controller.Cancelar(campA.Id));
        Assert.IsType<ForbidResult>(await controller.Delete(campA.Id));
        Assert.IsType<ForbidResult>(await controller.ConvidarTime(new ConvidarTimeDTO { CampeonatoId = campA.Id, TimeId = Guid.NewGuid() }));
        Assert.False(campeonatos.Mutated);
    }

    [Fact]
    public void Convite_ResponderEndpoint_RequiresOrganizadorTimeRole()
    {
        var method = typeof(CampeonatoController).GetMethod(nameof(CampeonatoController.Responder));
        var authorize = Assert.Single(method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false).Cast<AuthorizeAttribute>());

        Assert.Equal("OrganizadorTime", authorize.Roles);
    }

    [Fact]
    public async Task Convite_OrganizerB_CannotRespondInviteForTimeA()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userB] = UsuarioOrganizadorTime(_userB, _orgTimeB);
        var campeonatos = new FakeCampeonatoService { ExpectedInviteOwner = _orgTimeA };
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userB));

        var result = await controller.Responder(Guid.NewGuid(), new ResponderConviteDTO { OrganizadorTimeId = _orgTimeA, Aceito = true });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Convite_OrganizerA_CanRespondOwnInvite()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioOrganizadorTime(_userA, _orgTimeA);
        var campeonatos = new FakeCampeonatoService { ExpectedInviteOwner = _orgTimeA };
        var controller = new CampeonatoController(campeonatos, new FakeStorageService(), usuarios, new FakeCurrentUser(_userA));

        var result = await controller.Responder(Guid.NewGuid(), new ResponderConviteDTO { OrganizadorTimeId = _orgTimeB, Aceito = true });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CampeonatoService_DuplicateInvite_ThrowsAndKeepsSingleInvite()
    {
        var esporteId = Guid.NewGuid();
        var campeonatoId = Guid.NewGuid();
        var timeId = Guid.NewGuid();
        var campeonato = new Campeonato { Id = campeonatoId, EsporteId = esporteId };
        var time = new Time { Id = timeId, EsporteId = esporteId };
        var convites = new InMemoryRepo<CampeonatoTime>();
        var service = new CampeonatoService(
            new InMemoryRepo<Campeonato>(campeonato),
            convites,
            new InMemoryRepo<Time>(time),
            new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(campeonato));

        await service.AdicionarTimeAoCampeonato(campeonatoId, timeId);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AdicionarTimeAoCampeonato(campeonatoId, timeId));

        Assert.Contains("já possui convite", ex.Message);
        Assert.Equal(1, convites.Count);
    }

    [Fact]
    public async Task CampeonatoService_AlreadyAnsweredInvite_CannotBeAnsweredAgain()
    {
        var orgTimeId = Guid.NewGuid();
        var participacaoId = Guid.NewGuid();
        var participacao = new CampeonatoTime
        {
            Id = participacaoId,
            CampeonatoId = Guid.NewGuid(),
            TimeId = Guid.NewGuid(),
            Time = new Time { Id = Guid.NewGuid(), OrganizadorTimeId = orgTimeId },
            EnumStatusParticipacao = EnumStatusParticipacao.Pendente
        };
        var convites = new InMemoryRepo<CampeonatoTime>(participacao);
        var campeonato = Campeonato(_orgCampA);
        var service = new CampeonatoService(
            new InMemoryRepo<Campeonato>(campeonato),
            convites,
            new InMemoryRepo<Time>(),
            new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(campeonato));

        await service.ResponderConviteCampeonato(participacaoId, orgTimeId, true);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResponderConviteCampeonato(participacaoId, orgTimeId, false));

        Assert.Contains("já foi respondido", ex.Message);
        Assert.Equal(EnumStatusParticipacao.Aceito, participacao.EnumStatusParticipacao);
    }

    [Fact]
    public async Task FavoritoService_NonexistentTimeOrCampeonato_DoesNotPersist()
    {
        var favoritos = new InMemoryRepo<Favorito>();
        var service = new FavoritoService(
            favoritos,
            new InMemoryRepo<Time>(),
            new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(null));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Adicionar(_userA, EnumTipoFavorito.Time, Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.Adicionar(_userA, EnumTipoFavorito.Campeonato, Guid.NewGuid()));
        Assert.Equal(0, favoritos.Count);
    }

    [Fact]
    public async Task FavoritoService_DuplicateValidFavorite_RemainsIdempotent()
    {
        var timeId = Guid.NewGuid();
        var favoritos = new InMemoryRepo<Favorito>();
        var service = new FavoritoService(
            favoritos,
            new InMemoryRepo<Time>(new Time { Id = timeId }),
            new InMemoryRepo<Partida>(),
            new FakeCampeonatoRepository(null));

        await service.Adicionar(_userA, EnumTipoFavorito.Time, timeId);
        await service.Adicionar(_userA, EnumTipoFavorito.Time, timeId);

        Assert.Equal(1, favoritos.Count);
    }

    [Fact]
    public void Usuario_CheckEmailAndCpf_AreAllowAnonymous()
    {
        var checkEmail = typeof(UsuarioController).GetMethod(nameof(UsuarioController.CheckEmail));
        var checkCpf = typeof(UsuarioController).GetMethod(nameof(UsuarioController.CheckCpf));

        Assert.Contains(checkEmail!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false), a => a is AllowAnonymousAttribute);
        Assert.Contains(checkCpf!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: false), a => a is AllowAnonymousAttribute);
    }

    [Fact]
    public async Task Ingresso_Torcedor_CannotCreateLote()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[_userA] = UsuarioTorcedor(_userA);
        var service = new FakeIngressoLoteService();
        var controller = new IngressoController(service, usuarios, new FakeCurrentUser(_userA));

        var result = await controller.CriarLote(CriarIngressoLoteDto());

        Assert.IsType<ForbidResult>(result);
        Assert.False(service.CreateCalled);
    }

    [Fact]
    public async Task Ingresso_Service_BlocksOrganizerFromOtherCampeonato()
    {
        var partidaId = Guid.NewGuid();
        var campeonatoId = Guid.NewGuid();
        var campeonato = Campeonato(_orgCampA);
        campeonato.Id = campeonatoId;
        var partidaRepo = new InMemoryRepo<Partida>(new Partida { Id = partidaId, CampeonatoId = campeonatoId });
        var campeonatoRepo = new FakeCampeonatoRepository(campeonato);
        var service = new IngressoLoteService(new InMemoryRepo<IngressoLote>(), partidaRepo, campeonatoRepo);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CriarLote(CriarIngressoLoteDto(partidaId), _orgCampB, ehAdmin: false));
    }

    private static EditarUsuarioDTO EditarUsuarioDto() => new()
    {
        Nome = "Ana Silva",
        Email = "ana@example.test",
        Telefone = "11999999999",
        DataNascimento = new DateTime(1994, 3, 10),
        Endereco = EnderecoDto()
    };

    private static AtualizarTimeDto AtualizarTimeDto() => new()
    {
        EsporteId = Guid.NewGuid(),
        Nome = "Time Atualizado",
        Cidade = "Sao Paulo",
        Estado = "SP"
    };

    private static CriarCampeonatoDto CriarCampeonatoDto(Guid owner) => new()
    {
        OrganizadorCampeonatoId = owner,
        EsporteId = Guid.NewGuid(),
        Nome = "Copa Kivo",
        DataInicio = DateTime.Today.AddDays(5),
        DataFim = DateTime.Today.AddDays(20),
        PontosVitoria = 3,
        PontosDerrota = 0,
        PontosEmpate = 1,
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos
    };

    private static EditarCampeonatoDto EditarCampeonatoDto() => new()
    {
        EsporteId = Guid.NewGuid(),
        Nome = "Copa Atualizada",
        DataInicio = DateTime.Today.AddDays(5),
        DataFim = DateTime.Today.AddDays(20),
        PontosVitoria = 3,
        PontosDerrota = 0,
        PontosEmpate = 1,
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos
    };

    private static CriarIngressoLoteDTO CriarIngressoLoteDto(Guid? partidaId = null) => new()
    {
        PartidaId = partidaId ?? Guid.NewGuid(),
        NomeLote = "Arquibancada",
        Preco = 30,
        QuantidadeTotal = 100
    };

    private static EnderecoDto EnderecoDto() => new()
    {
        Cep = "01001000",
        Rua = "Rua Boa Vista",
        Numero = "100",
        Cidade = "Sao Paulo",
        Estado = "SP",
        Pais = "Brasil"
    };

}
