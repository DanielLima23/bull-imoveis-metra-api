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
    Task<PropertyDetailDto?> GetDetailAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyHistoryEntryDto>> GetHistoryAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyHistoryEntryDto?> AddHistoryAsync(Guid propertyId, PropertyHistoryEntryCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyDocumentDto>> GetDocumentsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyDocumentDto?> AddDocumentAsync(Guid propertyId, PropertyDocumentCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyPartyLinkDto>> GetRelationshipsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyPartyLinkDto?> LinkPartyAsync(Guid propertyId, PropertyPartyLinkCreateRequest request, CancellationToken cancellationToken);
}
