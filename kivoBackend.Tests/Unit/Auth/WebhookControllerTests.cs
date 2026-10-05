using System.Text;
using kivoBackend.Presentation.Controllers;
using kivoBackend.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace kivoBackend.Tests.Unit.Auth;

public class WebhookControllerTests
{
    private static WebhookController Build(FakeIngressoService ingressos, string body)
    {
        var controller = new WebhookController(ingressos);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public void PingAsaas_RetornaOk()
    {
        var controller = new WebhookController(new FakeIngressoService());

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
        var json = "{\"event\":\"PAYMENT_CONFIRMED\",\"payment\":{\"id\":\"pay_123\"}}";
        var controller = Build(ingressos, json);

        var result = await controller.ReceberWebhookAsaas();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ReceberWebhookAsaas_JsonInvalido_NaoLancaERetornaOk()
    {
        var ingressos = new FakeIngressoService();
        var controller = Build(ingressos, "{ isso nao e json valido");

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
    }

    [Fact]
    public async Task ReceberWebhookAsaas_PayloadSemPaymentId_NaoChamaServico()
    {
        var ingressos = new FakeIngressoService();
        var json = "{\"event\":\"PAYMENT_CONFIRMED\",\"payment\":{\"id\":\"\"}}";
        var controller = Build(ingressos, json);

        Assert.IsType<OkResult>(await controller.ReceberWebhookAsaas());
    }
}
