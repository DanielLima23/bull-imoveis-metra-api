using Imoveis.Application.Contracts.Auth;

namespace Imoveis.Application.Abstractions.Services;

public interface IAuthService
{
    Task<AuthTokenDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthTokenDto> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}
