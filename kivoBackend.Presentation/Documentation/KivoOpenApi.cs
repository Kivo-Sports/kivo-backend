using System.Globalization;
using System.Reflection;
using System.Text.Json;
using kivoBackend.Application.DTO.Asaas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace kivoBackend.Presentation.Documentation;

/// <summary>Configuração de documentação; não modifica filtros, autenticação ou respostas HTTP da API.</summary>
public static class KivoOpenApi
{
    public static void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Kivo Sports API",
            Version = "v1",
            Description = "Gestão de campeonatos, times, notícias e ingressos Pix. " +
                "Exemplos são cenários sintéticos, não dados de usuários ou comprovantes de pagamento. " +
                "Substitua os IDs pelos registros do seu ambiente. Enums são números salvo campos de resposta declarados como texto. " +
                "Erros seguem o formato atual de cada operação (texto, objeto message ou ValidationProblemDetails); " +
                "401/403 do middleware podem ter corpo vazio. Login, email, storage e pagamentos precisam das configurações descritas no README."
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Cole apenas o JWT retornado pelo login; o Swagger envia Authorization: Bearer <token>."
        });
        options.UseAllOfToExtendReferenceSchemas();
        options.SchemaFilter<KivoSchemaFilter>();
        options.OperationFilter<KivoOperationFilter>();
    }
}

