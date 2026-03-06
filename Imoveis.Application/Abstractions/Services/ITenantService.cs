using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Tenants;

namespace Imoveis.Application.Abstractions.Services;

public interface ITenantService
{
    Task<PagedResult<TenantDto>> QueryAsync(TenantQueryRequest request, CancellationToken cancellationToken);
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TenantDto> CreateAsync(TenantCreateRequest request, CancellationToken cancellationToken);
    Task<TenantDto?> UpdateAsync(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken);
}
