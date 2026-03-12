using Imoveis.Application.Common;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class PartyKindOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        if (!string.Equals(controller, "Pessoas", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (!string.Equals(parameter.Name, "kind", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parameter.Schema.Type = "string";
            parameter.Schema.Nullable = true;
            parameter.Schema.Enum = PartyKindContract.KindValues
                .Select(x => (IOpenApiAny)new OpenApiString(x))
                .ToList();
        }
    }
}
