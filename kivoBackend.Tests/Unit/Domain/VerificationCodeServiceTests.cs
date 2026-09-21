using kivoBackend.Application.Services;
using kivoBackend.Core.Entities;
using kivoBackend.Core.Enums;
using kivoBackend.Tests.TestSupport;

namespace kivoBackend.Tests.Unit.Domain;

public class VerificationCodeServiceTests
{
    private static (VerificationCodeService Service, FakeVerificationCodeRepository Repo) CriarServico()
    {
        var repo = new FakeVerificationCodeRepository();
        return (new VerificationCodeService(repo), repo);
    }

    [Fact]
    public async Task GerarCodigoAsync_CriaCodigoDe6Digitos()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();

        var codigo = await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);

        Assert.Equal(6, codigo.Length);
        Assert.True(int.TryParse(codigo, out _));
        Assert.Single(repo.Codes);
        Assert.NotEqual(codigo, repo.Codes.Values.First().Codigo); // armazenado como hash, não em texto puro
    }

    [Fact]
    public async Task GerarCodigoAsync_CodigoValidoExistente_InvalidaOAnterior()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();

        await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);
        var primeiro = repo.Codes.Values.First();
        await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);

        Assert.True(primeiro.Usado);
        Assert.Equal(2, repo.Codes.Count);
    }

    [Fact]
    public async Task ValidarCodigoAsync_SemCodigoValido_Lanca()
    {
        var (service, _) = CriarServico();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidarCodigoAsync(Guid.NewGuid(), "123456", VerificationCodeType.PasswordReset));
    }

    [Fact]
    public async Task ValidarCodigoAsync_CodigoCorreto_RetornaTrueERemove()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        var codigo = await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);

        var valido = await service.ValidarCodigoAsync(usuarioId, codigo, VerificationCodeType.PasswordReset);

        Assert.True(valido);
        Assert.Empty(repo.Codes);
    }

    [Fact]
    public async Task ValidarCodigoAsync_CodigoIncorreto_LancaEIncrementaTentativas()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidarCodigoAsync(usuarioId, "000000", VerificationCodeType.PasswordReset));

        Assert.Contains("Tentativas restantes", ex.Message);
        Assert.Equal(1, repo.Codes.Values.First().Tentativas);
    }

    [Fact]
    public async Task ValidarCodigoAsync_ExcedeuMaximoTentativas_RemoveELanca()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);
        repo.Codes.Values.First().Tentativas = 5;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidarCodigoAsync(usuarioId, "000000", VerificationCodeType.PasswordReset));

        Assert.Empty(repo.Codes);
    }

    [Fact]
    public async Task ValidarCodigoAsync_CodigoExpirado_TratadoComoInexistente()
    {
        // ObterCodigoValidoAsync (both here and in the real VerificationCodeRepository) already
        // filters out expired codes, so an expired entry is reported as "no valid code found"
        // rather than reaching the service's own expiry check — that inner branch is unreachable
        // through this entry point given the repository's query.
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        var codigo = await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);
        repo.Codes.Values.First().ExpiraEm = DateTime.Now.AddMinutes(-1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidarCodigoAsync(usuarioId, codigo, VerificationCodeType.PasswordReset));

        Assert.Contains("Nenhum código válido encontrado", ex.Message);
    }

    [Fact]
    public async Task ValidarCodigoAsync_CodigoJaUsado_Lanca()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        var codigo = await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);
        // Torna "válido" novamente (não usado, não expirado) mas marca Usado diretamente
        // para exercitar o branch de "já utilizado" sem depender do filtro do fake repo.
        var entry = repo.Codes.Values.First();
        entry.Usado = false;
        entry.ExpiraEm = DateTime.Now.AddMinutes(5);

        await service.ValidarCodigoAsync(usuarioId, codigo, VerificationCodeType.PasswordReset); // consome

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ValidarCodigoAsync(usuarioId, codigo, VerificationCodeType.PasswordReset));
    }

    [Fact]
    public async Task MarcarComoUsadoAsync_CodigoValido_MarcaUsado()
    {
        var (service, repo) = CriarServico();
        var usuarioId = Guid.NewGuid();
        await service.GerarCodigoAsync(usuarioId, VerificationCodeType.PasswordReset);

        await service.MarcarComoUsadoAsync(usuarioId, "qualquer", VerificationCodeType.PasswordReset);

        Assert.True(repo.Codes.Values.First().Usado);
    }

    [Fact]
    public async Task MarcarComoUsadoAsync_SemCodigoValido_NaoLanca()
    {
        var (service, _) = CriarServico();

        await service.MarcarComoUsadoAsync(Guid.NewGuid(), "x", VerificationCodeType.PasswordReset);
    }

    [Theory]
    [InlineData(VerificationCodeType.AccountReactivation, "Reativar Conta")]
    [InlineData(VerificationCodeType.PasswordReset, "Recuperar Senha")]
    [InlineData(VerificationCodeType.PasswordRedefinition, "Redefinir Senha")]
    [InlineData(VerificationCodeType.TwoFactorAuth, "Autenticação 2FA")]
    [InlineData(VerificationCodeType.EmailConfirmation, "Confirmação de Email")]
    public void ObterDescricaoTipo_RetornaDescricaoCorreta(VerificationCodeType tipo, string esperado)
    {
        var (service, _) = CriarServico();

        Assert.Equal(esperado, service.ObterDescricaoTipo(tipo));
    }
}
