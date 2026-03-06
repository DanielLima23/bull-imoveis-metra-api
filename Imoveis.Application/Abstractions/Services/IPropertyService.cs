using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;

namespace Imoveis.Application.Abstractions.Services;

public interface IPropertyService
{
    Task<PagedResult<PropertyDto>> QueryAsync(PropertyQueryRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PropertyDto> CreateAsync(PropertyCreateRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> UpdateAsync(Guid id, PropertyUpdateRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> UpdateStatusAsync(Guid id, PropertyStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<PropertyRentReferenceDto?> AddRentReferenceAsync(Guid propertyId, PropertyRentReferenceCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyRentReferenceDto>> GetRentHistoryAsync(Guid propertyId, CancellationToken cancellationToken);
}
