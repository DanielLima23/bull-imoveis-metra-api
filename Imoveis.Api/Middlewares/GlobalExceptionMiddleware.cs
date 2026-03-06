using System.Net;
using System.Text.Json;
using Imoveis.Api.Contracts;
using Imoveis.Application.Common;

namespace Imoveis.Api.Middlewares;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteAsync(context, (int)HttpStatusCode.InternalServerError, "internal_error", "Internal server error.", null);
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string code, string message, object? detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var requestId = context.TraceIdentifier;
        var payload = ApiResponse<object>.Fail(requestId, new ApiError(code, message, detail));

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
