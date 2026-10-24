using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Infrastructure.Repositories;
using kivoBackend.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace kivoBackend.Tests.Integration.Repositories;

/// <summary>
/// Repository integration tests against a real (SQLite in-memory) EF Core
/// provider — covers custom queries, eager-loading Includes, ownership/status
/// filters, and the unique-index constraints declared in AppDbContext, none
/// of which the hand-rolled in-memory fakes used elsewhere can exercise.
/// </summary>
public class RepositoryTests
{
    [Fact]
    public async Task RepositoryGenerics_CrudRoundTrip()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Esporte>(context);
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer", Ativo = true };

        await repo.Adicionar(esporte);
        var encontrado = await repo.ObterPorId(esporte.Id);
        Assert.NotNull(encontrado);
        Assert.Equal("Futebol", encontrado!.Nome);

        encontrado.Nome = "Futsal";
        await repo.Atualizar(encontrado);
        await using var contextoLeitura = db.NewContext();
        var repoLeitura = new RepositoryGenerics<Esporte>(contextoLeitura);
        Assert.Equal("Futsal", (await repoLeitura.ObterPorId(esporte.Id))!.Nome);

        await repoLeitura.Remover(esporte.Id);
        await using var contextoFinal = db.NewContext();
        Assert.Null(await new RepositoryGenerics<Esporte>(contextoFinal).ObterPorId(esporte.Id));
    }

    [Fact]
    public async Task RepositoryGenerics_Remover_IdInexistente_Lanca()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Esporte>(context);

        await Assert.ThrowsAsync<Exception>(() => repo.Remover(Guid.NewGuid()));
    }

    [Fact]
    public async Task RepositoryGenerics_Buscar_FiltraPeloPredicado()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Esporte>(context);
        await repo.Adicionar(new Esporte { Id = Guid.NewGuid(), Nome = "Ativo", Icone = "mdi:soccer", Ativo = true });
        await repo.Adicionar(new Esporte { Id = Guid.NewGuid(), Nome = "Inativo", Icone = "mdi:soccer", Ativo = false });

        var ativos = await repo.Buscar(e => e.Ativo);

        Assert.Single(ativos);
        Assert.Equal("Ativo", ativos.First().Nome);
    }

    [Fact]
    public async Task RepositoryCampeonato_ObterCampeonatoPorId_CarregaTimesEOrganizador()
    {
        using var db = SqliteDbContextFactory.Create();
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer" };
        var endereco = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua Teste", Numero = "100", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = "Organizador", Email = "org@test.com", Cpf = "11111111111", Telefone = "119", DataNascimento = DateTime.Today, EnumCargo = EnumCargo.OrganizadorCampeonato, Ativo = true };
        var organizador = new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = usuario.Id, EnderecoId = endereco.Id };
        var enderecoOrgTime = new Endereco { Id = Guid.NewGuid(), Cep = "02000-000", Rua = "Rua Time", Numero = "200", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var usuarioOrgTime = new Usuario { Id = Guid.NewGuid(), Nome = "Organizador Time", Email = "orgtime@test.com", Cpf = "22222222222", Telefone = "119", DataNascimento = DateTime.Today, EnumCargo = EnumCargo.OrganizadorTime, Ativo = true };
        var organizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuarioOrgTime.Id, EnderecoId = enderecoOrgTime.Id };
        var time = new Time { Id = Guid.NewGuid(), Nome = "Time A", Cidade = "SP", Estado = "SP", EsporteId = esporte.Id, OrganizadorTimeId = organizadorTime.Id, Ativo = true };
        var campeonato = new Campeonato { Id = Guid.NewGuid(), Nome = "Copa", OrganizadorCampeonatoId = organizador.Id, EsporteId = esporte.Id, DataInicio = DateTime.Today, DataFim = DateTime.Today.AddDays(10) };
        var vinculo = new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id, EnumStatusParticipacao = EnumStatusParticipacao.Aceito };

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(endereco);
            setup.Enderecos.Add(enderecoOrgTime);
            setup.Usuarios.Add(usuario);
            setup.Usuarios.Add(usuarioOrgTime);
            setup.OrganizadoresCampeonato.Add(organizador);
            setup.OrganizadoresTime.Add(organizadorTime);
            setup.Esportes.Add(esporte);
            setup.Times.Add(time);
            setup.Campeonatos.Add(campeonato);
            setup.CampeonatoTimes.Add(vinculo);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryCampeonato(context);

        var resultado = await repo.ObterCampeonatoPorId(campeonato.Id);

        Assert.NotNull(resultado);
        Assert.Single(resultado.CampeonatoTimes);
        Assert.Equal(time.Id, resultado.CampeonatoTimes.First().TimeId);
        Assert.Equal(time.Nome, resultado.CampeonatoTimes.First().Time!.Nome);
        Assert.NotNull(resultado.OrganizadorCampeonato);
    }

    [Fact]
    public async Task RepositoryUsuario_ObterTodosUsuarios_RetornaApenasAtivos()
    {
        using var db = SqliteDbContextFactory.Create();
        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(UsuarioSimples(ativo: true, cpf: "22222222222"));
            setup.Usuarios.Add(UsuarioSimples(ativo: false, cpf: "33333333333"));
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        var usuarios = await repo.ObterTodosUsuarios();

        Assert.Single(usuarios);
        Assert.True(usuarios.First().Ativo);
    }

    [Fact]
    public async Task RepositoryUsuario_ObterAdministradores_FiltraPorCargo()
    {
        using var db = SqliteDbContextFactory.Create();
        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(UsuarioSimples(ativo: true, cpf: "44444444444", cargo: EnumCargo.Administrador));
            setup.Usuarios.Add(UsuarioSimples(ativo: true, cpf: "55555555555", cargo: EnumCargo.Torcedor));
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        var admins = await repo.ObterAdministradores();

        Assert.Single(admins);
        Assert.Equal(EnumCargo.Administrador, admins.First().EnumCargo);
    }

    [Fact]
    public async Task Usuario_CpfDuplicado_ViolaIndiceUnico()
    {
        using var db = SqliteDbContextFactory.Create();
        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(UsuarioSimples(ativo: true, cpf: "66666666666"));
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        context.Usuarios.Add(UsuarioSimples(ativo: true, cpf: "66666666666"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CampeonatoTime_ConviteDuplicado_ViolaIndiceUnico()
    {
        using var db = SqliteDbContextFactory.Create();
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer" };
        var usuario = UsuarioSimples(ativo: true, cpf: "77777777777", cargo: EnumCargo.OrganizadorCampeonato);
        var organizador = new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = usuario.Id, EnderecoId = Guid.NewGuid() };
        var usuarioOrgTime = UsuarioSimples(ativo: true, cpf: "88888888888", cargo: EnumCargo.OrganizadorTime);
        var organizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuarioOrgTime.Id, EnderecoId = Guid.NewGuid() };
        var time = new Time { Id = Guid.NewGuid(), Nome = "Time A", Cidade = "SP", Estado = "SP", EsporteId = esporte.Id, OrganizadorTimeId = organizadorTime.Id, Ativo = true };
        var campeonato = new Campeonato { Id = Guid.NewGuid(), Nome = "Copa", OrganizadorCampeonatoId = organizador.Id, EsporteId = esporte.Id, DataInicio = DateTime.Today, DataFim = DateTime.Today.AddDays(10) };

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(new Endereco { Id = organizador.EnderecoId, Cep = "01000-000", Rua = "Rua Teste", Numero = "100", Cidade = "SP", Estado = "SP", Pais = "Brasil" });
            setup.Enderecos.Add(new Endereco { Id = organizadorTime.EnderecoId, Cep = "02000-000", Rua = "Rua Time", Numero = "200", Cidade = "SP", Estado = "SP", Pais = "Brasil" });
            setup.Usuarios.Add(usuario);
            setup.Usuarios.Add(usuarioOrgTime);
            setup.OrganizadoresCampeonato.Add(organizador);
            setup.OrganizadoresTime.Add(organizadorTime);
            setup.Esportes.Add(esporte);
            setup.Times.Add(time);
            setup.Campeonatos.Add(campeonato);
            setup.CampeonatoTimes.Add(new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id });
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        context.CampeonatoTimes.Add(new CampeonatoTime { Id = Guid.NewGuid(), CampeonatoId = campeonato.Id, TimeId = time.Id });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task RepositoryUsuario_ObterUsuarioPorCpf_CarregaPerfilEEndereco()
    {
        using var db = SqliteDbContextFactory.Create();
        var endereco = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var torcedor = new Torcedor { Id = Guid.NewGuid(), EnderecoId = endereco.Id };
        var usuario = UsuarioSimples(ativo: true, cpf: "88888888888");
        usuario.Torcedor = torcedor;

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(endereco);
            setup.Usuarios.Add(usuario);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        var resultado = await repo.ObterUsuarioPorCpf("88888888888");

        Assert.NotNull(resultado);
        Assert.NotNull(resultado!.Torcedor);
        Assert.Equal(endereco.Cep, resultado.Torcedor!.Endereco!.Cep);
    }

    [Fact]
    public async Task RepositoryUsuario_ObterUsuarioPorCpf_Inexistente_RetornaNull()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        Assert.Null(await repo.ObterUsuarioPorCpf("00000000000"));
    }

    [Fact]
    public async Task RepositoryUsuario_ObterUsuarioPorEmail_EncontraPorEmailExato()
    {
        using var db = SqliteDbContextFactory.Create();
        var usuario = UsuarioSimples(ativo: true, cpf: "99999999999");
        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(usuario);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        var resultado = await repo.ObterUsuarioPorEmail(usuario.Email);

        Assert.NotNull(resultado);
        Assert.Equal(usuario.Id, resultado!.Id);
    }

    [Fact]
    public async Task RepositoryUsuario_ObterUsuarioPorId_Inexistente_RetornaNull()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryUsuario(context);

        Assert.Null(await repo.ObterUsuarioPorId(Guid.NewGuid()));
    }

    [Fact]
    public async Task RepositoryTime_ObterTodos_IncluiEsporte()
    {
        using var db = SqliteDbContextFactory.Create();
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Basquete", Icone = "mdi:basketball" };
        var enderecoOrgTime = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var usuarioOrgTime = UsuarioSimples(ativo: true, cpf: "10101010101", cargo: EnumCargo.OrganizadorTime);
        var organizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuarioOrgTime.Id, EnderecoId = enderecoOrgTime.Id };
        var time = new Time { Id = Guid.NewGuid(), Nome = "Time X", Cidade = "SP", Estado = "SP", EsporteId = esporte.Id, OrganizadorTimeId = organizadorTime.Id, Ativo = true };

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(enderecoOrgTime);
            setup.Usuarios.Add(usuarioOrgTime);
            setup.OrganizadoresTime.Add(organizadorTime);
            setup.Esportes.Add(esporte);
            setup.Times.Add(time);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryTime(context);

        var todos = await repo.ObterTodos();

        Assert.Single(todos);
        Assert.Equal("Basquete", todos.First().Esporte!.Nome);
    }

    [Fact]
    public async Task RepositoryTime_ObterPorId_Inexistente_RetornaNull()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryTime(context);

        Assert.Null(await repo.ObterPorId(Guid.NewGuid()));
    }

    [Fact]
    public async Task VerificationCodeRepository_ObterCodigoValidoAsync_FiltraPorUsuarioTipoEValidade()
    {
        using var db = SqliteDbContextFactory.Create();
        var usuario = UsuarioSimples(ativo: true, cpf: "12121212121");
        var codigoValido = new VerificationCode
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Codigo = "hash-valido",
            Tipo = VerificationCodeType.PasswordReset,
            Usado = false,
            ExpiraEm = DateTime.Now.AddMinutes(5)
        };
        var codigoExpirado = new VerificationCode
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            Codigo = "hash-expirado",
            Tipo = VerificationCodeType.PasswordReset,
            Usado = false,
            ExpiraEm = DateTime.Now.AddMinutes(-5)
        };

        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(usuario);
            setup.VerificationCodes.Add(codigoValido);
            setup.VerificationCodes.Add(codigoExpirado);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new VerificationCodeRepository(context);

        var resultado = await repo.ObterCodigoValidoAsync(usuario.Id, VerificationCodeType.PasswordReset);

        Assert.NotNull(resultado);
        Assert.Equal(codigoValido.Id, resultado!.Id);
    }

    [Fact]
    public async Task VerificationCodeRepository_ObterCodigoAsync_EncontraPorCodigoExato()
    {
        using var db = SqliteDbContextFactory.Create();
        var usuario = UsuarioSimples(ativo: true, cpf: "13131313131");
        var codigo = new VerificationCode { Id = Guid.NewGuid(), UsuarioId = usuario.Id, Codigo = "hash-x", Tipo = VerificationCodeType.AccountReactivation, ExpiraEm = DateTime.Now.AddMinutes(5) };

        await using (var setup = db.NewContext())
        {
            setup.Usuarios.Add(usuario);
            setup.VerificationCodes.Add(codigo);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new VerificationCodeRepository(context);

        var resultado = await repo.ObterCodigoAsync(usuario.Id, "hash-x", VerificationCodeType.AccountReactivation);

        Assert.NotNull(resultado);
        Assert.Equal(codigo.Id, resultado!.Id);
    }

    [Fact]
    public async Task RepositoryGenerics_ObterComIncludes_CarregaRelacionamento()
    {
        using var db = SqliteDbContextFactory.Create();
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer" };
        var enderecoOrgTime = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var usuarioOrgTime = UsuarioSimples(ativo: true, cpf: "14141414141", cargo: EnumCargo.OrganizadorTime);
        var organizadorTime = new OrganizadorTime { Id = Guid.NewGuid(), UsuarioId = usuarioOrgTime.Id, EnderecoId = enderecoOrgTime.Id };
        var time = new Time { Id = Guid.NewGuid(), Nome = "Time X", Cidade = "SP", Estado = "SP", EsporteId = esporte.Id, OrganizadorTimeId = organizadorTime.Id, Ativo = true };

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(enderecoOrgTime);
            setup.Usuarios.Add(usuarioOrgTime);
            setup.OrganizadoresTime.Add(organizadorTime);
            setup.Esportes.Add(esporte);
            setup.Times.Add(time);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Time>(context);

        var resultado = (await repo.ObterComIncludes(t => t.Esporte)).ToList();

        Assert.Single(resultado);
        Assert.Equal("Futebol", resultado[0].Esporte!.Nome);
    }

    [Fact]
    public async Task RepositoryGenerics_BuscarPrimeiro_RetornaPrimeiroCorrespondente()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Esporte>(context);
        await repo.Adicionar(new Esporte { Id = Guid.NewGuid(), Nome = "Handebol", Icone = "mdi:handball", Ativo = true });

        var resultado = await repo.BuscarPrimeiro(e => e.Nome == "Handebol");

        Assert.NotNull(resultado);
        Assert.Equal("Handebol", resultado!.Nome);
    }

    [Fact]
    public async Task RepositoryGenerics_BuscarPrimeiro_SemCorrespondencia_RetornaNull()
    {
        using var db = SqliteDbContextFactory.Create();
        await using var context = db.NewContext();
        var repo = new RepositoryGenerics<Esporte>(context);

        Assert.Null(await repo.BuscarPrimeiro(e => e.Nome == "Inexistente"));
    }

    [Fact]
    public async Task RepositoryCampeonato_ObterCampeonatosComTimes_CarregaRelacionamentos()
    {
        using var db = SqliteDbContextFactory.Create();
        var esporte = new Esporte { Id = Guid.NewGuid(), Nome = "Futebol", Icone = "mdi:soccer" };
        var endereco = new Endereco { Id = Guid.NewGuid(), Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };
        var usuario = UsuarioSimples(ativo: true, cpf: "15151515151", cargo: EnumCargo.OrganizadorCampeonato);
        var organizador = new OrganizadorCampeonato { Id = Guid.NewGuid(), UsuarioId = usuario.Id, EnderecoId = endereco.Id };
        var campeonato = new Campeonato { Id = Guid.NewGuid(), Nome = "Copa", OrganizadorCampeonatoId = organizador.Id, EsporteId = esporte.Id, DataInicio = DateTime.Today, DataFim = DateTime.Today.AddDays(10) };

        await using (var setup = db.NewContext())
        {
            setup.Enderecos.Add(endereco);
            setup.Usuarios.Add(usuario);
            setup.OrganizadoresCampeonato.Add(organizador);
            setup.Esportes.Add(esporte);
            setup.Campeonatos.Add(campeonato);
            await setup.SaveChangesAsync();
        }

        await using var context = db.NewContext();
        var repo = new RepositoryCampeonato(context);

        var resultado = (await repo.ObterCampeonatosComTimes()).ToList();

        Assert.Single(resultado);
        Assert.Equal("Copa", resultado[0].Nome);
        Assert.Equal("Futebol", resultado[0].Esporte!.Nome);
    }

    private static Usuario UsuarioSimples(bool ativo, string cpf, EnumCargo cargo = EnumCargo.Torcedor) => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Usuario Teste",
        Email = $"{Guid.NewGuid()}@test.com",
        Cpf = cpf,
        Telefone = "11999999999",
        DataNascimento = DateTime.Today.AddYears(-25),
        EnumCargo = cargo,
        Ativo = ativo,
    };
}
