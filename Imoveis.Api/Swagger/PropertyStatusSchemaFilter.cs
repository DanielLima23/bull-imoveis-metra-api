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
}
