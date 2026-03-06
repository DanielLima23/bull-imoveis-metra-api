namespace Imoveis.Application.Contracts.Properties;

public sealed record PropertyQueryRequest(string? Search, string? Status, int Page = 1, int PageSize = 20);

public sealed record PropertyCreateRequest(
    string Title,
    string AddressLine1,
    string City,
    string State,
    string ZipCode,
    string PropertyType,
    string Status,
    string? Notes,
    decimal? InitialRentAmount,
    DateOnly? InitialRentEffectiveFrom);

public sealed record PropertyUpdateRequest(
    string Title,
    string AddressLine1,
    string City,
    string State,
    string ZipCode,
    string PropertyType,
    string Status,
    string? Notes);

public sealed record PropertyStatusUpdateRequest(string Status);

public sealed record PropertyRentReferenceCreateRequest(decimal Amount, DateOnly EffectiveFrom);

public sealed record PropertyRentReferenceDto(Guid Id, decimal Amount, DateOnly EffectiveFrom);

public sealed record PropertyDto(
    Guid Id,
    string Code,
    string Title,
    string AddressLine1,
    string City,
    string State,
    string ZipCode,
    string PropertyType,
    string Status,
    string? Notes,
    decimal? CurrentBaseRent,
    DateTime CreatedAtUtc);
