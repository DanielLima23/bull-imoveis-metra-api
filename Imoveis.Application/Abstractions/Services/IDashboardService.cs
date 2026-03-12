using Imoveis.Application.Contracts.Dashboard;

namespace Imoveis.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<RealEstateDashboardDto> GetAsync(int month, int year, CancellationToken cancellationToken);
}
