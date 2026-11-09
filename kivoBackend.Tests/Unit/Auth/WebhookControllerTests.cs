using System.Text;
using kivoBackend.Presentation.Controllers;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace kivoBackend.Tests.Unit.Auth;

public class WebhookControllerTests
{
    private const string PayloadValido = "{\"event\":\"PAYMENT_CONFIRMED\",\"payment\":{\"id\":\"pay_123\"}}";

    private static IConfiguration Config(string? tokenWebhook = null) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ASAAS_WEBHOOK_TOKEN"] = tokenWebhook })
            .Build();

    private static WebhookController Build(FakeIngressoService ingressos, string body, string? tokenConfigurado = null, string? tokenRecebido = null)
    {
        var controller = new WebhookController(ingressos, Config(tokenConfigurado));
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        if (tokenRecebido != null)
            httpContext.Request.Headers["asaas-access-token"] = new StringValues(tokenRecebido);
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public void PingAsaas_RetornaOk()
    {
        var controller = new WebhookController(new FakeIngressoService(), Config());

        Assert.IsType<OkObjectResult>(controller.PingAsaas());
    }

    [Fact]
    public async Task ReceberWebhookAsaas_CorpoVazio_RetornaOk()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, "");

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
    }

    [Fact]
    public async Task ReceberWebhookAsaas_PayloadValido_ProcessaEChamaServico()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, PayloadValido);

        var result = await controller.ReceberWebhookAsaas();

        Assert.IsType<OkResult>(result);
        Assert.Equal(1, ingressos.ProcessarWebhookChamadas);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_JsonInvalido_NaoLancaERetornaOk()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, "{ isso nao e json valido");

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
        Assert.Equal(0, ingressos.ProcessarWebhookChamadas);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_PayloadSemPaymentId_NaoChamaServico()
    {
        var ingressos = new FakeIngressoService();
        var json = "{\"event\":\"PAYMENT_CONFIRMED\",\"payment\":{\"id\":\"\"}}";
        var controller = Build(ingressos, json);

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
        Assert.Equal(0, ingressos.ProcessarWebhookChamadas);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_FalhaAoProcessar_Retorna500ParaAsaasReenviar()
    {
        var ingressos = new FakeIngressoService { ThrowOnProcessarWebhook = new Exception("Asaas indisponível") };
        var controller = Build(ingressos, PayloadValido);

        var result = await controller.ReceberWebhookAsaas();

        Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    // ─── Token de autenticação (ASAAS_WEBHOOK_TOKEN) ─────────────────────────

    [Fact]
    public async Task ReceberWebhookAsaas_TokenConfiguradoEHeaderCorreto_Processa()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, PayloadValido, tokenConfigurado: "token-secreto", tokenRecebido: "token-secreto");

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
        Assert.Equal(1, ingressos.ProcessarWebhookChamadas);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_TokenConfiguradoSemHeader_Retorna401SemProcessar()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, PayloadValido, tokenConfigurado: "token-secreto");

        Assert.IsType<UnauthorizedResult>(await controller.ReceberWebhookAsaas());
        Assert.Equal(0, ingressos.ProcessarWebhookChamadas);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_TokenConfiguradoEHeaderErrado_Retorna401SemProcessar()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, PayloadValido, tokenConfigurado: "token-secreto", tokenRecebido: "token-forjado");

        Assert.IsType<UnauthorizedResult>(await controller.ReceberWebhookAsaas());
        Assert.Equal(0, ingressos.ProcessarWebhookChamadas);
    }
}
