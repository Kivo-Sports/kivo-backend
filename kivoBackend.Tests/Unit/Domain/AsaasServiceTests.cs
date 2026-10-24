using System.Net;
using kivoBackend.Application.Services;
using kivoBackend.Tests.TestSupport;
using Microsoft.Extensions.Configuration;

namespace kivoBackend.Tests.Unit.Domain;

/// <summary>
/// AsaasService talks to the Asaas payment API over HttpClient — the real HTTP boundary is
/// replaced with FakeHttpMessageHandler (see TestSupport/Fakes.cs); the service's own request
/// mapping/response handling/failure handling is what's under test, per the "don't call the
/// real external service" rule.
/// </summary>
public class AsaasServiceTests
{
    private static IConfiguration ConfiguracaoComChave() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Asaas:ApiKey"] = "chave-teste-123" })
        .Build();

    private static IConfiguration ConfiguracaoSemChave() => new ConfigurationBuilder().Build();

    private static AsaasService CriarServico(FakeHttpMessageHandler handler, IConfiguration? config = null)
        => new(new HttpClient(handler), config ?? ConfiguracaoComChave());

    [Fact]
    public async Task ObterOuCriarClienteAsync_SemApiKeyConfigurada_Lanca()
    {
        var service = CriarServico(new FakeHttpMessageHandler(), ConfiguracaoSemChave());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ObterOuCriarClienteAsync("Ana", "12345678901", "ana@test.com"));
    }

    [Fact]
    public async Task ObterOuCriarClienteAsync_ClienteJaExiste_RetornaIdEncontrado()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, "{\"data\":[{\"id\":\"cus_existente\"}]}");
        var service = CriarServico(handler);

        var id = await service.ObterOuCriarClienteAsync("Ana", "52998224725", "ana@test.com");

        Assert.Equal("cus_existente", id);
    }

    [Fact]
    public async Task ObterOuCriarClienteAsync_ClienteNaoExiste_CriaNovo()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, "{\"data\":[]}")
            .Enqueue(HttpStatusCode.OK, "{\"id\":\"cus_novo\"}");
        var service = CriarServico(handler);

        var id = await service.ObterOuCriarClienteAsync("Ana", "52998224725", "ana@test.com");

        Assert.Equal("cus_novo", id);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task ObterOuCriarClienteAsync_FalhaAoCriar_Lanca()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, "{\"data\":[]}")
            .Enqueue(HttpStatusCode.BadRequest, "{\"errors\":[{\"description\":\"CPF inválido\"}]}");
        var service = CriarServico(handler);

        await Assert.ThrowsAsync<Exception>(() => service.ObterOuCriarClienteAsync("Ana", "52998224725", "ana@test.com"));
    }

    [Fact]
    public async Task CriarCobrancaPixAsync_Sucesso_RetornaCobranca()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, "{\"id\":\"pay_1\",\"status\":\"PENDING\",\"value\":100.0}");
        var service = CriarServico(handler);

        var resultado = await service.CriarCobrancaPixAsync("cus_1", 100m, "Ingressos", "ref-1");

        Assert.Equal("pay_1", resultado.Id);
        Assert.Equal("PENDING", resultado.Status);
    }

    [Fact]
    public async Task CriarCobrancaPixAsync_ErroDaApi_Lanca()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.InternalServerError, "{\"errors\":[]}");
        var service = CriarServico(handler);

        await Assert.ThrowsAsync<Exception>(() => service.CriarCobrancaPixAsync("cus_1", 100m, "Ingressos", "ref-1"));
    }

    [Fact]
    public async Task CriarCobrancaPixAsync_SemApiKey_Lanca()
    {
        var service = CriarServico(new FakeHttpMessageHandler(), ConfiguracaoSemChave());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarCobrancaPixAsync("cus_1", 10m, "d", "r"));
    }

    [Fact]
    public async Task ObterQrCodePixAsync_Sucesso_RetornaQrCode()
    {
        var handler = new FakeHttpMessageHandler()
            .Enqueue(HttpStatusCode.OK, "{\"encodedImage\":\"ZmFrZQ==\",\"payload\":\"00020101\"}");
        var service = CriarServico(handler);

        var resultado = await service.ObterQrCodePixAsync("pay_1");

        Assert.Equal("00020101", resultado.Payload);
    }

    [Fact]
    public async Task ObterQrCodePixAsync_Falha_Lanca()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.NotFound, "{}");
        var service = CriarServico(handler);

        await Assert.ThrowsAsync<Exception>(() => service.ObterQrCodePixAsync("pay_inexistente"));
    }

    [Fact]
    public async Task ConsultarStatusCobrancaAsync_Sucesso_RetornaStatus()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.OK, "{\"status\":\"RECEIVED\"}");
        var service = CriarServico(handler);

        Assert.Equal("RECEIVED", await service.ConsultarStatusCobrancaAsync("pay_1"));
    }

    [Fact]
    public async Task ConsultarStatusCobrancaAsync_Falha_Lanca()
    {
        var handler = new FakeHttpMessageHandler().Enqueue(HttpStatusCode.BadGateway, "erro upstream");
        var service = CriarServico(handler);

        await Assert.ThrowsAsync<Exception>(() => service.ConsultarStatusCobrancaAsync("pay_1"));
    }
}
