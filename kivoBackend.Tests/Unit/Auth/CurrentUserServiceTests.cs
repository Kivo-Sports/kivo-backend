using System.Security.Claims;
using kivoBackend.Presentation.Auth;
using Microsoft.AspNetCore.Http;

namespace kivoBackend.Tests.Unit.Auth;

/// <summary>CurrentUserService wraps IHttpContextAccessor/ClaimsPrincipal — exercised here
/// with a real DefaultHttpContext instead of an ASP.NET pipeline.</summary>
public class CurrentUserServiceTests
{
    private static ICurrentUserService Build(ClaimsPrincipal? user)
    {
        var context = new DefaultHttpContext();
        if (user != null) context.User = user;
        var accessor = new HttpContextAccessorStub(context);
        return new CurrentUserService(accessor);
    }

    private static ClaimsPrincipal AuthenticatedUser(Guid userId, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void IsAuthenticated_SemHttpContext_False()
    {
        var accessor = new HttpContextAccessorStub(null);
        var service = new CurrentUserService(accessor);

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
        Assert.False(service.IsAdmin);
    }

    [Fact]
    public void IsAuthenticated_UsuarioAnonimo_False()
    {
        var anonimo = new ClaimsPrincipal(new ClaimsIdentity()); // sem authenticationType => não autenticado
        var service = Build(anonimo);

        Assert.False(service.IsAuthenticated);
        Assert.Null(service.UserId);
    }

    [Fact]
    public void UserId_ClaimValida_RetornaGuid()
    {
        var userId = Guid.NewGuid();
        var service = Build(AuthenticatedUser(userId));

        Assert.True(service.IsAuthenticated);
        Assert.Equal(userId, service.UserId);
    }

    [Fact]
    public void UserId_ClaimAusenteOuInvalida_RetornaNull()
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Torcedor") }, "TestAuth");
        var service = Build(new ClaimsPrincipal(identity));

        Assert.Null(service.UserId);
    }

    [Theory]
    [InlineData("Administrador", true)]
    [InlineData("Admin", true)]
    [InlineData("Torcedor", false)]
    public void IsAdmin_DependeDaRole(string role, bool esperado)
    {
        var service = Build(AuthenticatedUser(Guid.NewGuid(), role));

        Assert.Equal(esperado, service.IsAdmin);
    }

    [Fact]
    public void IsInRole_RoleNaoAtribuida_False()
    {
        var service = Build(AuthenticatedUser(Guid.NewGuid(), "Torcedor"));

        Assert.False(service.IsInRole("OrganizadorTime"));
        Assert.True(service.IsInRole("Torcedor"));
    }

    /// <summary>Minimal IHttpContextAccessor — the real implementation stores state in AsyncLocal,
    /// which is unnecessary ceremony for a unit test that only needs to hand back a fixed context.</summary>
    private sealed class HttpContextAccessorStub : IHttpContextAccessor
    {
        public HttpContextAccessorStub(HttpContext? context) => HttpContext = context;
        public HttpContext? HttpContext { get; set; }
    }
}
