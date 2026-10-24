using kivoBackend.Application.DTO;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Presentation.Controller;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using static kivoBackend.Tests.TestSupport.TestEntities;

namespace kivoBackend.Tests.Unit.Auth;

public class UsuarioControllerTests
{
    private static UsuarioController Build(FakeUsuarioService usuarios, Guid? currentUserId = null, bool isAdmin = false,
        EmptyRepo<OrganizadorCampeonato>? orgCampRepo = null, EmptyRepo<OrganizadorTime>? orgTimeRepo = null)
        => new(usuarios, orgCampRepo ?? new EmptyRepo<OrganizadorCampeonato>(), orgTimeRepo ?? new EmptyRepo<OrganizadorTime>(),
               new FakeCurrentUser(currentUserId, isAdmin));

    private static EnderecoDto NovoEndereco() => new() { Cep = "01000-000", Rua = "Rua A", Numero = "1", Cidade = "SP", Estado = "SP", Pais = "Brasil" };

    private static UsuarioDTO NovoUsuarioDto() => new()
    {
        Nome = "Ana Silva",
        Email = "ana@test.com",
        Senha = "Senha123!",
        Cpf = "12345678901",
        Telefone = "11999999999",
        DataNascimento = new DateTime(1994, 3, 10)
    };

    // ---------- GetAll / GetById / GetAdministradores / ObterPorCpf ----------

    [Fact]
    public async Task GetAll_ComUsuarios_RetornaOk()
    {
        var usuarios = new FakeUsuarioService();
        usuarios.Users[Guid.NewGuid()] = UsuarioTorcedor(Guid.NewGuid());
        var controller = Build(usuarios);

        Assert.IsType<OkObjectResult>(await controller.GetAll());
    }

    [Fact]
    public async Task GetAll_SemUsuarios_RetornaNotFound()
    {
        var usuarios = new FakeUsuarioService { ThrowOnObterTodos = new KeyNotFoundException("Nenhum usuário encontrado.") };
        var controller = Build(usuarios);

        Assert.IsType<NotFoundObjectResult>(await controller.GetAll());
    }

    [Fact]
    public async Task GetById_Existente_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var controller = Build(usuarios);

