using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Parties;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class PartyKindSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(PartyCreateRequest)
            && context.Type != typeof(PartyUpdateRequest)
            && context.Type != typeof(PartyDto))
        {
            return;
        }

        var property = schema.Properties
            .FirstOrDefault(x => string.Equals(x.Key, "kind", StringComparison.OrdinalIgnoreCase))
            .Value;

        if (property is null)
        {
            return;
        }

        property.Type = "string";
        property.Enum = PartyKindContract.KindValues
            .Select(x => (IOpenApiAny)new OpenApiString(x))
            .ToList();
    }
}
