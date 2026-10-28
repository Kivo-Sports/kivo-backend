using System.Text.Json;
using kivoBackend.Presentation.Controller;
using kivoBackend.Presentation.Documentation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;

namespace kivoBackend.Tests.Integration;

public class OpenApiDocumentationTests
{
    [Fact]
    public async Task DocumentoCompleto_DescreveOperacoesExemplosEAutenticacaoSemDependenciasExternas()
    {
        // Monta os controllers e o pipeline real de documentação sem o startup de banco/Identity da API.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(KivoOpenApi.Configure);
        await using var app = builder.Build();
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        try
        {
            await app.StartAsync();
            var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
            var operations = document.Paths.SelectMany(path => path.Value.Operations
                .Select(operation => (Path: path.Key, Method: operation.Key, Operation: operation.Value))).ToArray();

            Assert.Equal(99, operations.Length);
            Assert.Equal(operations.Length, operations.Select(item => item.Operation.OperationId).Distinct().Count());
            Assert.All(operations, item =>
            {
                Assert.False(string.IsNullOrWhiteSpace(item.Operation.Summary), $"Sem resumo: {item.Method} {item.Path}");
                Assert.False(string.IsNullOrWhiteSpace(item.Operation.Description), $"Sem descrição: {item.Method} {item.Path}");
                Assert.All(item.Operation.Parameters, parameter =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(parameter.Description));
                    Assert.NotNull(parameter.Example);
                });
                if (item.Operation.RequestBody != null)
                    Assert.All(item.Operation.RequestBody.Content.Values,
                        media => Assert.True(media.Example != null || media.Examples.Count > 0, $"Sem exemplo de entrada: {item.Path}"));
                Assert.All(item.Operation.Responses, response =>
                {
                    Assert.False(string.IsNullOrWhiteSpace(response.Value.Description));
                    Assert.All(response.Value.Content.Values,
                        media => Assert.True(media.Example != null || media.Examples.Count > 0, $"Sem exemplo de resposta: {item.Path} {response.Key}"));
                    if (response.Key == "204" || item.Method == OperationType.Head)
                        Assert.Empty(response.Value.Content);
                });
            });

            // Contratos públicos e protegidos são conferidos por fluxos reais, incluindo o override AllowAnonymous.
            Assert.Empty(document.Paths["/api/auth/login"].Operations[OperationType.Post].Security);
            Assert.Empty(document.Paths["/api/Post"].Operations[OperationType.Get].Security);
            Assert.Empty(document.Paths["/api/Ingresso/partida/{partidaId}/lotes"].Operations[OperationType.Get].Security);
            Assert.Empty(document.Paths["/api/Webhook/asaas"].Operations[OperationType.Post].Security);
            Assert.Empty(document.Paths["/health"].Operations[OperationType.Get].Security);
            Assert.Contains(document.Paths["/api/Ingresso/comprar"].Operations[OperationType.Post].Security,
                requirement => requirement.Keys.Any(scheme => scheme.Reference?.Id == "Bearer"));
            Assert.Contains(document.Paths["/api/Post"].Operations[OperationType.Post].Security,
                requirement => requirement.Keys.Any(scheme => scheme.Reference?.Id == "Bearer"));
            Assert.Contains("Administrador", document.Paths["/api/relatorios/administrativo"].Operations[OperationType.Get].Description);
            Assert.Equal(SecuritySchemeType.Http, document.Components.SecuritySchemes["Bearer"].Type);
            Assert.Equal("bearer", document.Components.SecuritySchemes["Bearer"].Scheme);

            // Serialização final exercita também o documento efetivamente importável por clientes HTTP.
            using var output = new StringWriter();
            document.SerializeAsV3(new OpenApiJsonWriter(output));
            using var json = JsonDocument.Parse(output.ToString());
            Assert.Equal("3.0.1", json.RootElement.GetProperty("openapi").GetString());
            Assert.True(json.RootElement.GetProperty("paths").TryGetProperty("/api/Ingresso/{ingressoId}/estorno", out _));
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
