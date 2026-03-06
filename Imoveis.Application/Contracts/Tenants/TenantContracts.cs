namespace Imoveis.Application.Contracts.Tenants;

public sealed record TenantQueryRequest(string? Search, bool? Active, int Page = 1, int PageSize = 20);

public sealed record TenantCreateRequest(string Name, string DocumentNumber, string Email, string Phone);

public sealed record TenantUpdateRequest(string Name, string DocumentNumber, string Email, string Phone, bool IsActive);

public sealed record TenantDto(
    Guid Id,
    string Name,
    string DocumentNumber,
    string Email,
    string Phone,
    bool IsActive,
    DateTime CreatedAtUtc);
