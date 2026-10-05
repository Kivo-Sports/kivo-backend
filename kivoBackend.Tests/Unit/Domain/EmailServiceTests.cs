using kivoBackend.Application.Services;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// EmailService sends real SMTP mail with no injectable transport boundary, so — per the
/// "don't call the real external service" rule — only the parts reachable without an actual
/// network call are exercised here: the SMTP-settings guard clause (EnviarEmailComCodigoAsync /
/// EnviarCodigoReativacaoAsync) and the fast, local argument validation SmtpClient itself
/// performs before any I/O (EnviarEmailNotificacaoAsync).
/// </summary>
public class EmailServiceTests
{
    private static IConfiguration ConfiguracaoIncompleta() => new ConfigurationBuilder().Build();

    [Fact]
    public async Task EnviarEmailComCodigoAsync_SemConfiguracaoSmtp_Lanca()
    {
        var service = new EmailService(ConfiguracaoIncompleta());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnviarEmailComCodigoAsync("ana@test.com", "Ana", "123456", "Titulo", "Mensagem"));
    }

    [Fact]
    public async Task EnviarCodigoReativacaoAsync_SemConfiguracaoSmtp_Lanca()
    {
        var service = new EmailService(ConfiguracaoIncompleta());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.EnviarCodigoReativacaoAsync("ana@test.com", "Ana", "123456"));
    }

    [Fact]
    public async Task EnviarEmailNotificacaoAsync_SemServidorConfigurado_LancaAoConstruirClienteSmtp()
    {
        var service = new EmailService(ConfiguracaoIncompleta());

        // Sem SmtpServer configurado, o próprio SmtpClient rejeita o host nulo/vazio antes de
        // qualquer I/O — não há chamada de rede real.
        await Assert.ThrowsAnyAsync<Exception>(() =>
            service.EnviarEmailNotificacaoAsync("ana@test.com", "Ana", "Titulo", "Mensagem"));
    }
}
