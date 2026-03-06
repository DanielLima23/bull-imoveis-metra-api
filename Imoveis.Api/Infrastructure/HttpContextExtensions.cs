using System.Security.Claims;
using Imoveis.Application.Common;

namespace Imoveis.Api.Infrastructure;

public static class HttpContextExtensions
{
    public static Guid GetRequiredUserId(this HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value) || !Guid.TryParse(value, out var userId))
        {
            throw new AppException("Unauthorized.", 401, "unauthorized");
        }

        return userId;
    }
}
