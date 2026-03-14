using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class PropertyStatusSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(PropertyIdentitySectionRequest)
            && context.Type != typeof(PropertyStatusUpdateRequest)
            && context.Type != typeof(PropertyDto))
        {
            return;
        }

        SetStringEnum(schema, "status", PropertyStatusContract.StatusValues);
        SetStringEnum(schema, "motivoOciosidade", PropertyStatusContract.IdleReasonValues);
        SetDescription(
            schema,
            "status",
            "Status do imovel. 'alugado' exige locacao ACTIVE vinculada; enquanto houver locacao ACTIVE, o status manual deve permanecer em 'alugado'.");
        SetDescription(schema, "motivoOciosidade", "Obrigatorio apenas quando o status for 'ocioso'.");
        SetDescription(schema, "hasActiveLease", "Indica se o imovel possui locacao ACTIVE vinculada.");
        SetDescription(schema, "activeLeaseId", "Identificador da locacao ACTIVE vinculada, quando existir.");
    }

    private static void SetStringEnum(OpenApiSchema schema, string propertyName, IReadOnlyList<string> values)
    {
        var property = schema.Properties
            .FirstOrDefault(x => string.Equals(x.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (property is null)
        {
            return;
        }

        property.Type = "string";
        property.Enum = values.Select(x => (IOpenApiAny)new OpenApiString(x)).ToList();
    }

    private static void SetDescription(OpenApiSchema schema, string propertyName, string description)
    {
        var property = schema.Properties
            .FirstOrDefault(x => string.Equals(x.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            .Value;

        if (property is null)
        {
            return;
        }

        property.Description = description;
    }
}
