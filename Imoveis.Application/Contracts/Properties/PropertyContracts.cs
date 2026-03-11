using Imoveis.Application.Contracts.Leases;
using Imoveis.Application.Contracts.Pendencies;

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
    string? RegistrationNumber,
    string? DeedNumber,
    string? RegistrationCertificate,
    int? Bedrooms,
    bool HasElevator,
    bool HasGarage,
    DateOnly? VacatedAt,
    string? VacancyReason,
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
    string? Notes,
    string? RegistrationNumber,
    string? DeedNumber,
    string? RegistrationCertificate,
    int? Bedrooms,
    bool HasElevator,
    bool HasGarage,
    DateOnly? VacatedAt,
    string? VacancyReason);

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
    string? RegistrationNumber,
    string? DeedNumber,
    string? RegistrationCertificate,
    int? Bedrooms,
    bool HasElevator,
    bool HasGarage,
    DateOnly? VacatedAt,
    string? VacancyReason,
    decimal? CurrentBaseRent,
    DateTime CreatedAtUtc);

public sealed record PropertyHistoryEntryCreateRequest(string Title, string? Description, DateTime OccurredAtUtc);

public sealed record PropertyHistoryEntryDto(Guid Id, string Title, string? Description, DateTime OccurredAtUtc, DateTime CreatedAtUtc);

public sealed record PropertyDocumentCreateRequest(string Name, string Kind, string Url, string? Notes);

public sealed record PropertyDocumentDto(Guid Id, string Name, string Kind, string Url, string? Notes, DateTime CreatedAtUtc);

public sealed record PropertyPartyLinkCreateRequest(Guid PartyId, string Role, bool IsPrimary, DateTime? StartsAtUtc, DateTime? EndsAtUtc, string? Notes);

public sealed record PropertyPartyLinkDto(
    Guid Id,
    Guid PropertyId,
    Guid PartyId,
    string Role,
    bool IsPrimary,
    DateTime? StartsAtUtc,
    DateTime? EndsAtUtc,
    string? Notes,
    string PartyKind,
    string PartyName,
    string? PartyDocumentNumber,
    string? PartyEmail,
    string? PartyPhone);

public sealed record PropertyFinancialInstallmentDto(
    Guid InstallmentId,
    string ExpenseTypeName,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    string Status);

public sealed record PropertyFinancialOverviewDto(
    decimal? CurrentBaseRent,
    decimal? ActiveLeaseRent,
    decimal OpenExpenseAmount,
    decimal OverdueExpenseAmount,
    IReadOnlyList<PropertyFinancialInstallmentDto> UpcomingInstallments);

public sealed record PropertyDetailDto(
    PropertyDto Property,
    LeaseDto? ActiveLease,
    IReadOnlyList<LeaseDto> LeaseHistory,
    PropertyFinancialOverviewDto Financial,
    IReadOnlyList<PendencyDto> OpenPendencies,
    IReadOnlyList<PropertyHistoryEntryDto> History,
    IReadOnlyList<PropertyDocumentDto> Documents,
    IReadOnlyList<PropertyPartyLinkDto> Relationships,
    IReadOnlyList<PropertyRentReferenceDto> RentHistory);
