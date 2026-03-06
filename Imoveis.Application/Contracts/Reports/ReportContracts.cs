namespace Imoveis.Application.Contracts.Reports;

public sealed record ReportCsvDto(string FileName, string ContentType, byte[] Content);
