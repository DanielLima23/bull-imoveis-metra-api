using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Parties;

namespace Imoveis.Application.Abstractions.Services;

public interface IPartyService
{
    Task<PagedResult<PartyDto>> QueryAsync(PartyQueryRequest request, CancellationToken cancellationToken);
    Task<PartyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PartyDto> CreateAsync(PartyCreateRequest request, CancellationToken cancellationToken);
    Task<PartyDto?> UpdateAsync(Guid id, PartyUpdateRequest request, CancellationToken cancellationToken);
}
