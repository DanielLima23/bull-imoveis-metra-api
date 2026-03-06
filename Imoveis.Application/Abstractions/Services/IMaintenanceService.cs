using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Maintenance;

namespace Imoveis.Application.Abstractions.Services;

public interface IMaintenanceService
{
    Task<PagedResult<MaintenanceDto>> QueryAsync(MaintenanceQueryRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MaintenanceDto> CreateAsync(MaintenanceCreateRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto?> UpdateAsync(Guid id, MaintenanceUpdateRequest request, CancellationToken cancellationToken);
    Task<MaintenanceDto?> UpdateStatusAsync(Guid id, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken);
}
