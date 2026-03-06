using Imoveis.Domain.Entities;

namespace Imoveis.Application.Abstractions.Security;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshTokenRaw();
    string ComputeRefreshTokenHash(string rawToken);
}
