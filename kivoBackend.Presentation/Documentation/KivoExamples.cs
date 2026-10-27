using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace kivoBackend.Presentation.Documentation;

// Exemplos sintéticos gerados a partir do schema emitido: nomes e tipos acompanham o contrato serializado.
internal static class KivoExamples
{
    internal static IOpenApiAny ForSchema(OpenApiSchema schema, SchemaRepository repository, string name = "", HashSet<string>? ancestors = null)
    {
        ancestors = ancestors == null ? [] : new HashSet<string>(ancestors);
        var model = schema.Reference?.Id ?? name;
        if (schema.Reference is { } reference)
        {
            if (!ancestors.Add(reference.Id)) return new OpenApiNull();
            schema = repository.Schemas[reference.Id];
        }
        if (schema.AllOf.Count == 1 && schema.Properties.Count == 0)
            return ForSchema(schema.AllOf[0], repository, name, ancestors);
        if (name.Equals("tipoAutorExibicao", StringComparison.OrdinalIgnoreCase)) return new OpenApiInteger(1);
        if (schema.Enum.Count > 0) return schema.Enum[0];
        if (schema.Type == "array") return new OpenApiArray { ForSchema(schema.Items, repository, name, ancestors) };
        if (schema.Type == "object" || schema.Properties.Count > 0)
        {
            var result = new OpenApiObject();
            foreach (var property in schema.Properties)
            {
                // Navegações EF não são necessárias ao cenário de lote/notificação e podem formar ciclos.
                if (property.Key is "usuario" or "partida") result[property.Key] = new OpenApiNull();
                else if (property.Value.Format != "binary") result[property.Key] = ForSchema(property.Value, repository, property.Key, ancestors);
            }
            var nameKey = result.Keys.FirstOrDefault(k => k.Equals("nome", StringComparison.OrdinalIgnoreCase));
            if (nameKey != null)
                result[nameKey] = new OpenApiString(model.Contains("Campeonato") ? "Copa Kivo 2026" : model.Contains("Esporte") ? "Futebol" : model.Contains("Time") ? "Atlético Centro" :
                    model == "ItemQuantidadeDTO" ? (name == "usuariosPorPerfil" ? "Torcedor" : "Futebol") : "Pessoa Exemplo");
            if (model == "ListarUsuarioDTO")
            {
                result["contaBanco"] = new OpenApiNull();
                result["timesAdministrados"] = new OpenApiNull();
                result["organizadorCampeonatoId"] = new OpenApiNull();
                result["organizadorTimeId"] = new OpenApiNull();
            }
            if (model is "CriarTorcedorDto" or "CriarOrganizadorTimeDto") result["contaBanco"] = new OpenApiNull();
            if (model == "CompraIngressosResponseDTO")
            {
                result["pixCopiaCola"] = new OpenApiString("000201-EXEMPLO-SINTETICO-NAO-PAGAVEL");
                result["qrCodeBase64"] = new OpenApiString("data:image/png;base64,iVBORw0KGgo=");
            }
            return result;
        }
        var key = name.ToLowerInvariant();
        if (schema.Type == "boolean") return new OpenApiBoolean(key is "ativo" or "exists" or "lida");
        if (schema.Type is "integer" or "number")
        {
            var number = key switch
            {
                "preco" or "precopago" or "valortotal" or "receita" or "receitaconfirmada" or "gastosconfirmados" => 25,
                "pontosvitoria" => 3,
                "pontosderrota" or "golstimevisitante" or "golsvisitante" or "derrotas" or "empates" or "golscontra" or "golssofridos" or
                "ingressospendentes" or "ingressoscancelados" or "receitasemdatapagamento" or "ingressossemdatapagamento" or "ingressosutilizados" => 0,
                "taxacomparecimento" or "ocupacaopercentual" => 100,
                "quantidadetimesclassificam" => 2,
                _ => 1
            };
            if (schema.Minimum.HasValue) number = Math.Max(number, (int)schema.Minimum.Value);
            return schema.Type == "integer" ? new OpenApiInteger(number) : new OpenApiDouble(number);
        }
        if (schema.Format == "uuid") return new OpenApiString(key switch
        {
            "tipodeficienciaid" => "d1a1b2c3-0001-4000-8000-000000000001",
            "timecasaid" => "33333333-3333-4333-8333-333333333333",
            "timevisitanteid" => "44444444-4444-4444-8444-444444444444",
            _ => "11111111-1111-4111-8111-111111111111"
        });
        if (schema.Format == "date") return new OpenApiString(key is "fim" or "datafim" ? "2026-11-30" : key == "datanascimento" ? "1995-05-10" : "2026-11-01");
        if (schema.Format == "date-time") return new OpenApiString(key switch
        {
            "datanascimento" => "1995-05-10T00:00:00-03:00",
            "fim" or "datafim" => "2026-11-30T23:59:59-03:00",
            "inicio" or "datainicio" => "2026-11-01T00:00:00-03:00",
            "datapartida" or "datahora" => "2026-11-08T15:00:00-03:00",
            "data" => "2026-11-08T00:00:00-03:00",
            _ => "2026-10-26T12:00:00Z"
        });
        return new OpenApiString(key switch
        {
            "nome" => "Pessoa Exemplo",
            "email" or "identificador" or "chavepix" => "pessoa@example.com",
            "senha" or "senhatual" or "senhaatual" => "Exemplo@123",
            "novasenha" => "NovaSenha@123",
            "cpf" => "12345678909",
            "cpftitular" or "nometitular" or "codigovalidacao" or "qrcodebase64" or "pixcopiacola" => "",
            "telefone" => "41999990000",
            "codigo" => "123456",
            "cidade" => "Curitiba",
            "estado" => "PR",
            "cep" => "80000000",
            "rua" => "Rua Exemplo",
            "numero" => "100",
            "complemento" => "Sala 1",
            "pais" => "Brasil",
            "banco" => "Banco Exemplo",
            "agencia" => "0001",
            "conta" => "12345-6",
            "tipo" => "Corrente",
            "cargo" => "Torcedor",
            "nomepartida" or "confronto" => "Atlético Centro x União Esporte",
            "nomelote" or "lote" => "Lote único",
            "nometime" or "timecasa" or "nometimecasa" or "autornome" => "Atlético Centro",
            "timevisitante" or "nometimevisitante" => "União Esporte",
            "nomecampeonato" => "Copa Kivo 2026",
            "esportenome" => "Futebol",
            "icone" or "esporteicone" => "mdi:soccer",
            "local" or "localpartida" => "Ginásio Municipal",
            "fase" => "Nenhuma",
            "status" or "statuscampeonato" => "Rascunho",
            "formatocampeonato" => "PontosCorridos",
            "origem" => "Time",
            "titulo" => "Ingressos disponíveis para a próxima rodada",
            "conteudo" or "mensagem" => "Os ingressos da próxima rodada já estão disponíveis no Kivo.",
            "motivo" => "Não poderei comparecer ao evento.",
            "tipodeficienciadescricao" or "descricao" => "Nenhuma",
            "asaaspaymentid" => "pay_exemplo_001",
            "logo" or "imagem" or "logourl" or "imagemurl" or "logocasa" or "logovisitante" or "logotimecasa" or "logotimevisitante" or "autorimagemurl" => "https://example.com/imagens/time.png",
            "linkredirecionamento" => "/meus-ingressos",
            _ => "Exemplo"
        });
    }

