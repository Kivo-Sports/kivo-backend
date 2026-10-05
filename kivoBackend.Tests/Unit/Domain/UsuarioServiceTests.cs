using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Core.Interfaces;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// UsuarioService — creation, profile editing, activation/reactivation and
/// password recovery. UserManager&lt;IdentityUser&gt; is backed by a real
/// instance over <see cref="FakeUserStore"/> (see IdentityTestSupport.cs),
/// everything else uses the project's hand-rolled fakes.
/// </summary>
public class UsuarioServiceTests
{
    private static (UsuarioService Service, InMemoryRepo<Usuario> Repo, FakeRepositoryUsuario UsuarioRepo,
        UserManager<IdentityUser> UserManager, FakeEmailService Email, InMemoryRepo<CodigoReativacao> Codigos,
        FakeVerificationCodeService VerificationCodes, FakeNotificacaoService Notificacoes) CriarServico(params Usuario[] seed)
    {
        var repo = new InMemoryRepo<Usuario>(seed);
        var usuarioRepo = new FakeRepositoryUsuario();
        foreach (var u in seed) usuarioRepo.Users[u.Id] = u;
        var userManager = UserManagerTestFactory.Create(new FakeUserStore());
        var email = new FakeEmailService();
        var codigos = new InMemoryRepo<CodigoReativacao>();
        var verificationCodes = new FakeVerificationCodeService();
        var notificacoes = new FakeNotificacaoService();

        var service = new UsuarioService(repo, usuarioRepo, userManager, email,
            codigos, verificationCodes, notificacoes);

        return (service, repo, usuarioRepo, userManager, email, codigos, verificationCodes, notificacoes);
    }

    private static Usuario NovoUsuario(EnumCargo cargo = EnumCargo.Torcedor, bool ativo = true) => new()
    {
        Id = Guid.NewGuid(),
        Nome = "Ana Silva",
        Email = $"{Guid.NewGuid()}@test.com",
        Cpf = Guid.NewGuid().ToString("N")[..11],
        Telefone = "11999999999",
        DataNascimento = new DateTime(1994, 3, 10),
        EnumCargo = cargo,
        Ativo = ativo,
    };

    // ---------- CriarUsuario ----------

    [Fact]
    public async Task CriarUsuario_DadosValidos_CriaEInicializaPerfilTorcedor()
    {
        var (service, repo, _, _, _, _, _, notificacoes) = CriarServico();
        var usuario = NovoUsuario();

        var criado = await service.CriarUsuario(usuario, "Senha123!");

        Assert.True(repo.Count == 1);
        Assert.True(criado.Ativo);
        Assert.NotNull(criado.Torcedor);
        Assert.Equal(criado.Id, criado.Torcedor!.UsuarioId);
        Assert.Single(notificacoes.Criadas);
    }

    [Fact]
    public async Task CriarUsuario_CpfVazio_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario();
        usuario.Cpf = "";

