using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Visits;

namespace Imoveis.Application.Abstractions.Services;

public interface IVisitService
{
    Task<PagedResult<VisitDto>> QueryAsync(VisitQueryRequest request, CancellationToken cancellationToken);
    Task<VisitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<VisitDto> CreateAsync(VisitCreateRequest request, CancellationToken cancellationToken);
    Task<VisitDto?> UpdateAsync(Guid id, VisitUpdateRequest request, CancellationToken cancellationToken);
    Task<VisitDto?> UpdateStatusAsync(Guid id, VisitStatusUpdateRequest request, CancellationToken cancellationToken);
}
