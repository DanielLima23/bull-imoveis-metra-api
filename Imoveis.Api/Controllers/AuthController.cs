using Imoveis.Api.Contracts;
using Imoveis.Api.Infrastructure;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imoveis.Api.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var data = await _authService.LoginAsync(request, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokenDto>>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var data = await _authService.RefreshAsync(request, cancellationToken);
        return OkResponse(data);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return OkResponse<object>(new { message = "Logged out." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthUserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetRequiredUserId();
        var data = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (data is null)
        {
            return NotFoundResponse("User not found.");
        }

        return OkResponse(data);
    }
}
