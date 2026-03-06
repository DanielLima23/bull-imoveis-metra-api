using Imoveis.Application.Contracts.Settings;

namespace Imoveis.Application.Abstractions.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetPublicAsync(CancellationToken cancellationToken);
    Task<SystemSettingsDto> GetAsync(CancellationToken cancellationToken);
    Task<SystemSettingsDto> UpdateAsync(SystemSettingsUpdateRequest request, CancellationToken cancellationToken);
}