    internal static IOpenApiAny FromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return FromElement(document.RootElement);
    }

    internal static IOpenApiAny ForResponse(OpenApiSchema schema, SchemaRepository repository, string operation)
    {
        var example = ForSchema(schema, repository);
        var role = operation switch
        {
            "Usuario.GetAdministradores" or "Usuario.PostAdmin" or "Usuario.EditarAdmin" => "Administrador",
            "Usuario.PostOrganizadorTime" or "Usuario.EditarOrgTime" => "OrganizadorTime",
            "Usuario.PostOrganizadorCampeonato" or "Usuario.EditarOrgCampeonato" => "OrganizadorCampeonato",
            _ => "Torcedor"
        };
        foreach (var item in example is OpenApiArray array ? array : new OpenApiArray { example })
        {
            if (item is not OpenApiObject obj || !obj.ContainsKey("cargo")) continue;
            obj["cargo"] = new OpenApiString(role);
            if (role == "Administrador") obj["endereco"] = new OpenApiNull();
            if (role == "OrganizadorTime") obj["organizadorTimeId"] = new OpenApiString("55555555-5555-4555-8555-555555555555");
            if (role == "OrganizadorCampeonato")
            {
                obj["organizadorCampeonatoId"] = new OpenApiString("55555555-5555-4555-8555-555555555555");
                obj["contaBanco"] = ForSchema(new OpenApiSchema { Reference = new OpenApiReference { Id = "ContaBancoDTO", Type = ReferenceType.Schema } }, repository);
            }
        }
        return example;
    }

    private static IOpenApiAny FromElement(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => Object(value),
        JsonValueKind.Array => Array(value),
        JsonValueKind.String => new OpenApiString(value.GetString()),
        JsonValueKind.Number => value.TryGetInt32(out var integer) ? new OpenApiInteger(integer) : new OpenApiDouble(value.GetDouble()),
        JsonValueKind.True => new OpenApiBoolean(true),
        JsonValueKind.False => new OpenApiBoolean(false),
        _ => new OpenApiNull()
    };

    private static OpenApiObject Object(JsonElement value)
    {
        var result = new OpenApiObject();
        foreach (var property in value.EnumerateObject()) result[property.Name] = FromElement(property.Value);
        return result;
    }

    private static OpenApiArray Array(JsonElement value)
    {
        var result = new OpenApiArray();
        foreach (var item in value.EnumerateArray()) result.Add(FromElement(item));
        return result;
    }

    internal static OpenApiSchema SchemaFor(IOpenApiAny example) => example switch
    {
        OpenApiObject obj => new OpenApiSchema { Type = "object", Properties = obj.ToDictionary(p => p.Key, p => SchemaFor(p.Value)) },
        OpenApiArray array => new OpenApiSchema { Type = "array", Items = array.Count > 0 ? SchemaFor(array[0]) : new OpenApiSchema() },
        OpenApiInteger => new OpenApiSchema { Type = "integer", Format = "int32" },
        OpenApiDouble => new OpenApiSchema { Type = "number", Format = "double" },
        OpenApiBoolean => new OpenApiSchema { Type = "boolean" },
        OpenApiNull => new OpenApiSchema { Nullable = true },
        _ => new OpenApiSchema { Type = "string" }
    };
}


// Essas navegações são devolvidas como null quando o EF não as carregou nos endpoints de lote/notificação.
public sealed class KivoSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var property = context.Type.Name switch { "IngressoLote" => "partida", "Notificacao" => "usuario", _ => null };
        if (property == null || !schema.Properties.TryGetValue(property, out var navigation)) return;
        schema.Properties[property] = new OpenApiSchema { Nullable = true, AllOf = [navigation] };
    }
}
