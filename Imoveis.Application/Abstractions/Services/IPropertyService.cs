using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;

namespace Imoveis.Application.Abstractions.Services;

public interface IPropertyService
{
    Task<PagedResult<PropertyDto>> QueryAsync(PropertyQueryRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PropertyDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken);
    Task<PropertyDto> CreateAsync(PropertyCreateRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> UpdateAsync(Guid id, PropertyUpdateRequest request, CancellationToken cancellationToken);
    Task<PropertyDto?> UpdateStatusAsync(Guid id, PropertyStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<PropertyRentReferenceDto?> AddRentReferenceAsync(Guid propertyId, PropertyRentReferenceCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyRentReferenceDto>> GetRentHistoryAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyChargeTemplateDto>> ListChargeTemplatesAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyChargeTemplateDto?> AddChargeTemplateAsync(Guid propertyId, PropertyChargeTemplateCreateRequest request, CancellationToken cancellationToken);
    Task<PropertyChargeTemplateDto?> UpdateChargeTemplateAsync(Guid propertyId, Guid templateId, PropertyChargeTemplateUpdateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyHistoryEntryDto>> ListHistoryAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyHistoryEntryDto?> AddHistoryAsync(Guid propertyId, PropertyHistoryEntryCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PropertyAttachmentDto>> ListAttachmentsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<PropertyAttachmentDto?> AddAttachmentAsync(Guid propertyId, PropertyAttachmentCreateRequest request, CancellationToken cancellationToken);
    Task<PropertyMonthlyStatementDto?> GetMonthlyStatementAsync(Guid propertyId, int year, int? month, CancellationToken cancellationToken);
}
