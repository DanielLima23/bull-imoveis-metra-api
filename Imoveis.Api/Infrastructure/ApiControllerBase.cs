using Imoveis.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Infrastructure;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string RequestId => HttpContext.TraceIdentifier;

    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data)
        => Ok(ApiResponse<T>.Ok(data, RequestId));

    protected ActionResult NotFoundResponse(string message = "Resource not found.")
        => NotFound(ApiResponse<object>.Fail(RequestId, new ApiError("not_found", message)));
}
