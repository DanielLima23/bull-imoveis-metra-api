using Imoveis.Application.Contracts.Reports;

namespace Imoveis.Application.Abstractions.Services;

public interface IReportService
{
    Task<IReadOnlyList<ReportCatalogItemDto>> ListCatalogAsync(CancellationToken cancellationToken);
    Task<ReportCsvDto> BuildBySlugAsync(string slug, int? month, int? year, CancellationToken cancellationToken);
    Task<ReportCsvDto> BuildFinancialCsvAsync(int month, int year, CancellationToken cancellationToken);
    Task<ReportCsvDto> BuildVacancyCsvAsync(int month, int year, CancellationToken cancellationToken);
    Task<ReportCsvDto> BuildPendenciesCsvAsync(CancellationToken cancellationToken);
}