public sealed class KivoOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType?.Name.Replace("Controller", "", StringComparison.Ordinal);
        var key = controller + "." + method.Name;
        var health = context.ApiDescription.RelativePath == "health";
        var doc = health
            ? new OperationDocumentation("Consultar saúde do processo", "Retorna status ok enquanto o processo HTTP responde. Não consulta banco, Asaas, email ou storage.",
                new ResponseDocumentation(200, "Processo HTTP disponível.", ExampleJson: "{\"status\":\"ok\"}"))
            : KivoOperationCatalog.Operations.TryGetValue(key, out var entry)
                ? entry : throw new InvalidOperationException($"Operação sem documentação: {key}");

        operation.OperationId = health ? "Health" : controller + "_" + method.Name + "_" + context.ApiDescription.HttpMethod;
        operation.Summary = doc.Summary;
        operation.Description = doc.Description;
        operation.Tags = [new OpenApiTag { Name = health ? "Health" : controller }];
        operation.Responses.Clear();
        foreach (var response in doc.Responses)
            AddResponse(operation, response, context);

        // HEAD tem os cabeçalhos/status de GET, mas nunca corpo.
        if (context.ApiDescription.HttpMethod == "HEAD")
            foreach (var response in operation.Responses.Values) response.Content.Clear();

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var anonymous = metadata.OfType<IAllowAnonymous>().Any() || method.IsDefined(typeof(AllowAnonymousAttribute), true)
            || method.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;
        var authorization = metadata.OfType<IAuthorizeData>().ToArray();
        if (!anonymous && authorization.Length != 0)
        {
            operation.Security = [new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
            }];
            operation.Description += "\n\nAutenticação: JWT Bearer obrigatório.";
            var roles = authorization.Where(a => !string.IsNullOrEmpty(a.Roles)).Select(a => a.Roles).Distinct().ToArray();
            if (roles.Length > 0)
            {
                operation.Description += " Perfis permitidos: " + string.Join("; e ", roles) + ".";
                AddResponse(operation, new ResponseDocumentation(403, "Usuário autenticado sem o perfil exigido; corpo pode ser vazio."), context);
            }
            AddResponse(operation, new ResponseDocumentation(401, "JWT ausente, inválido ou expirado; o middleware pode responder sem corpo."), context);
        }
        else
        {
            operation.Security = [];
            operation.Description += "\n\nAcesso público: não exige JWT nesta versão.";
        }

        foreach (var parameter in operation.Parameters)
        {
            parameter.Description ??= ParameterDescription(parameter.Name);
            parameter.Example = parameter.Name.Equals("codigo", StringComparison.OrdinalIgnoreCase)
                ? new OpenApiString("ABCDEF0123456789ABCDEF0123456789")
                : KivoExamples.ForSchema(parameter.Schema, context.SchemaRepository, parameter.Name);
        }

        // O webhook lê Request.Body diretamente: o ApiExplorer não infere seu contrato JSON.
        if (key == "Webhook.ReceberWebhookAsaas")
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Required = false,
                Description = "Evento JSON do Asaas. Exemplo de confirmação; estornos usam PAYMENT_REFUNDED/PAYMENT_REFUND_DENIED.",
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new()
                    {
                        Schema = context.SchemaGenerator.GenerateSchema(typeof(AsaasWebHookPayloadDTO), context.SchemaRepository),
                        Example = KivoExamples.FromJson("{\"event\":\"PAYMENT_CONFIRMED\",\"payment\":{\"id\":\"pay_exemplo_001\",\"status\":\"CONFIRMED\",\"value\":25.00}}")
                    }
                }
            };
        }
        else if (operation.RequestBody is { } body)
        {
            body.Description ??= body.Content.ContainsKey("multipart/form-data")
                ? "Campos enviados como multipart/form-data. Arquivos são opcionais; selecione um arquivo real no Swagger para enviá-lo."
                : "Exemplo sintético de requisição; IDs referenciam registros que devem existir no ambiente.";
            foreach (var content in body.Content.Values)
                content.Example = KivoExamples.ForSchema(content.Schema, context.SchemaRepository, controller ?? "");
        }

        // [ApiController] gera 400 ao falhar binding/validação, independentemente do tratamento da action.
        if (!health && (context.ApiDescription.ParameterDescriptions.Count > 0 || operation.RequestBody != null) && key != "Webhook.ReceberWebhookAsaas")
        {
            AddResponse(operation, new ResponseDocumentation(400, "Binding ou validação automática do ApiController.", ExampleJson:
                "{\"type\":\"https://tools.ietf.org/html/rfc9110#section-15.5.1\",\"title\":\"One or more validation errors occurred.\",\"status\":400,\"errors\":{\"campo\":[\"Valor inválido para o campo informado.\"]}}"), context, "application/problem+json");
        }
    }

    private static void AddResponse(OpenApiOperation operation, ResponseDocumentation doc, OperationFilterContext context, string? media = null)
    {
        var status = doc.Status.ToString(CultureInfo.InvariantCulture);
        if (!operation.Responses.TryGetValue(status, out var response))
        {
            response = new OpenApiResponse { Description = doc.Description };
            operation.Responses[status] = response;
        }
        else if (!response.Description.Contains(doc.Description, StringComparison.Ordinal))
            response.Description += " " + doc.Description;
        if (doc.BodyType == null && doc.ExampleJson == null) return;

        var example = doc.ExampleJson != null ? KivoExamples.FromJson(doc.ExampleJson) : null;
        var schema = doc.BodyType != null
            ? context.SchemaGenerator.GenerateSchema(doc.BodyType, context.SchemaRepository)
            : KivoExamples.SchemaFor(example!);
        example ??= KivoExamples.ForResponse(schema, context.SchemaRepository, context.MethodInfo.DeclaringType?.Name.Replace("Controller", "", StringComparison.Ordinal) + "." + context.MethodInfo.Name);
        if (doc.Status == 201)
            response.Headers["Location"] = new OpenApiHeader { Description = "URL de consulta do recurso criado.", Schema = new OpenApiSchema { Type = "string", Format = "uri-reference" } };
        var isText = schema.Type == "string";
        var contentType = media ?? (isText ? "text/plain" : "application/json");
        var content = new OpenApiMediaType { Schema = schema, Example = example };
        if (response.Content.TryGetValue(contentType, out var existing))
        {
            // Preserva os dois formatos quando a mesma action retorna texto e objeto message no mesmo status.
            existing.Schema = new OpenApiSchema { OneOf = [existing.Schema, schema] };
            existing.Examples["alternativo"] = new OpenApiExample { Value = example };
            existing.Examples["principal"] = new OpenApiExample { Value = existing.Example };
            existing.Example = null;
        }
        else response.Content[contentType] = content;
        if (isText && media == null) response.Content.TryAdd("application/json", new OpenApiMediaType { Schema = schema, Example = example });
    }

    private static string ParameterDescription(string name) => name.ToLowerInvariant() switch
    {
        "inicio" => "Primeiro dia do período (inclusivo), em America/Sao_Paulo. Omitir para não limitar o início.",
        "fim" => "Último dia do período (inclusivo), em America/Sao_Paulo; deve ser igual ou posterior ao início.",
        "esporteid" => "ID da modalidade; no relatório administrativo não filtra os dados de usuários.",
        "organizadortimeid" => "ID do perfil de organizador de time, não do usuário.",
        "cpf" => "CPF usado na consulta, conforme o formato aceito pela operação.",
        "codigo" => "Código de validação do QR Code de entrada: 32 caracteres hexadecimais, sem hífens.",
        _ => "Identificador do recurso (UUID) recebido na rota. Substitua pelo ID do seu ambiente."
    };
}
