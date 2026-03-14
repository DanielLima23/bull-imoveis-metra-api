using Imoveis.Application.Contracts.Settings;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class SystemSettingsSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(SystemSettingsDto)
            && context.Type != typeof(SystemSettingsUpdateRequest))
        {
            return;
        }

        SetDescription(
            schema,
            "enableGuidedFlows",
            "Ativa os fluxos guiados do sistema para locacoes e demais jornadas configuraveis. Default: false.");
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