        var result = Assert.IsType<OkObjectResult>(await controller.GetById(userId));
        var dto = Assert.IsType<ListarUsuarioDTO>(result.Value);
        Assert.Equal(userId, dto.Id);
    }

    [Fact]
    public async Task GetById_Inexistente_RetornaNotFound()
    {
        var controller = Build(new FakeUsuarioService());

        Assert.IsType<NotFoundObjectResult>(await controller.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetAdministradores_SemAdmins_RetornaNotFound()
    {
        var usuarios = new FakeUsuarioService { ThrowOnObterAdministradores = new KeyNotFoundException("Nenhum administrador encontrado.") };
        var controller = Build(usuarios);

        Assert.IsType<NotFoundObjectResult>(await controller.GetAdministradores());
    }

    [Fact]
    public async Task GetAdministradores_ComAdmins_RetornaOk()
    {
        var admin = UsuarioTorcedor(Guid.NewGuid());
        admin.EnumCargo = EnumCargo.Administrador;
        var usuarios = new FakeUsuarioService();
        usuarios.Users[admin.Id] = admin;
        var controller = Build(usuarios);

        Assert.IsType<OkObjectResult>(await controller.GetAdministradores());
    }

    [Fact]
    public async Task ObterPorCpf_Existente_RetornaOk()
    {
        var usuario = UsuarioTorcedor(Guid.NewGuid());
        var usuarios = new FakeUsuarioService();
        usuarios.UsersByCpf[usuario.Cpf] = usuario;
        var controller = Build(usuarios);

        Assert.IsType<OkObjectResult>(await controller.ObterPorCpf(usuario.Cpf));
    }

    [Fact]
    public async Task ObterPorCpf_Inexistente_RetornaNotFound()
    {
        var controller = Build(new FakeUsuarioService());

        Assert.IsType<NotFoundObjectResult>(await controller.ObterPorCpf("00000000000"));
    }

    // ---------- Creation ----------

    [Fact]
    public async Task CriarTorcedor_DadosValidos_RetornaCreated()
    {
        var usuarios = new FakeUsuarioService();
        var controller = Build(usuarios);
        var dto = new CriarTorcedorDto { Nome = "Ana", Email = "ana@test.com", Senha = "Senha123!", Cpf = "12345678901", Telefone = "11999999999", DataNascimento = DateTime.Today.AddYears(-20), Endereco = NovoEndereco() };

        var result = await controller.CriarTorcedor(dto);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var lista = Assert.IsType<ListarUsuarioDTO>(created.Value);
        Assert.Equal("Ana", lista.Nome);
        Assert.NotNull(lista.Endereco);
    }

    [Fact]
    public async Task CriarTorcedor_ServicoLanca_RetornaBadRequest()
    {
        var usuarios = new FakeUsuarioService { ThrowOnCriarUsuario = new InvalidOperationException("Já existe um usuário com este CPF.") };
        var controller = Build(usuarios);
        var dto = new CriarTorcedorDto { Nome = "Ana", Email = "ana@test.com", Senha = "Senha123!", Cpf = "12345678901", Telefone = "11999999999", DataNascimento = DateTime.Today.AddYears(-20), Endereco = NovoEndereco() };

        Assert.IsType<BadRequestObjectResult>(await controller.CriarTorcedor(dto));
    }

    [Fact]
    public async Task PostOrganizadorTime_DadosValidos_RetornaCreated()
    {
        var usuarios = new FakeUsuarioService();
        var controller = Build(usuarios);
        var dto = new CriarOrganizadorTimeDto { Nome = "Bruno", Email = "bruno@test.com", Senha = "Senha123!", Cpf = "12345678902", Telefone = "11988888888", DataNascimento = DateTime.Today.AddYears(-30), Endereco = NovoEndereco() };

        var result = Assert.IsType<CreatedAtActionResult>(await controller.PostOrganizadorTime(dto));
        var lista = Assert.IsType<ListarUsuarioDTO>(result.Value);
        Assert.Equal("Bruno", lista.Nome);
    }

    [Fact]
    public async Task PostOrganizadorCampeonato_DadosValidos_RetornaCreated()
    {
        var usuarios = new FakeUsuarioService();
        var controller = Build(usuarios);
        var dto = new CriarOrganizadorCampeonatoDto
        {
            Nome = "Carla",
            Email = "carla@test.com",
            Senha = "Senha123!",
            Cpf = "12345678903",
            Telefone = "11977777777",
            DataNascimento = DateTime.Today.AddYears(-40),
            Endereco = NovoEndereco(),
            ContaBanco = new ContaBancoDTO { Banco = "Banco X", Agencia = "1", Conta = "1", ChavePix = "pix" }
        };

        var result = Assert.IsType<CreatedAtActionResult>(await controller.PostOrganizadorCampeonato(dto));
        var lista = Assert.IsType<ListarUsuarioDTO>(result.Value);
        Assert.NotNull(lista.ContaBanco);
        Assert.Equal("Banco X", lista.ContaBanco!.Banco);
    }

    [Fact]
    public async Task PostAdmin_DadosValidos_RetornaCreated()
    {
        var usuarios = new FakeUsuarioService();
        var controller = Build(usuarios);

        var result = Assert.IsType<CreatedAtActionResult>(await controller.PostAdmin(NovoUsuarioDto()));
        var lista = Assert.IsType<ListarUsuarioDTO>(result.Value);
        Assert.Equal("Administrador", lista.Cargo);
    }

    // ---------- Editing (ownership) ----------

    [Fact]
    public async Task EditarTorcedor_DonoDoPerfil_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var controller = Build(usuarios, currentUserId: userId);

        var dto = new EditarUsuarioDTO { Nome = "Novo Nome", Email = "novo@test.com", Telefone = "11999999999", DataNascimento = DateTime.Today, Endereco = NovoEndereco() };

        Assert.IsType<OkObjectResult>(await controller.EditarTorcedor(userId, dto));
    }

    [Fact]
    public async Task EditarTorcedor_OutroUsuario_RetornaForbid()
    {
        var controller = Build(new FakeUsuarioService(), currentUserId: Guid.NewGuid());

        var dto = new EditarUsuarioDTO { Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco() };

        Assert.IsType<ForbidResult>(await controller.EditarTorcedor(Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task EditarTorcedor_SemTokenValido_RetornaUnauthorized()
    {
        var controller = Build(new FakeUsuarioService(), currentUserId: null);

        var dto = new EditarUsuarioDTO { Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco() };

        Assert.IsType<UnauthorizedObjectResult>(await controller.EditarTorcedor(Guid.NewGuid(), dto));
    }

    [Fact]
    public async Task EditarOrgTime_Admin_PodeEditarQualquerUsuario()
    {
        var targetId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[targetId] = UsuarioOrganizadorTime(targetId, Guid.NewGuid());
        var controller = Build(usuarios, currentUserId: Guid.NewGuid(), isAdmin: true);

        var dto = new EditarUsuarioDTO { Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco() };

        Assert.IsType<OkObjectResult>(await controller.EditarOrgTime(targetId, dto));
    }

    [Fact]
    public async Task EditarOrgCampeonato_SemContaBanco_RetornaBadRequest()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var controller = Build(usuarios, currentUserId: userId);

        var dto = new EditarOrganizadorCampeonatoDTO { Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco(), ContaBanco = null };

        Assert.IsType<BadRequestObjectResult>(await controller.EditarOrgCampeonato(userId, dto));
    }

    [Fact]
    public async Task EditarOrgCampeonato_ComContaBanco_RetornaOk()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioOrganizadorCampeonato(userId, Guid.NewGuid());
        var controller = Build(usuarios, currentUserId: userId);

        var dto = new EditarOrganizadorCampeonatoDTO
        {
            Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco(),
            ContaBanco = new ContaBancoDTO { Banco = "B", Agencia = "1", Conta = "1", ChavePix = "p" }
        };

        Assert.IsType<OkObjectResult>(await controller.EditarOrgCampeonato(userId, dto));
    }

    [Fact]
    public async Task EditarAdmin_NaoValidaOwnership()
    {
        var targetId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[targetId] = UsuarioTorcedor(targetId);
        var controller = Build(usuarios, currentUserId: Guid.NewGuid(), isAdmin: true);

        var dto = new EditarUsuarioDTO { Nome = "X", Email = "x@test.com", Telefone = "1", DataNascimento = DateTime.Today, Endereco = NovoEndereco() };

        Assert.IsType<OkObjectResult>(await controller.EditarAdmin(targetId, dto));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_DonoDoPerfil_RemoveERetornaNoContent()
    {
        var userId = Guid.NewGuid();
        var usuarios = new FakeUsuarioService();
        usuarios.Users[userId] = UsuarioTorcedor(userId);
        var controller = Build(usuarios, currentUserId: userId);

        Assert.IsType<NoContentResult>(await controller.Delete(userId));
        Assert.True(usuarios.RemoveCalled);
    }

    [Fact]
    public async Task Delete_OutroUsuario_RetornaForbidENaoRemove()
    {
        var usuarios = new FakeUsuarioService();
        var controller = Build(usuarios, currentUserId: Guid.NewGuid());

        Assert.IsType<ForbidResult>(await controller.Delete(Guid.NewGuid()));
        Assert.False(usuarios.RemoveCalled);
    }

    [Fact]
    public async Task Delete_Inexistente_RetornaNotFound()
    {
        var userId = Guid.NewGuid();
        var controller = Build(new FakeUsuarioService(), currentUserId: userId);

        Assert.IsType<NotFoundResult>(await controller.Delete(userId));
    }

    // ---------- Check email/cpf ----------

    [Fact]
    public async Task CheckEmail_EmailVazio_RetornaBadRequest()
    {
        var controller = Build(new FakeUsuarioService());

        Assert.IsType<BadRequestObjectResult>(await controller.CheckEmail(new CheckEmailRequest { Email = "" }));
    }

    [Fact]
    public async Task CheckEmail_EmailExistente_RetornaExistsTrue()
    {
        var usuarios = new FakeUsuarioService { EmailExists = true };
        var controller = Build(usuarios);

        var result = Assert.IsType<OkObjectResult>(await controller.CheckEmail(new CheckEmailRequest { Email = "x@test.com" }));
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task CheckCpf_CpfVazio_RetornaBadRequest()
    {
        var controller = Build(new FakeUsuarioService());

        Assert.IsType<BadRequestObjectResult>(await controller.CheckCpf(new CheckCpfRequest { Cpf = "" }));
    }

    [Fact]
    public async Task CheckCpf_CpfComFormatacao_LimpaEChecaExistencia()
    {
        var usuarios = new FakeUsuarioService { CpfExists = true };
        var controller = Build(usuarios);

        var result = Assert.IsType<OkObjectResult>(await controller.CheckCpf(new CheckCpfRequest { Cpf = "123.456.789-01" }));
        Assert.NotNull(result.Value);
    }

    // ---------- Reativar ----------

    [Fact]
    public async Task Reativar_ContaInativa_RetornaOk()
    {
        var controller = Build(new FakeUsuarioService(), isAdmin: true);

        Assert.IsType<OkObjectResult>(await controller.Reativar(Guid.NewGuid()));
    }

    [Fact]
    public async Task Reativar_ServicoLanca_RetornaBadRequest()
    {
        var usuarios = new FakeUsuarioService { ThrowOnAtivarConta = new InvalidOperationException("Esta conta já está ativa.") };
        var controller = Build(usuarios, isAdmin: true);

        Assert.IsType<BadRequestObjectResult>(await controller.Reativar(Guid.NewGuid()));
    }

    // ---------- Admin-only listings ----------

    [Fact]
    public async Task GetOrganizadoresCampeonato_RetornaListaMapeada()
    {
        var usuario = UsuarioOrganizadorCampeonato(Guid.NewGuid(), Guid.NewGuid());
        var organizador = usuario.OrganizadorCampeonato!;
        organizador.Usuario = usuario;
        var controllerComDados = new UsuarioController(new FakeUsuarioService(), new InMemoryRepo<OrganizadorCampeonato>(organizador), new EmptyRepo<OrganizadorTime>(), new FakeCurrentUser(Guid.NewGuid(), isAdmin: true));

        var result = Assert.IsType<OkObjectResult>(await controllerComDados.GetOrganizadoresCampeonato());
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task GetOrganizadoresTime_RetornaListaMapeada()
    {
        var usuario = UsuarioOrganizadorTime(Guid.NewGuid(), Guid.NewGuid());
        var organizador = new OrganizadorTime { Id = usuario.OrganizadorTime!.Id, UsuarioId = usuario.Id, Usuario = usuario };
        var controller = new UsuarioController(new FakeUsuarioService(), new EmptyRepo<OrganizadorCampeonato>(), new InMemoryRepo<OrganizadorTime>(organizador), new FakeCurrentUser(Guid.NewGuid(), isAdmin: true));

        var result = Assert.IsType<OkObjectResult>(await controller.GetOrganizadoresTime());
        Assert.NotNull(result.Value);
    }
}
