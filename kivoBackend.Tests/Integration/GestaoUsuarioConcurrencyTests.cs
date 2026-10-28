using System.Security.Claims;
using System.Data.Common;
using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Data;
using kivoBackend.Infrastructure.Repositories;
using kivoBackend.Presentation.Auth;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace kivoBackend.Tests.Integration;

// SQLite proves relational rollback/session rules, not SQL Server lock hints or parallelism.
public class GestaoUsuarioConcurrencyTests
{
    private sealed class Fixture : IAsyncDisposable
    {
        public readonly DbConnection Connection;
        private readonly string? ownedDatabase;
        public string ConnectionString => Connection.ConnectionString;
        public readonly AppDbContext Db;
        private readonly ServiceProvider provider;
        public UserManager<IdentityUser> Manager => provider.GetRequiredService<UserManager<IdentityUser>>();
        public UsuarioService Service => new(new RepositoryGenerics<Usuario>(Db), new RepositoryUsuario(Db), Manager, new FakeEmailService(), new InMemoryRepo<CodigoReativacao>(), new FakeVerificationCodeService(), new FakeNotificacaoService(), Db);
        public Fixture(IInterceptor? interceptor = null, string? sqlConnection = null, bool createDatabase = true)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>();
            if (sqlConnection != null)
            {
                var builder = new SqlConnectionStringBuilder(sqlConnection);
                if (createDatabase) { ownedDatabase = "KivoT15Test_" + Guid.NewGuid().ToString("N"); builder.InitialCatalog = ownedDatabase; }
                Connection = new SqlConnection(builder.ConnectionString);
                options.UseSqlServer(builder.ConnectionString, sql => sql.EnableRetryOnFailure(3));
            }
            else { Connection = new SqliteConnection("DataSource=:memory:"); Connection.Open(); options.UseSqlite(Connection); }
            if (interceptor != null) options.AddInterceptors(interceptor);
            Db = new AppDbContext(options.Options);
            if (createDatabase) Db.Database.EnsureCreated();
            var services = new ServiceCollection().AddLogging().AddSingleton(Db);
            services.AddDataProtection();
            services.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
            provider = services.BuildServiceProvider();
        }
        public async Task<Usuario> User(EnumCargo cargo = EnumCargo.Administrador)
        {
            var user = new Usuario { Id = Guid.NewGuid(), Nome = "Ana", Email = $"{Guid.NewGuid():N}@test.local", Cpf = Guid.NewGuid().ToString("N"), Telefone = "11999999999", DataNascimento = new(1990,1,1), Ativo = true, EnumCargo = cargo };
            var role = cargo.ToString();
            if (!await Db.Roles.AnyAsync(r => r.Name == role)) await provider.GetRequiredService<RoleManager<IdentityRole>>().CreateAsync(new IdentityRole(role));
            var identity = new IdentityUser { Email = user.Email, UserName = user.Email };
            Assert.True((await Manager.CreateAsync(identity, "Teste123!")).Succeeded);
            Assert.True((await Manager.AddToRoleAsync(identity, role)).Succeeded);
            Db.Usuarios.Add(user); await Db.SaveChangesAsync();
            return user;
        }
        public ClaimsPrincipal Principal(Usuario u, int? version = null) => new(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim("Cargo", u.EnumCargo.ToString()), new Claim(ClaimTypes.Role, u.EnumCargo.ToString()),
            new Claim(UsuarioSessionValidation.VersionClaim, (version ?? u.VersaoSessao).ToString()) }, "test"));
        public async ValueTask DisposeAsync()
        {
            if (ownedDatabase != null && ownedDatabase.StartsWith("KivoT15Test_", StringComparison.Ordinal) && new SqlConnectionStringBuilder(Connection.ConnectionString).InitialCatalog == ownedDatabase)
                await Db.Database.EnsureDeletedAsync();
            await provider.DisposeAsync(); await Db.DisposeAsync(); await Connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task BloquearRestaurar_NaoRessuscitaSessaoNemRemoveIdentidade()
    {
        await using var f = new Fixture(); var admin = await f.User(); var target = await f.User(EnumCargo.Torcedor);
        var old = f.Principal(target); Assert.True(await UsuarioSessionValidation.Valida(f.Db, old));
        await f.Service.AlterarEstadoAdministrativo(target.Id, admin.Id, "bloquear");
        Assert.False(await UsuarioSessionValidation.Valida(f.Db, old));
        await f.Service.AlterarEstadoAdministrativo(target.Id, admin.Id, "desbloquear");
        Assert.False(await UsuarioSessionValidation.Valida(f.Db, old));
        var restored = await f.Db.Usuarios.SingleAsync(u => u.Id == target.Id);
        Assert.True(restored.Ativo); Assert.Equal(3, restored.VersaoSessao);
        Assert.NotNull(await f.Manager.FindByEmailAsync(target.Email));
        Assert.True(await UsuarioSessionValidation.Valida(f.Db, f.Principal(restored)));
    }
    [Fact]
    public async Task TokenSemVersaoOuCargoDivergente_Rejeitado()
    {
        await using var f = new Fixture(); var user = await f.User(); var claims = f.Principal(user);
        ((ClaimsIdentity)claims.Identity!).RemoveClaim(claims.FindFirst(UsuarioSessionValidation.VersionClaim)!);
        Assert.False(await UsuarioSessionValidation.Valida(f.Db, claims));
        var old = f.Principal(user); user.EnumCargo = EnumCargo.Torcedor; await f.Db.SaveChangesAsync();
        Assert.False(await UsuarioSessionValidation.Valida(f.Db, old));
    }
    [Fact]
    public async Task PerfilSemRoleDestino_FalhaRevertePerfilCargoVersaoERoles()
    {
        await using var f = new Fixture(); var admin = await f.User(); var target = await f.User();
        await Assert.ThrowsAnyAsync<Exception>(() => f.Service.AlterarCargoAdministrativo(target.Id, admin.Id, new() { Cargo = EnumCargo.OrganizadorTime, DadosPerfil = new() { Endereco = new() { Cep = "12345678", Rua = "Rua", Numero = "1", Cidade = "Cidade", Estado = "SC", Pais = "Brasil" } } }));
        var saved = await f.Db.Usuarios.SingleAsync(u => u.Id == target.Id);
        Assert.Equal(EnumCargo.Administrador, saved.EnumCargo); Assert.Equal(1, saved.VersaoSessao); Assert.Empty(f.Db.OrganizadoresTime);
        Assert.Equal(new[] { "Administrador" }, await f.Manager.GetRolesAsync((await f.Manager.FindByEmailAsync(target.Email))!));
    }
    [Fact]
    public async Task AutoExclusaoEUltimoAdmin_NaoPermitemPerderTodosAdministradores()
    {
        await using var f = new Fixture(); var a = await f.User(); var b = await f.User();
        await Assert.ThrowsAsync<GestaoUsuarioException>(() => f.Service.AlterarEstadoAdministrativo(a.Id, a.Id, "excluir"));
        await f.Service.AlterarEstadoAdministrativo(b.Id, a.Id, "bloquear");
        await Assert.ThrowsAsync<GestaoUsuarioException>(() => f.Service.AlterarEstadoAdministrativo(a.Id, b.Id, "bloquear"));
        Assert.Equal(1, await f.Db.Usuarios.CountAsync(u => u.Ativo && u.EnumCargo == EnumCargo.Administrador));
    }
    [Theory]
    [InlineData(EnumEstadoAdministrativo.Bloqueado)] [InlineData(EnumEstadoAdministrativo.Excluido)]
    public async Task CodigoAntigo_NaoReativaRestricao(EnumEstadoAdministrativo estado)
    {
        await using var f = new Fixture(); var u = await f.User(EnumCargo.Torcedor); u.Ativo = false; u.EstadoAdministrativo = estado; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.ConfirmarReativacao(u.Email, "123456"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Service.ConfirmarRecuperacaoSenha(u.Email, "123456", "Teste123!"));
        Assert.False((await f.Db.Usuarios.SingleAsync()).Ativo);
    }
    private sealed class FailCommit : DbTransactionInterceptor
    {
        public bool Fail;
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => Fail ? throw new SqliteException("simulated commit failure", 5) : ValueTask.FromResult(result);
    }
    [Fact]
    public async Task Filtro_CommitFalhaAposAction_NuncaRetornaSucesso()
    {
        var interceptor = new FailCommit(); await using var f = new Fixture(interceptor); var user = await f.User();
        var http = new DefaultHttpContext { User = f.Principal(user) }; http.Request.Method = "PATCH";
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var executing = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        var executed = new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()) { Result = new OkResult() };
        interceptor.Fail = true;
        await new UsuarioResponsabilidadeFilter(f.Db).OnActionExecutionAsync(executing, () => Task.FromResult(executed));
        Assert.IsType<ConflictObjectResult>(executed.Result);
    }
    private sealed class SqlServerFactAttribute : FactAttribute
    {
        public SqlServerFactAttribute() { if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("T15_TEST_SQLSERVER"))) Skip = "Requires explicitly provisioned SQL Server test connection."; }
    }
    [SqlServerFact]
    public async Task SqlServer_ConcorrenciaAdmins_PreservaAdministradorEfetivo()
    {
        await using var f = new Fixture(sqlConnection: Environment.GetEnvironmentVariable("T15_TEST_SQLSERVER"));
        var a = await f.User(); var b = await f.User();
        await using var other = new Fixture(sqlConnection: f.ConnectionString, createDatabase: false);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Block(Fixture fixture, Guid target, Guid actor)
        {
            await start.Task;
            try { await fixture.Service.AlterarEstadoAdministrativo(target, actor, "bloquear"); return true; }
            catch (GestaoUsuarioException) { return false; }
        }
        var first = Block(f, a.Id, b.Id); var second = Block(other, b.Id, a.Id); start.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Single(results.Where(ok => ok));
        Assert.Equal(1, await f.Db.Usuarios.AsNoTracking().CountAsync(u => u.Ativo && u.EnumCargo == EnumCargo.Administrador && u.EstadoAdministrativo == EnumEstadoAdministrativo.Normal));
    }
    [SqlServerFact]
    public async Task SqlServer_CriarTimeVersusTrocarCargo_NaoDeixaVinculoIncompativel()
    {
        await using var f = new Fixture(sqlConnection: Environment.GetEnvironmentVariable("T15_TEST_SQLSERVER"));
        var admin = await f.User(); var owner = await f.User(EnumCargo.OrganizadorTime);
        owner.OrganizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = owner.Id, Endereco = new Endereco { Cep = "12345678", Rua = "Rua", Numero = "1", Cidade = "Cidade", Estado = "SC", Pais = "Brasil" } };
        f.Db.OrganizadoresTime.Add(owner.OrganizadorTime);
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Teste", Icone = "teste" };
        f.Db.Esportes.Add(esporte);
        await f.Db.SaveChangesAsync();
        // Ensure destination role exists; test the responsibility race rather than an Identity failure.
        f.Db.Roles.Add(new IdentityRole("Torcedor") { NormalizedName = "TORCEDOR" }); await f.Db.SaveChangesAsync();
        await using var other = new Fixture(sqlConnection: f.ConnectionString, createDatabase: false);
        var principal = f.Principal(owner);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task Create()
        {
            await start.Task;
            var http = new DefaultHttpContext { User = principal }; http.Request.Method = "POST";
            var ac = new ActionContext(http, new RouteData(), new ActionDescriptor());
            var executing = new ActionExecutingContext(ac, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
            await new UsuarioResponsabilidadeFilter(other.Db).OnActionExecutionAsync(executing, async () =>
            {
                other.Db.Times.Add(new Time { Id = Guid.NewGuid(), OrganizadorTimeId = owner.OrganizadorTime.Id, EsporteId = esporte.Id, Nome = "Time", Cidade = "Cidade", Estado = "SC", Ativo = true });
                await other.Db.SaveChangesAsync();
                return new ActionExecutedContext(ac, new List<IFilterMetadata>(), new object()) { Result = new OkResult() };
            });
        }
        async Task Demote()
        {
            await start.Task;
            try { await f.Service.AlterarCargoAdministrativo(owner.Id, admin.Id, new() { Cargo = EnumCargo.Torcedor }); }
            catch (GestaoUsuarioException ex) { Assert.Equal("management_links", ex.Code); }
        }
        var create = Create(); var demote = Demote(); start.SetResult(); await Task.WhenAll(create, demote);
        var persisted = await f.Db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == owner.Id);
        Assert.False(persisted.EnumCargo == EnumCargo.Torcedor && await f.Db.Times.AsNoTracking().AnyAsync(t => t.OrganizadorTimeId == owner.OrganizadorTime.Id && t.Ativo));
    }
    [SqlServerFact]
    public async Task SqlServer_RollbackPreservaCargoPerfilRoles()
    {
        await using var f = new Fixture(sqlConnection: Environment.GetEnvironmentVariable("T15_TEST_SQLSERVER"));
        var a = await f.User(); var target = await f.User();
        await Assert.ThrowsAnyAsync<Exception>(() => f.Service.AlterarCargoAdministrativo(target.Id, a.Id, new() { Cargo = EnumCargo.OrganizadorTime, DadosPerfil = new() { Endereco = new() { Cep = "12345678", Rua = "Rua", Numero = "1", Cidade = "Cidade", Estado = "SC", Pais = "Brasil" } } }));
        Assert.Equal(EnumCargo.Administrador, (await f.Db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == target.Id)).EnumCargo);
        Assert.Empty(f.Db.OrganizadoresTime);
        Assert.Equal(new[] { "Administrador" }, await f.Manager.GetRolesAsync((await f.Manager.FindByEmailAsync(target.Email))!));
    }

    [SqlServerFact]
    public async Task SqlServer_TrocaBemSucedida_InserePerfisEnderecoBancoERolePreservandoHistorico()
    {
        await using var f = new Fixture(sqlConnection: Environment.GetEnvironmentVariable("T15_TEST_SQLSERVER"));
        var admin = await f.User(); var target = await f.User();
        foreach (var cargo in new[] { EnumCargo.OrganizadorTime, EnumCargo.OrganizadorCampeonato, EnumCargo.Torcedor })
            f.Db.Roles.Add(new IdentityRole(cargo.ToString()) { NormalizedName = cargo.ToString().ToUpperInvariant() });
        await f.Db.SaveChangesAsync();
        var old = f.Principal(target);
        var dados = new DadosPerfilGestaoDTO {
            Endereco = new() { Cep = "12345678", Rua = "Rua", Numero = "1", Cidade = "Cidade", Estado = "SC", Pais = "Brasil" },
            ContaBanco = new() { Banco = "Banco", Agencia = "1", Conta = "1", ChavePix = "pix@test.local" }
        };
        foreach (var cargo in new[] { EnumCargo.OrganizadorTime, EnumCargo.OrganizadorCampeonato, EnumCargo.Torcedor })
            await f.Service.AlterarCargoAdministrativo(target.Id, admin.Id, new() { Cargo = cargo, DadosPerfil = dados });
        f.Db.ChangeTracker.Clear();
        var saved = await f.Db.Usuarios.Include(u => u.Torcedor).Include(u => u.OrganizadorTime).Include(u => u.OrganizadorCampeonato).ThenInclude(p => p.ContaBanco).SingleAsync(u => u.Id == target.Id);
        Assert.Equal(EnumCargo.Torcedor, saved.EnumCargo); Assert.Equal(4, saved.VersaoSessao);
        Assert.NotNull(saved.Torcedor); Assert.NotNull(saved.OrganizadorTime); Assert.NotNull(saved.OrganizadorCampeonato.ContaBanco);
        Assert.Equal(1, await f.Db.Enderecos.CountAsync());
        Assert.False(await UsuarioSessionValidation.Valida(f.Db, old));
        Assert.Equal(new[] { "Torcedor" }, await f.Manager.GetRolesAsync((await f.Manager.FindByEmailAsync(target.Email))!));
    }

}
