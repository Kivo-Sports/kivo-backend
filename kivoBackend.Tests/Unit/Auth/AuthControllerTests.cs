using kivoBackend.Application.DTO;
using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Tests.Unit.Auth;

public class AuthControllerTests
{
    private static IConfiguration Configuracao() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "chave-super-secreta-de-teste-com-32-bytes-ou-mais!",
            ["Jwt:Issuer"] = "kivo-tests",
            ["Jwt:Audience"] = "kivo-tests-audience",
        }).Build();

    private static async Task<(AuthController Controller, UserManager<IdentityUser> UserManager, FakeUsuarioService Usuarios)> Build()
    {
        var userManager = UserManagerTestFactory.Create(new FakeUserStore());
        var usuarios = new FakeUsuarioService();
        var tokenService = new TokenService(userManager, Configuracao());
        var controller = new AuthController(userManager, usuarios, tokenService);
        return (controller, userManager, usuarios);
    }

    private static Usuario NovoUsuario(string email, string cpf, bool ativo = true) => new()
    {
        Id = Guid.NewGuid(), Nome = "Ana", Email = email, Cpf = cpf, Telefone = "11999999999",
        DataNascimento = DateTime.Today.AddYears(-20), EnumCargo = EnumCargo.Torcedor, Ativo = ativo
    };

    [Fact]
    public async Task Login_ComEmailESenhaCorretos_RetornaToken()
    {
        var (controller, userManager, usuarios) = await Build();
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        var usuario = NovoUsuario("ana@test.com", "12345678901");
        usuarios.Users[usuario.Id] = usuario;
        usuarios.UsersByCpf[usuario.Cpf] = usuario;

        var result = Assert.IsType<OkObjectResult>(await controller.Login(new LoginDTO { Identificador = "ana@test.com", Senha = "Senha123!" }));
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task Login_ComCpfESenhaCorretos_RetornaToken()
    {
        var (controller, userManager, usuarios) = await Build();
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        var usuario = NovoUsuario("ana@test.com", "12345678901");
        usuarios.Users[usuario.Id] = usuario;
        usuarios.UsersByCpf[usuario.Cpf] = usuario;

        Assert.IsType<OkObjectResult>(await controller.Login(new LoginDTO { Identificador = "12345678901", Senha = "Senha123!" }));
    }

    [Fact]
    public async Task Login_UsuarioInexistente_RetornaUnauthorized()
    {
        var (controller, _, _) = await Build();

        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(new LoginDTO { Identificador = "naoexiste@test.com", Senha = "x" }));
    }

    [Fact]
    public async Task Login_ContaDesativada_RetornaUnauthorized()
    {
        var (controller, userManager, usuarios) = await Build();
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "Senha123!");
        var usuario = NovoUsuario("ana@test.com", "12345678901", ativo: false);
        usuarios.Users[usuario.Id] = usuario;

        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(new LoginDTO { Identificador = "ana@test.com", Senha = "Senha123!" }));
    }

    [Fact]
    public async Task Login_SenhaIncorreta_RetornaUnauthorized()
    {
        var (controller, userManager, usuarios) = await Build();
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "SenhaCorreta1!");
        var usuario = NovoUsuario("ana@test.com", "12345678901");
        usuarios.Users[usuario.Id] = usuario;

        Assert.IsType<UnauthorizedObjectResult>(await controller.Login(new LoginDTO { Identificador = "ana@test.com", Senha = "SenhaErrada1!" }));
    }

    [Fact]
    public async Task EnviarCodigoReativacao_ModelStateInvalido_RetornaBadRequest()
    {
        var (controller, _, _) = await Build();
        controller.ModelState.AddModelError("Email", "Email inválido");

        Assert.IsType<BadRequestObjectResult>(await controller.EnviarCodigoReativacao(new EnviarCodigoReativacaoDTO { Email = "x" }));
    }

    [Fact]
    public async Task EnviarCodigoReativacao_Valido_RetornaOk()
    {
        var (controller, _, _) = await Build();

        Assert.IsType<OkObjectResult>(await controller.EnviarCodigoReativacao(new EnviarCodigoReativacaoDTO { Email = "ana@test.com" }));
    }

    [Fact]
    public async Task EnviarCodigoReativacao_UsuarioNaoEncontrado_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnGerarCodigoReativacao = new KeyNotFoundException("Usuário não encontrado.");

        Assert.IsType<BadRequestObjectResult>(await controller.EnviarCodigoReativacao(new EnviarCodigoReativacaoDTO { Email = "x@test.com" }));
    }

    [Fact]
    public async Task EnviarCodigoReativacao_RegraDeNegocioViolada_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnGerarCodigoReativacao = new InvalidOperationException("Esta conta já está ativa.");

        Assert.IsType<BadRequestObjectResult>(await controller.EnviarCodigoReativacao(new EnviarCodigoReativacaoDTO { Email = "x@test.com" }));
    }

    [Fact]
    public async Task EnviarCodigoReativacao_ErroInesperado_Retorna500()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnGerarCodigoReativacao = new Exception("falha no envio");

        var result = Assert.IsType<ObjectResult>(await controller.EnviarCodigoReativacao(new EnviarCodigoReativacaoDTO { Email = "x@test.com" }));
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task ConfirmarReativacao_ModelStateInvalido_RetornaBadRequest()
    {
        var (controller, _, _) = await Build();
        controller.ModelState.AddModelError("Codigo", "invalido");

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarReativacao(new ConfirmarReativacaoDTO { Email = "a@test.com", Codigo = "1" }));
    }

    [Fact]
    public async Task ConfirmarReativacao_Valido_RetornaOk()
    {
        var (controller, _, _) = await Build();

        Assert.IsType<OkObjectResult>(await controller.ConfirmarReativacao(new ConfirmarReativacaoDTO { Email = "a@test.com", Codigo = "123456" }));
    }

    [Fact]
    public async Task ConfirmarReativacao_CodigoInvalido_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarReativacao = new InvalidOperationException("Código inválido ou expirado.");

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarReativacao(new ConfirmarReativacaoDTO { Email = "a@test.com", Codigo = "123456" }));
    }

    [Fact]
    public async Task ConfirmarReativacao_UsuarioNaoEncontrado_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarReativacao = new KeyNotFoundException();

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarReativacao(new ConfirmarReativacaoDTO { Email = "a@test.com", Codigo = "123456" }));
    }

    [Fact]
    public async Task ConfirmarReativacao_ErroInesperado_Retorna500()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarReativacao = new Exception("falha");

        var result = Assert.IsType<ObjectResult>(await controller.ConfirmarReativacao(new ConfirmarReativacaoDTO { Email = "a@test.com", Codigo = "123456" }));
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task EnviarCodigoRecuperacaoSenha_ModelStateInvalido_RetornaBadRequest()
    {
        var (controller, _, _) = await Build();
        controller.ModelState.AddModelError("Email", "invalido");

        Assert.IsType<BadRequestObjectResult>(await controller.EnviarCodigoRecuperacaoSenha(new EnviarCodigoRecuperacaoSenhaDTO { Email = "x" }));
    }

    [Fact]
    public async Task EnviarCodigoRecuperacaoSenha_Valido_RetornaOk()
    {
        var (controller, _, _) = await Build();

        Assert.IsType<OkObjectResult>(await controller.EnviarCodigoRecuperacaoSenha(new EnviarCodigoRecuperacaoSenhaDTO { Email = "a@test.com" }));
    }

    [Fact]
    public async Task EnviarCodigoRecuperacaoSenha_UsuarioNaoEncontrado_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnGerarCodigoRecuperacaoSenha = new KeyNotFoundException();

        Assert.IsType<BadRequestObjectResult>(await controller.EnviarCodigoRecuperacaoSenha(new EnviarCodigoRecuperacaoSenhaDTO { Email = "a@test.com" }));
    }

    [Fact]
    public async Task EnviarCodigoRecuperacaoSenha_ErroInesperado_Retorna500()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnGerarCodigoRecuperacaoSenha = new Exception("falha");

        var result = Assert.IsType<ObjectResult>(await controller.EnviarCodigoRecuperacaoSenha(new EnviarCodigoRecuperacaoSenhaDTO { Email = "a@test.com" }));
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_ModelStateInvalido_RetornaBadRequest()
    {
        var (controller, _, _) = await Build();
        controller.ModelState.AddModelError("Codigo", "invalido");

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarRecuperacaoSenha(new ConfirmarRecuperacaoSenhaDTO { Email = "a@test.com", Codigo = "1", NovaSenha = "x" }));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_Valido_RetornaOk()
    {
        var (controller, _, _) = await Build();

        Assert.IsType<OkObjectResult>(await controller.ConfirmarRecuperacaoSenha(new ConfirmarRecuperacaoSenhaDTO { Email = "a@test.com", Codigo = "123456", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_CodigoInvalido_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarRecuperacaoSenha = new InvalidOperationException("Código inválido ou expirado.");

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarRecuperacaoSenha(new ConfirmarRecuperacaoSenhaDTO { Email = "a@test.com", Codigo = "123456", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_UsuarioNaoEncontrado_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarRecuperacaoSenha = new KeyNotFoundException();

        Assert.IsType<BadRequestObjectResult>(await controller.ConfirmarRecuperacaoSenha(new ConfirmarRecuperacaoSenhaDTO { Email = "a@test.com", Codigo = "123456", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_ErroInesperado_Retorna500()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnConfirmarRecuperacaoSenha = new Exception("falha");

        var result = Assert.IsType<ObjectResult>(await controller.ConfirmarRecuperacaoSenha(new ConfirmarRecuperacaoSenhaDTO { Email = "a@test.com", Codigo = "123456", NovaSenha = "NovaSenha123!" }));
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public async Task RedefinirSenha_ModelStateInvalido_RetornaBadRequest()
    {
        var (controller, _, _) = await Build();
        controller.ControllerContext = FakeHttp.ContextFor(Guid.NewGuid());
        controller.ModelState.AddModelError("NovaSenha", "invalido");

        Assert.IsType<BadRequestObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "a", NovaSenha = "x" }));
    }

    [Fact]
    public async Task RedefinirSenha_SemEmailNoToken_RetornaUnauthorized()
    {
        var (controller, _, _) = await Build();
        controller.ControllerContext = FakeHttp.ContextFor(Guid.NewGuid()); // sem claim de email

        Assert.IsType<UnauthorizedObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "a", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task RedefinirSenha_ComEmailNoToken_RetornaOk()
    {
        var (controller, userManager, usuarios) = await Build();
        var identity = new IdentityUser { UserName = "ana@test.com", Email = "ana@test.com" };
        await userManager.CreateAsync(identity, "SenhaAtual1!");
        var usuario = NovoUsuario("ana@test.com", "12345678901");
        usuarios.Users[usuario.Id] = usuario;

        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.Email, "ana@test.com") };
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "TestAuth"))
        };
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };

        Assert.IsType<OkObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "SenhaAtual1!", NovaSenha = "NovaSenha123!" }));
    }

    private static void ComEmailNoToken(AuthController controller, string email)
    {
        var claims = new List<System.Security.Claims.Claim> { new(System.Security.Claims.ClaimTypes.Email, email) };
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(claims, "TestAuth"))
        };
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext = httpContext };
    }

    [Fact]
    public async Task RedefinirSenha_UsuarioNaoEncontrado_RetornaNotFound()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnRedefinirSenha = new KeyNotFoundException();
        ComEmailNoToken(controller, "ana@test.com");

        Assert.IsType<NotFoundObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "a", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task RedefinirSenha_SenhaAtualIncorreta_RetornaBadRequest()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnRedefinirSenha = new InvalidOperationException("Senha atual incorreta.");
        ComEmailNoToken(controller, "ana@test.com");

        Assert.IsType<BadRequestObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "a", NovaSenha = "NovaSenha123!" }));
    }

    [Fact]
    public async Task RedefinirSenha_ErroInesperado_Retorna500()
    {
        var (controller, _, usuarios) = await Build();
        usuarios.ThrowOnRedefinirSenha = new Exception("falha");
        ComEmailNoToken(controller, "ana@test.com");

        var result = Assert.IsType<ObjectResult>(await controller.RedefinirSenha(new RedefinirSenhaDTO { SenhaAtual = "a", NovaSenha = "NovaSenha123!" }));
        Assert.Equal(500, result.StatusCode);
    }
}
