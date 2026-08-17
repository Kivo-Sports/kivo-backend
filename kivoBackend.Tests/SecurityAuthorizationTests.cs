using System.Linq.Expressions;
using kivoBackend.Application.DTO;
using kivoBackend.Application.Interfaces;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Presentation.Auth;
using kivoBackend.Presentation.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    private static Usuario UsuarioTorcedor(Guid userId) => new()
    {
        Id = userId,
        Nome = "Ana Silva",
        Email = "ana@example.test",
        Cpf = "12345678901",
        Telefone = "11999999999",
        DataNascimento = new DateTime(1994, 3, 10),
        EnumCargo = EnumCargo.Torcedor,
        Ativo = true,
        Torcedor = new Torcedor { Id = Guid.NewGuid(), Endereco = new Endereco() }
    };

    private static Usuario UsuarioOrganizadorTime(Guid userId, Guid organizadorTimeId) => new()
    {
        Id = userId,
        Nome = "Bruno Costa",
        Email = "bruno@example.test",
        Cpf = "12345678902",
        Telefone = "11988888888",
        DataNascimento = new DateTime(1988, 6, 8),
        EnumCargo = EnumCargo.OrganizadorTime,
        Ativo = true,
        OrganizadorTime = new OrganizadorTime { Id = organizadorTimeId }
    };

    private static Usuario UsuarioOrganizadorCampeonato(Guid userId, Guid organizadorCampeonatoId) => new()
    {
        Id = userId,
        Nome = "Carla Mendes",
        Email = "carla@example.test",
        Cpf = "12345678903",
        Telefone = "11977777777",
        DataNascimento = new DateTime(1985, 9, 15),
        EnumCargo = EnumCargo.OrganizadorCampeonato,
        Ativo = true,
        OrganizadorCampeonato = new OrganizadorCampeonato { Id = organizadorCampeonatoId }
    };

    private static Campeonato Campeonato(Guid ownerId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizadorCampeonatoId = ownerId,
        EsporteId = Guid.NewGuid(),
        Nome = "Copa Teste",
        DataInicio = DateTime.Today.AddDays(5),
        DataFim = DateTime.Today.AddDays(20),
        EnumStatusCampeonato = EnumStatusCampeonato.Rascunho,
        FormatoCampeonato = EnumFormatoCampeonato.PontosCorridos,
        CampeonatoTimes = new List<CampeonatoTime>()
    };

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public FakeCurrentUser(Guid? userId, bool isAdmin = false)
        {
            UserId = userId;
            IsAdmin = isAdmin;
        }

        public bool IsAuthenticated => UserId.HasValue;
        public Guid? UserId { get; }
        public bool IsAdmin { get; }
        public bool IsInRole(string role) => IsAdmin && role is "Administrador" or "Admin";
    }

    private sealed class FakeUsuarioService : IUsuarioService
    {
        public Dictionary<Guid, Usuario> Users { get; } = new();
        public bool RemoveCalled { get; private set; }
        public Task<Usuario> CriarUsuario(Usuario usuario, string senha) => Task.FromResult(usuario);
        public Task<Usuario> ObterUsuarioPorId(Guid id) => Task.FromResult(Users[id]);
        public Task<IEnumerable<Usuario>> ObterTodosUsuarios() => Task.FromResult<IEnumerable<Usuario>>(Users.Values);
        public Task<IEnumerable<Usuario>> ObterAdministradores() => Task.FromResult<IEnumerable<Usuario>>(Array.Empty<Usuario>());
        public Task<Usuario> EditarDadosUsuario(Guid id, Usuario usuario) => Task.FromResult(Users[id]);
        public Task<Usuario> RemoverUsuario(Guid id) { RemoveCalled = true; return Task.FromResult(Users[id]); }
        public Task<Usuario?> ObterUsuarioPorCpf(string cpf) => Task.FromResult<Usuario?>(null);
        public Task<Usuario?> ObterUsuarioPorEmail(string email) => Task.FromResult<Usuario?>(null);
        public void InicializarPerfilPorCargo(Usuario usuario) { }
        public Task DesativarConta(Usuario usuario) => Task.CompletedTask;
        public Task AtivarConta(Guid id) => Task.CompletedTask;
        public Task GerarCodigoReativacao(string email) => Task.CompletedTask;
        public Task ConfirmarReativacao(string email, string codigo) => Task.CompletedTask;
        public Task GerarCodigoRecuperacaoSenha(string email) => Task.CompletedTask;
        public Task ConfirmarRecuperacaoSenha(string email, string codigo, string novaSenha) => Task.CompletedTask;
        public Task RedefinirSenha(string email, string senhaAtual, string novaSenha) => Task.CompletedTask;
        public Task<bool> VerificarEmailExiste(string email) => Task.FromResult(false);
        public Task<bool> VerificarCpfExiste(string cpf) => Task.FromResult(false);
    }

    private sealed class FakeTimeService : ITimeService
    {
        public Time? Current { get; init; }
        public Time? Added { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool RemoveCalled { get; private set; }
        public Task<IEnumerable<Time>> ObterTodos() => Task.FromResult<IEnumerable<Time>>(Current == null ? Array.Empty<Time>() : new[] { Current });
        public Task<Time?> ObterPorId(Guid id) => Task.FromResult(Current);
        public Task<Time> Adicionar(Time entidade) { Added = entidade; return Task.FromResult(entidade); }
        public Task Atualizar(Time entidade) { UpdateCalled = true; return Task.CompletedTask; }
        public Task Remover(Guid id) { RemoveCalled = true; return Task.CompletedTask; }
    }

    private sealed class FakeCampeonatoService : ICampeonatoService
    {
        public Campeonato? Current { get; init; }
        public Campeonato? Added { get; private set; }
        public Guid? ExpectedInviteOwner { get; init; }
        public bool Mutated { get; private set; }
        public Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes() => Task.FromResult<IEnumerable<Campeonato>>(Current == null ? Array.Empty<Campeonato>() : new[] { Current });
        public Task<Campeonato> ObterCampeonatoPorId(Guid id) => Task.FromResult(Current ?? Campeonato(Guid.NewGuid()));
        public Task AdicionarTimeAoCampeonato(Guid campeonatoId, Guid timeId) { Mutated = true; return Task.CompletedTask; }
        public Task<Campeonato> EditarCampeonato(Guid campeonatoId, EditarCampeonatoDto editarCampeonatoDto, bool ehAdmin = false) { Mutated = true; return Task.FromResult(Current!); }
        public Task DeletarCampeonatoAdmin(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
        public Task DescancelarCampeonato(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
        public Task ReatribuirCampeonato(Guid campeonatoId, Guid novoOrganizadorCampeonatoId) { Mutated = true; return Task.CompletedTask; }
        public Task RemoverTimeDoCampeonato(Guid campeonatoId, Guid timeId) { Mutated = true; return Task.CompletedTask; }
        public Task ResponderConviteCampeonato(Guid ParticipacaoId, Guid OrganizadorTimeId, bool aceito)
        {
            if (ExpectedInviteOwner.HasValue && OrganizadorTimeId != ExpectedInviteOwner.Value)
                throw new UnauthorizedAccessException();
            Mutated = true;
            return Task.CompletedTask;
        }
        public Task<IEnumerable<CampeonatoTime>> ObterConvitesPorOrganizador(Guid organizadorTimeId) => Task.FromResult<IEnumerable<CampeonatoTime>>(Array.Empty<CampeonatoTime>());
        public Task<IEnumerable<CampeonatoTime>> ObterConvitesPorCampeonato(Guid campeonatoId) => Task.FromResult<IEnumerable<CampeonatoTime>>(Array.Empty<CampeonatoTime>());
        public Task<IEnumerable<Campeonato>> ObterTodosComTimes() => Task.FromResult<IEnumerable<Campeonato>>(Array.Empty<Campeonato>());
        public Task AbrirInscricoes(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
        public Task<Campeonato> IniciarCampeonato(Guid campeonatoId) { Mutated = true; return Task.FromResult(Current!); }
        public Task CancelarCampeonato(Guid campeonatoId) { Mutated = true; return Task.CompletedTask; }
        public Task<IEnumerable<Campeonato>> ObterTodos() => Task.FromResult<IEnumerable<Campeonato>>(Array.Empty<Campeonato>());
        public Task<Campeonato?> ObterPorId(Guid id) => Task.FromResult(Current);
        public Task<Campeonato> Adicionar(Campeonato entidade) { Added = entidade; return Task.FromResult(entidade); }
        public Task Atualizar(Campeonato entidade) { Mutated = true; return Task.CompletedTask; }
        public Task Remover(Guid id) { Mutated = true; return Task.CompletedTask; }
    }

    private sealed class FakeIngressoLoteService : IIngressoLoteService
    {
        public bool CreateCalled { get; private set; }
        public Task<IngressoLote> CriarLote(CriarIngressoLoteDTO dto, Guid? organizadorCampeonatoId, bool ehAdmin)
        {
            CreateCalled = true;
            return Task.FromResult(new IngressoLote { Id = Guid.NewGuid(), PartidaId = dto.PartidaId, NomeLote = dto.NomeLote, Preco = dto.Preco, QuantidadeTotal = dto.QuantidadeTotal, QuantidadeDisponivel = dto.QuantidadeTotal, Ativo = true });
        }
        public Task<IEnumerable<IngressoLote>> ObterLotesPorPartida(Guid partidaId) => Task.FromResult<IEnumerable<IngressoLote>>(Array.Empty<IngressoLote>());
    }

    private sealed class FakeStorageService : IStorageService
    {
        public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType) => Task.FromResult("https://example.test/upload.png");
    }

    private class EmptyRepo<T> : IRepositoryGenerics<T> where T : class
    {
        public virtual Task<IEnumerable<T>> ObterTodos() => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        public virtual Task<T?> ObterPorId(Guid id) => Task.FromResult<T?>(null);
        public virtual Task<T> Adicionar(T entidade) => Task.FromResult(entidade);
        public virtual Task Atualizar(T entidade) => Task.CompletedTask;
        public virtual Task Remover(Guid id) => Task.CompletedTask;
        public virtual Task<IEnumerable<T>> ObterComIncludes(params Expression<Func<T, object>>[] includes) => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        public virtual Task<IEnumerable<T>> Buscar(Expression<Func<T, bool>> predicate) => Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        public virtual Task<T?> BuscarPrimeiro(Expression<Func<T, bool>> predicate) => Task.FromResult<T?>(null);
    }

    private sealed class InMemoryRepo<T> : EmptyRepo<T> where T : class
    {
        private readonly List<T> _items;
        public InMemoryRepo(params T[] items) => _items = items.ToList();
        public override string ToString() => $"{_items.Count}";
        public override Task<T?> ObterPorId(Guid id)
        {
            var property = typeof(T).GetProperty("Id");
            return Task.FromResult(_items.FirstOrDefault(x => property?.GetValue(x) is Guid value && value == id));
        }
        public override Task<T> Adicionar(T entidade)
        {
            _items.Add(entidade);
            return Task.FromResult(entidade);
        }
    }

    private sealed class FakeCampeonatoRepository : EmptyRepo<Campeonato>, IRepositoryCampeonato
    {
        private readonly Campeonato _campeonato;
        public FakeCampeonatoRepository(Campeonato campeonato) => _campeonato = campeonato;
        public Task<Campeonato> ObterCampeonatoPorId(Guid id) => Task.FromResult(_campeonato);
        public Task<IEnumerable<Campeonato>> ObterCampeonatosComTimes() => Task.FromResult<IEnumerable<Campeonato>>(new[] { _campeonato });
    }
}
