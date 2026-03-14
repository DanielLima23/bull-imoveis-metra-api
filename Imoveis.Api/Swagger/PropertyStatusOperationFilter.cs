using Imoveis.Application.Common;
using Imoveis.Api.Contracts;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Imoveis.Api.Swagger;

public sealed class PropertyStatusOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        var action = context.MethodInfo.Name;

        if (string.Equals(controller, "Imoveis", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var parameter in operation.Parameters)
            {
                if (string.Equals(parameter.Name, "status", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Schema.Type = "string";
                    parameter.Schema.Enum = PropertyStatusContract.StatusValues
                        .Select(x => (IOpenApiAny)new OpenApiString(x))
                        .ToList();
                    parameter.Description = "Filtro por status canonico do imovel.";
                }

                if (string.Equals(parameter.Name, "motivoOciosidade", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.Schema.Type = "string";
                    parameter.Schema.Nullable = true;
                    parameter.Schema.Enum = PropertyStatusContract.IdleReasonValues
                        .Select(x => (IOpenApiAny)new OpenApiString(x))
                        .ToList();
                    parameter.Description = "Motivo de ociosidade, usado apenas quando o status for 'ocioso'.";
                }
            }

            if (string.Equals(action, "Update", StringComparison.Ordinal)
                || string.Equals(action, "UpdateStatus", StringComparison.Ordinal))
            {
                operation.Description = AppendDescription(
                    operation.Description,
                    "O status do imovel depende da existencia e do encerramento da locacao ativa vinculada.");

                operation.Responses["409"] = BuildConflictResponse(
                    context,
                    "Conflito de status do imovel. Codigos possiveis: PROPERTY_REQUIRES_ACTIVE_LEASE, PROPERTY_HAS_ACTIVE_LEASE.",
                    PropertyLeaseErrorCodes.PropertyRequiresActiveLease,
                    "O imovel so pode ser marcado como alugado quando existir uma locacao ativa vinculada.");
            }
        }

        if (string.Equals(controller, "Locacoes", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(action, "Create", StringComparison.Ordinal)
                || string.Equals(action, "Update", StringComparison.Ordinal)))
        {
            operation.Description = AppendDescription(
                operation.Description,
                "Cada imovel pode possuir no maximo uma locacao ACTIVE. Ativar ou criar uma segunda locacao ACTIVE retorna conflito.");

            operation.Responses["409"] = BuildConflictResponse(
                context,
                "Conflito de locacao ativa. Codigo possivel: PROPERTY_ALREADY_HAS_ACTIVE_LEASE.",
                PropertyLeaseErrorCodes.PropertyAlreadyHasActiveLease,
                "Este imovel ja possui uma locacao ativa vinculada.");
        }
    }

    private static OpenApiResponse BuildConflictResponse(
        OperationFilterContext context,
        string description,
        string code,
        string message)
    {
        var schema = context.SchemaGenerator.GenerateSchema(typeof(ApiResponse<object>), context.SchemaRepository);

        return new OpenApiResponse
        {
            Description = description,
            Content =
            {
                ["application/json"] = new OpenApiMediaType
                {
                    Schema = schema,
                    Example = new OpenApiObject
                    {
                        ["success"] = new OpenApiBoolean(false),
                        ["data"] = new OpenApiNull(),
                        ["errors"] = new OpenApiArray
                        {
                            new OpenApiObject
                            {
                                ["code"] = new OpenApiString(code),
                                ["message"] = new OpenApiString(message),
                                ["detail"] = new OpenApiNull()
                            }
                        },
                        ["requestId"] = new OpenApiString("req-123")
                    }
                }
            }
        };
    }

    private static string AppendDescription(string? current, string extra)
    {
        if (string.IsNullOrWhiteSpace(current))
        {
            return extra;
        }

        return $"{current} {extra}";
    }
}
