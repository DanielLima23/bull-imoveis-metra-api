namespace Imoveis.Application.Contracts.Leases;

public sealed record LeaseQueryRequest(Guid? PropertyId, Guid? TenantId, string? Status, int Page = 1, int PageSize = 20);

public sealed record LeaseCreateRequest(
    Guid PropertyId,
    Guid TenantId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal MonthlyRent,
    decimal? DepositAmount,
    string? Notes);

public sealed record LeaseUpdateRequest(
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal MonthlyRent,
    decimal? DepositAmount,
    string Status,
    string? Notes);

public sealed record LeaseCloseRequest(DateOnly EndDate);

public sealed record LeaseDto(
    Guid Id,
    Guid PropertyId,
    Guid TenantId,
    string PropertyTitle,
    string TenantName,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal MonthlyRent,
    decimal? DepositAmount,
    string Status,
    string? Notes,
    DateTime CreatedAtUtc);
