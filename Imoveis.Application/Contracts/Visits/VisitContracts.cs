namespace Imoveis.Application.Contracts.Visits;

public sealed record VisitQueryRequest(Guid? PropertyId, string? Status, int Page = 1, int PageSize = 20);

public sealed record VisitCreateRequest(Guid PropertyId, DateTime ScheduledAtUtc, string ContactName, string? ContactPhone, string? ResponsibleName, string? Notes);

public sealed record VisitUpdateRequest(DateTime ScheduledAtUtc, string ContactName, string? ContactPhone, string? ResponsibleName, string Status, string? Notes);

public sealed record VisitStatusUpdateRequest(string Status);

public sealed record VisitDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    DateTime ScheduledAtUtc,
    string ContactName,
    string? ContactPhone,
    string? ResponsibleName,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc);
