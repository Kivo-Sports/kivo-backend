using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Tests.Unit.Domain;

public class TokenServiceTests
{
    private static IConfiguration ConfiguracaoValida() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "chave-super-secreta-de-teste-com-32-bytes-ou-mais!",
            ["Jwt:Issuer"] = "kivo-tests",
            ["Jwt:Audience"] = "kivo-tests-audience",
        })
        .Build();

    [Fact]
    public async Task GenerateToken_UsuarioComRoles_IncluiClaimsEssenciais()
    {
        var store = new FakeUserStore();
        var userManager = UserManagerTestFactory.Create(store);
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        await userManager.AddToRoleAsync(identity, "Torcedor");

        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = "Ana", Email = "ana@test.com", EnumCargo = EnumCargo.Torcedor };
        var service = new TokenService(userManager, ConfiguracaoValida());

        var jwt = await service.GenerateToken(identity, usuario);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        Assert.Equal(usuario.Id.ToString(), token.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("ana@test.com", token.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Torcedor", token.Claims.First(c => c.Type == "Cargo").Value);
        // UserManager.AddToRoleAsync normalizes the role name before persisting it (ASP.NET
        // Identity convention), so the round-tripped claim comes back upper-cased.
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "TORCEDOR");
        Assert.Equal("kivo-tests", token.Issuer);
        Assert.Contains("kivo-tests-audience", token.Audiences);
    }

    [Fact]
    public async Task GenerateToken_SemChaveConfigurada_Lanca()
    {
        var store = new FakeUserStore();
        var userManager = UserManagerTestFactory.Create(store);
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = "Ana", Email = "ana@test.com", EnumCargo = EnumCargo.Torcedor };

        var configSemChave = new ConfigurationBuilder().Build();
        var service = new TokenService(userManager, configSemChave);

        await Assert.ThrowsAsync<Exception>(() => service.GenerateToken(identity, usuario));
    }

    [Fact]
    public async Task GenerateToken_SemRoles_NaoIncluiClaimsDeRole()
    {
        var store = new FakeUserStore();
        var userManager = UserManagerTestFactory.Create(store);
        var identity = new IdentityUser { UserName = "bruno@test.com", Email = "bruno@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        var usuario = new Usuario { Id = Guid.NewGuid(), Nome = "Bruno", Email = "bruno@test.com", EnumCargo = EnumCargo.OrganizadorTime };

        var service = new TokenService(userManager, ConfiguracaoValida());
        var jwt = await service.GenerateToken(identity, usuario);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        Assert.DoesNotContain(token.Claims, c => c.Type == ClaimTypes.Role);
    }
}
