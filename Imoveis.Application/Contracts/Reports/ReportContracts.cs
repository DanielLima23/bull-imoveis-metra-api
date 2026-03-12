namespace Imoveis.Application.Contracts.Reports;

public sealed record ReportCsvDto(string FileName, string ContentType, byte[] Content);

public sealed record ReportCatalogItemDto(string Slug, string Name, string Description, bool RequiresMonth, bool RequiresYear);
