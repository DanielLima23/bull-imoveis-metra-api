using Imoveis.Application.Common;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class PropertyStatusOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        if (!string.Equals(controller, "Imoveis", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (string.Equals(parameter.Name, "status", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Schema.Type = "string";
                parameter.Schema.Enum = PropertyStatusContract.StatusValues
                    .Select(x => (IOpenApiAny)new OpenApiString(x))
                    .ToList();
            }

            if (string.Equals(parameter.Name, "motivoOciosidade", StringComparison.OrdinalIgnoreCase))
            {
                parameter.Schema.Type = "string";
                parameter.Schema.Nullable = true;
                parameter.Schema.Enum = PropertyStatusContract.IdleReasonValues
                    .Select(x => (IOpenApiAny)new OpenApiString(x))
                    .ToList();
            }
        }
    }
}
