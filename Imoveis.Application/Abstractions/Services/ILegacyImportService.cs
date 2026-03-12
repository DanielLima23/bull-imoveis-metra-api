using Imoveis.Application.Contracts.Legacy;

namespace Imoveis.Application.Abstractions.Services;

public interface ILegacyImportService
{
    Task<LegacyImportResultDto> ImportAsync(LegacyImportRequest request, CancellationToken cancellationToken);
}