        await Assert.ThrowsAsync<ArgumentException>(() => service.CriarUsuario(usuario, "Senha123!"));
    }

    [Fact]
    public async Task CriarUsuario_EmailVazio_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario();
        usuario.Email = "   ";

        await Assert.ThrowsAsync<ArgumentException>(() => service.CriarUsuario(usuario, "Senha123!"));
    }

    [Fact]
    public async Task CriarUsuario_CpfDuplicado_Lanca()
    {
        var existente = NovoUsuario();
        var (service, _, _, _, _, _, _, _) = CriarServico(existente);
        var novo = NovoUsuario();
        novo.Cpf = existente.Cpf;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarUsuario(novo, "Senha123!"));
    }

    [Fact]
    public async Task CriarUsuario_EmailDuplicadoNoIdentity_Lanca()
    {
        var store = new FakeUserStore();
        var userManager = UserManagerTestFactory.Create(store);
        var repo = new InMemoryRepo<Usuario>();
        var usuarioRepo = new FakeRepositoryUsuario();
        var service = new UsuarioService(repo, usuarioRepo, userManager, new FakeEmailService(),
            new InMemoryRepo<CodigoReativacao>(), new FakeVerificationCodeService(), new FakeNotificacaoService());

        var primeiro = NovoUsuario();
        await service.CriarUsuario(primeiro, "Senha123!");

        var segundo = NovoUsuario();
        segundo.Email = primeiro.Email; // mesmo email, cpf diferente

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarUsuario(segundo, "Senha123!"));
    }

    [Fact]
    public async Task CriarUsuario_OrganizadorCampeonato_InicializaPerfilCorreto()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario(EnumCargo.OrganizadorCampeonato);

        var criado = await service.CriarUsuario(usuario, "Senha123!");

        Assert.NotNull(criado.OrganizadorCampeonato);
        Assert.Null(criado.Torcedor);
    }

    [Fact]
    public async Task CriarUsuario_OrganizadorTime_InicializaPerfilCorreto()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario(EnumCargo.OrganizadorTime);

        var criado = await service.CriarUsuario(usuario, "Senha123!");

        Assert.NotNull(criado.OrganizadorTime);
    }

    // ---------- InicializarPerfilPorCargo ----------

    [Fact]
    public void InicializarPerfilPorCargo_CargoInvalido_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario();
        usuario.EnumCargo = (EnumCargo)999;

        Assert.Throws<ArgumentException>(() => service.InicializarPerfilPorCargo(usuario));
    }

    [Fact]
    public void InicializarPerfilPorCargo_Administrador_NaoCriaPerfil()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();
        var usuario = NovoUsuario(EnumCargo.Administrador);

        service.InicializarPerfilPorCargo(usuario);

        Assert.Null(usuario.Torcedor);
        Assert.Null(usuario.OrganizadorTime);
        Assert.Null(usuario.OrganizadorCampeonato);
    }

    // ---------- AtivarConta / DesativarConta ----------

    [Fact]
    public async Task AtivarConta_ContaInativa_Ativa()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, repo, _, _, _, _, _, _) = CriarServico(usuario);

        await service.AtivarConta(usuario.Id);

        Assert.True((await repo.ObterPorId(usuario.Id))!.Ativo);
    }

    [Fact]
    public async Task AtivarConta_JaAtiva_Lanca()
    {
        var usuario = NovoUsuario(ativo: true);
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AtivarConta(usuario.Id));
    }

    [Fact]
    public async Task AtivarConta_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AtivarConta(Guid.NewGuid()));
    }

    [Fact]
    public async Task DesativarConta_ContaAtiva_Desativa()
    {
        var usuario = NovoUsuario(ativo: true);
        var (service, repo, _, _, _, _, _, _) = CriarServico(usuario);

        await service.DesativarConta(usuario);

        Assert.False((await repo.ObterPorId(usuario.Id))!.Ativo);
    }

    [Fact]
    public async Task DesativarConta_JaDesativada_Lanca()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DesativarConta(usuario));
    }

    [Fact]
    public async Task RemoverUsuario_Existente_DesativaERetorna()
    {
        var usuario = NovoUsuario(ativo: true);
        var (service, repo, _, _, _, _, _, _) = CriarServico(usuario);

        var removido = await service.RemoverUsuario(usuario.Id);

        Assert.Equal(usuario.Id, removido.Id);
        Assert.False((await repo.ObterPorId(usuario.Id))!.Ativo);
    }

    [Fact]
    public async Task RemoverUsuario_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.RemoverUsuario(Guid.NewGuid()));
    }

    // ---------- EditarDadosUsuario (ownership is enforced by the controller; here we cover the data-merge rules) ----------

    [Fact]
    public async Task EditarDadosUsuario_AtualizaCamposBasicosEEndereco()
    {
        var usuario = TestEntities.UsuarioTorcedor(Guid.NewGuid());
        usuario.Torcedor!.Endereco = new Endereco { Cep = "old", Rua = "old", Numero = "1", Cidade = "old", Estado = "old", Pais = "Brasil" };
        var (service, repo, usuarioRepo, _, _, _, _, notificacoes) = CriarServico(usuario);

        var editado = new Usuario
        {
            Nome = "Ana Nova",
            Email = usuario.Email,
            Telefone = "11888888888",
            DataNascimento = usuario.DataNascimento,
            Torcedor = new Torcedor { Endereco = new Endereco { Cep = "new", Rua = "new", Numero = "2", Cidade = "new", Estado = "new", Complemento = "apto 1" } }
        };

        var resultado = await service.EditarDadosUsuario(usuario.Id, editado);

        Assert.Equal("Ana Nova", resultado.Nome);
        Assert.Equal("new", resultado.Torcedor!.Endereco!.Cep);
        Assert.Equal("apto 1", resultado.Torcedor.Endereco.Complemento);
        Assert.Single(notificacoes.Criadas);
    }

    [Fact]
    public async Task EditarDadosUsuario_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.EditarDadosUsuario(Guid.NewGuid(), NovoUsuario()));
    }

    [Fact]
    public async Task EditarDadosUsuario_ContaInativa_Lanca()
    {
        var usuario = TestEntities.UsuarioTorcedor(Guid.NewGuid());
        usuario.Ativo = false;
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EditarDadosUsuario(usuario.Id, NovoUsuario()));
    }

    [Fact]
    public async Task EditarDadosUsuario_OrganizadorCampeonatoComContaBanco_AtualizaDadosBancarios()
    {
        var usuario = TestEntities.UsuarioOrganizadorCampeonato(Guid.NewGuid(), Guid.NewGuid());
        usuario.OrganizadorCampeonato!.Endereco = new Endereco();
        usuario.OrganizadorCampeonato.ContaBanco = new ContaBanco { Banco = "old", Agencia = "1", Conta = "1", ChavePix = "old" };
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        var editado = new Usuario
        {
            Nome = usuario.Nome,
            Email = usuario.Email,
            Telefone = usuario.Telefone,
            DataNascimento = usuario.DataNascimento,
            OrganizadorCampeonato = new OrganizadorCampeonato
            {
                Endereco = new Endereco { Cep = "x", Rua = "x", Numero = "x", Cidade = "x", Estado = "x" },
                ContaBanco = new ContaBanco { Banco = "novo", Agencia = "2", Conta = "2", ChavePix = "pix-novo" }
            }
        };

        var resultado = await service.EditarDadosUsuario(usuario.Id, editado);

        Assert.Equal("novo", resultado.OrganizadorCampeonato!.ContaBanco!.Banco);
        Assert.Equal("pix-novo", resultado.OrganizadorCampeonato.ContaBanco.ChavePix);
    }

    // ---------- Reactivation via email code ----------

    [Fact]
    public async Task GerarCodigoReativacao_UsuarioInativoNaoAdmin_GeraEEnviaEmail()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, _, _, _, email, _, verificationCodes, _) = CriarServico(usuario);

        await service.GerarCodigoReativacao(usuario.Email);

        Assert.True(email.EnviarChamado);
        Assert.Single(verificationCodes.Gerados);
        Assert.Equal(VerificationCodeType.AccountReactivation, verificationCodes.Gerados[0].Tipo);
    }

    [Fact]
    public async Task GerarCodigoReativacao_Administrador_Lanca()
    {
        var usuario = NovoUsuario(EnumCargo.Administrador, ativo: false);
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GerarCodigoReativacao(usuario.Email));
    }

    [Fact]
    public async Task GerarCodigoReativacao_ContaJaAtiva_Lanca()
    {
        var usuario = NovoUsuario(ativo: true);
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GerarCodigoReativacao(usuario.Email));
    }

    [Fact]
    public async Task GerarCodigoReativacao_EmailInexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GerarCodigoReativacao("naoexiste@test.com"));
    }

    [Fact]
    public async Task ConfirmarReativacao_CodigoValido_AtivaConta()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, repo, _, _, _, _, verificationCodes, notificacoes) = CriarServico(usuario);

        await service.ConfirmarReativacao(usuario.Email, "123456");

        Assert.True((await repo.ObterPorId(usuario.Id))!.Ativo);
        Assert.Single(verificationCodes.MarcadosUsados);
        Assert.Contains(notificacoes.Criadas, n => n.UsuarioId == usuario.Id);
    }

    [Fact]
    public async Task ConfirmarReativacao_CodigoInvalido_Lanca()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, _, _, _, _, _, verificationCodes, _) = CriarServico(usuario);
        verificationCodes.NextValidarResult = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarReativacao(usuario.Email, "000000"));
    }

    [Fact]
    public async Task ConfirmarReativacao_ContaJaAtiva_Lanca()
    {
        var usuario = NovoUsuario(ativo: true);
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConfirmarReativacao(usuario.Email, "123456"));
    }

    // ---------- Password recovery ----------

    [Fact]
    public async Task GerarCodigoRecuperacaoSenha_UsuarioExistente_GeraEEnviaEmail()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, email, _, verificationCodes, _) = CriarServico(usuario);

        await service.GerarCodigoRecuperacaoSenha(usuario.Email);

        Assert.True(email.EnviarChamado);
        Assert.Equal(VerificationCodeType.PasswordReset, verificationCodes.Gerados[0].Tipo);
    }

    [Fact]
    public async Task GerarCodigoRecuperacaoSenha_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GerarCodigoRecuperacaoSenha("x@test.com"));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_CodigoInvalido_Lanca()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, _, _, verificationCodes, _) = CriarServico(usuario);
        verificationCodes.NextValidarResult = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmarRecuperacaoSenha(usuario.Email, "000000", "NovaSenha123!"));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.ConfirmarRecuperacaoSenha("x@test.com", "123456", "NovaSenha123!"));
    }

    [Fact]
    public async Task ConfirmarRecuperacaoSenha_CodigoValidoEContaInativa_ReativaEAtualizaSenha()
    {
        var usuario = NovoUsuario(ativo: false);
        var (service, repo, usuarioRepo, userManager, _, _, _, notificacoes) = CriarServico(usuario);
        var identity = new IdentityUser { UserName = usuario.Email, Email = usuario.Email };
        await userManager.CreateAsync(identity, "SenhaAntiga1!");

        await service.ConfirmarRecuperacaoSenha(usuario.Email, "123456", "SenhaNova123!");

        Assert.True((await repo.ObterPorId(usuario.Id))!.Ativo);
        Assert.Contains(notificacoes.Criadas, n => n.Titulo.Contains("Senha"));
    }

    // ---------- RedefinirSenha (authenticated, requires current password) ----------

    [Fact]
    public async Task RedefinirSenha_SenhaAtualIncorreta_Lanca()
    {
        var usuario = NovoUsuario();
        var (service, _, _, userManager, _, _, _, _) = CriarServico(usuario);
        var identity = new IdentityUser { UserName = usuario.Email, Email = usuario.Email };
        await userManager.CreateAsync(identity, "SenhaCorreta1!");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RedefinirSenha(usuario.Email, "SenhaErrada1!", "SenhaNova123!"));
    }

    [Fact]
    public async Task RedefinirSenha_SenhaAtualCorreta_Atualiza()
    {
        var usuario = NovoUsuario();
        var (service, _, _, userManager, _, _, _, notificacoes) = CriarServico(usuario);
        var identity = new IdentityUser { UserName = usuario.Email, Email = usuario.Email };
        await userManager.CreateAsync(identity, "SenhaCorreta1!");

        await service.RedefinirSenha(usuario.Email, "SenhaCorreta1!", "SenhaNova123!");

        Assert.True(await userManager.CheckPasswordAsync(identity, "SenhaNova123!"));
        Assert.Contains(notificacoes.Criadas, n => n.Titulo.Contains("Senha"));
    }

    [Fact]
    public async Task RedefinirSenha_UsuarioSemIdentity_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.RedefinirSenha("naoexiste@test.com", "a", "b"));
    }

    // ---------- Verificar existencia ----------

    [Fact]
    public async Task VerificarEmailExiste_EmailVazio_RetornaFalso()
        => Assert.False(await CriarServico().Service.VerificarEmailExiste(""));

    [Fact]
    public async Task VerificarEmailExiste_EmailCadastrado_RetornaTrue()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        Assert.True(await service.VerificarEmailExiste(usuario.Email));
    }

    [Fact]
    public async Task VerificarCpfExiste_CpfVazio_RetornaFalso()
        => Assert.False(await CriarServico().Service.VerificarCpfExiste(""));

    [Fact]
    public async Task VerificarCpfExiste_CpfCadastrado_RetornaTrue()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        Assert.True(await service.VerificarCpfExiste(usuario.Cpf));
    }

    // ---------- Queries ----------

    [Fact]
    public async Task ObterTodosUsuarios_SemUsuarios_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ObterTodosUsuarios());
    }

    [Fact]
    public async Task ObterTodosUsuarios_ComUsuarios_RetornaLista()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        var resultado = await service.ObterTodosUsuarios();

        Assert.Single(resultado);
    }

    [Fact]
    public async Task ObterAdministradores_SemAdmins_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico(NovoUsuario());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ObterAdministradores());
    }

    [Fact]
    public async Task ObterUsuarioPorId_Inexistente_Lanca()
    {
        var (service, _, _, _, _, _, _, _) = CriarServico();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ObterUsuarioPorId(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterUsuarioPorId_Existente_Retorna()
    {
        var usuario = NovoUsuario();
        var (service, _, _, _, _, _, _, _) = CriarServico(usuario);

        var resultado = await service.ObterUsuarioPorId(usuario.Id);

        Assert.Equal(usuario.Id, resultado.Id);
    }
}
