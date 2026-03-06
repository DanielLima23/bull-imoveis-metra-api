using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Leases;

namespace Imoveis.Application.Abstractions.Services;

public interface ILeaseService
{
    Task<PagedResult<LeaseDto>> QueryAsync(LeaseQueryRequest request, CancellationToken cancellationToken);
    Task<LeaseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<LeaseDto> CreateAsync(LeaseCreateRequest request, CancellationToken cancellationToken);
    Task<LeaseDto?> UpdateAsync(Guid id, LeaseUpdateRequest request, CancellationToken cancellationToken);
    Task<LeaseDto?> CloseAsync(Guid id, LeaseCloseRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaseDto>> HistoryByPropertyAsync(Guid propertyId, CancellationToken cancellationToken);
}
