using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Pendencies;

namespace Imoveis.Application.Abstractions.Services;

public interface IPendencyService
{
    Task<IReadOnlyList<PendencyTypeDto>> ListTypesAsync(CancellationToken cancellationToken);
    Task<PendencyTypeDto> CreateTypeAsync(PendencyTypeCreateRequest request, CancellationToken cancellationToken);
    Task<PendencyTypeDto?> UpdateTypeAsync(Guid id, PendencyTypeUpdateRequest request, CancellationToken cancellationToken);

    Task<PagedResult<PendencyDto>> QueryAsync(PendencyQueryRequest request, CancellationToken cancellationToken);
    Task<PendencyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PendencyDto> CreateAsync(PendencyCreateRequest request, CancellationToken cancellationToken);
    Task<PendencyDto?> UpdateAsync(Guid id, PendencyUpdateRequest request, CancellationToken cancellationToken);
    Task<PendencyDto?> ResolveAsync(Guid id, PendencyResolveRequest request, CancellationToken cancellationToken);
}
