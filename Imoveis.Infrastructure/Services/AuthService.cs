using Imoveis.Application.Abstractions.Security;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Auth;
using Imoveis.Domain.Entities;
using Imoveis.Infrastructure.Options;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Imoveis.Infrastructure.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthTokenDto> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email && x.IsActive, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("Invalid credentials.", 401, "invalid_credentials");
        }

        return await CreateTokensAsync(user, cancellationToken);
    }

    public async Task<AuthTokenDto> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _tokenService.ComputeRefreshTokenHash(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (refreshToken is null || refreshToken.IsRevoked || refreshToken.IsExpired || !refreshToken.User.IsActive)
        {
            throw new AppException("Invalid refresh token.", 401, "invalid_refresh_token");
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await CreateTokensAsync(refreshToken.User, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = _tokenService.ComputeRefreshTokenHash(request.RefreshToken);

        var refreshToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (refreshToken is null)
        {
            return;
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);

        return user is null
            ? null
            : new AuthUserDto(user.Id, user.Name, user.Email, user.Role.ToString());
    }

    private async Task<AuthTokenDto> CreateTokensAsync(User user, CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.GenerateAccessToken(user);
        var accessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var refreshRawToken = _tokenService.GenerateRefreshTokenRaw();
        var refreshHash = _tokenService.ComputeRefreshTokenHash(refreshRawToken);
        var refreshExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshExpiryDays);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAtUtc = refreshExpiresAtUtc
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokenDto(
            accessToken,
            accessTokenExpiresAtUtc,
            refreshRawToken,
            refreshExpiresAtUtc,
            new AuthUserDto(user.Id, user.Name, user.Email, user.Role.ToString()));
    }
}
