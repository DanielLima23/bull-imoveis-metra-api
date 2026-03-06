namespace Imoveis.Application.Contracts.Maintenance;

public sealed record MaintenanceQueryRequest(Guid? PropertyId, string? Status, string? Priority, int Page = 1, int PageSize = 20);

public sealed record MaintenanceCreateRequest(
    Guid PropertyId,
    string Title,
    string Description,
    string Priority,
    decimal? EstimatedCost,
    string? Notes);

public sealed record MaintenanceUpdateRequest(
    string Title,
    string Description,
    string Priority,
    decimal? EstimatedCost,
    decimal? ActualCost,
    string Status,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? Notes);

public sealed record MaintenanceStatusUpdateRequest(string Status);

public sealed record MaintenanceDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    string Title,
    string Description,
    string Priority,
    string Status,
    decimal? EstimatedCost,
    decimal? ActualCost,
    DateTime RequestedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? Notes,
    DateTime CreatedAtUtc);
