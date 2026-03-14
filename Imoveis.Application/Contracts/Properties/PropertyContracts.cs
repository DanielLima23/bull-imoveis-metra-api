using System.Text.Json;
using System.Text.Json.Serialization;

namespace Imoveis.Application.Contracts.Properties;

public sealed record PropertyQueryRequest(
    string? Search,
    string? Status,
    string? MotivoOciosidade,
    string? PropertyType,
    string? City,
    string? Proprietary,
    string? Administrator,
    int Page = 1,
    int PageSize = 20);

public sealed record PropertyIdentitySectionRequest(
    string Title,
    string AddressLine1,
    string City,
    string State,
    string ZipCode,
    string PropertyType,
    string Status,
    string? MotivoOciosidade)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public string? ResolveMotivoOciosidade()
        => PropertyContractAliasResolver.ResolveAlias(MotivoOciosidade, AdditionalData);
}

public sealed record PropertyDocumentationSectionRequest(
    string? Registration,
    string? Scripture,
    string? RegistrationCertification);

public sealed record PropertyCharacteristicsSectionRequest(
    int? NumOfRooms,
    bool Elevator,
    bool Garage);

public sealed record PropertyAdministrationSectionRequest(
    string? Proprietary,
    Guid? ProprietaryPartyId,
    string? Administrator,
    Guid? AdministratorPartyId,
    string? AdministrateTax,
    string? Lawyer,
    Guid? LawyerPartyId,
    string? Observation);

public sealed record PropertyCreateRequest(
    PropertyIdentitySectionRequest Identity,
    PropertyDocumentationSectionRequest Documentation,
    PropertyCharacteristicsSectionRequest Characteristics,
    PropertyAdministrationSectionRequest Administration,
    decimal? InitialRentAmount,
    DateOnly? InitialRentEffectiveFrom);

public sealed record PropertyUpdateRequest(
    PropertyIdentitySectionRequest Identity,
    PropertyDocumentationSectionRequest Documentation,
    PropertyCharacteristicsSectionRequest Characteristics,
    PropertyAdministrationSectionRequest Administration);

public sealed record PropertyStatusUpdateRequest(string Status, string? MotivoOciosidade)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }

    public string? ResolveMotivoOciosidade()
        => PropertyContractAliasResolver.ResolveAlias(MotivoOciosidade, AdditionalData);
}

public sealed record PropertyRentReferenceCreateRequest(decimal Amount, DateOnly EffectiveFrom);

public sealed record PropertyRentReferenceDto(Guid Id, decimal Amount, DateOnly EffectiveFrom);

public sealed record PropertyDocumentationSectionDto(
    string? Registration,
    string? Scripture,
    string? RegistrationCertification);

public sealed record PropertyCharacteristicsSectionDto(
    int? NumOfRooms,
    bool Elevator,
    bool Garage,
    DateOnly? UnoccupiedSince);

public sealed record PropertyAdministrationSectionDto(
    string? Proprietary,
    Guid? ProprietaryPartyId,
    string? Administrator,
    Guid? AdministratorPartyId,
    string? AdministrateTax,
    string? Lawyer,
    Guid? LawyerPartyId,
    string? Observation);

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
    string? MotivoOciosidade,
    string? Proprietary,
    Guid? ProprietaryPartyId,
    string? Administrator,
    Guid? AdministratorPartyId,
    Guid? LawyerPartyId,
    decimal? CurrentBaseRent,
    DateTime CreatedAtUtc,
    PropertyDocumentationSectionDto Documentation,
    PropertyCharacteristicsSectionDto Characteristics,
    PropertyAdministrationSectionDto Administration)
{
    public bool HasActiveLease { get; init; }
    public Guid? ActiveLeaseId { get; init; }
}

public sealed record PropertyChargeTemplateCreateRequest(
    string Kind,
    string Title,
    decimal DefaultAmount,
    int? DueDay,
    string DefaultResponsibility,
    string? ProviderInformation,
    string? Notes,
    bool IsActive);

public sealed record PropertyChargeTemplateUpdateRequest(
    string Kind,
    string Title,
    decimal DefaultAmount,
    int? DueDay,
    string DefaultResponsibility,
    string? ProviderInformation,
    string? Notes,
    bool IsActive);

public sealed record PropertyChargeTemplateDto(
    Guid Id,
    string Kind,
    string Title,
    decimal DefaultAmount,
    int? DueDay,
    string DefaultResponsibility,
    string? ProviderInformation,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record PropertyHistoryEntryCreateRequest(string Content, DateTime? OccurredAtUtc);

public sealed record PropertyHistoryEntryDto(Guid Id, string Content, DateTime OccurredAtUtc, DateTime CreatedAtUtc);

public sealed record PropertyAttachmentCreateRequest(
    string Category,
    string Title,
    string ResourceLocation,
    string? Notes,
    DateTime? ReferenceDateUtc);

public sealed record PropertyAttachmentDto(
    Guid Id,
    string Category,
    string Title,
    string ResourceLocation,
    string? Notes,
    DateTime? ReferenceDateUtc,
    DateTime CreatedAtUtc);

public sealed record PropertyOpenPendencyDto(Guid Id, string Code, string Name, string Title, DateTime DueAtUtc, string Severity, string Status);

public sealed record PropertyUpcomingVisitDto(Guid Id, DateTime ScheduledAtUtc, string ContactName, string Status);

public sealed record PropertyMaintenanceSummaryDto(Guid Id, string Title, string Priority, string Status, decimal? EstimatedCost, decimal? ActualCost);

public sealed record PropertyCurrentLeaseDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal MonthlyRent,
    string Status,
    int? PaymentDay,
    string? PaymentLocation);

public sealed record PropertyDetailDto(
    PropertyDto Property,
    PropertyCurrentLeaseDto? CurrentLease,
    IReadOnlyList<PropertyRentReferenceDto> RentHistory,
    IReadOnlyList<PropertyChargeTemplateDto> ChargeTemplates,
    IReadOnlyList<PropertyHistoryEntryDto> HistoryEntries,
    IReadOnlyList<PropertyAttachmentDto> Attachments,
    IReadOnlyList<PropertyOpenPendencyDto> OpenPendencies,
    IReadOnlyList<PropertyUpcomingVisitDto> UpcomingVisits,
    IReadOnlyList<PropertyMaintenanceSummaryDto> MaintenanceItems);

public sealed record PropertyMonthlyStatementLineDto(
    string SourceType,
    string Kind,
    string Label,
    DateOnly CompetenceDate,
    DateOnly DueDate,
    decimal ExpectedAmount,
    decimal? PaidAmount,
    string Status,
    string? PaidBy,
    string? Notes);

public sealed record PropertyMonthlyStatementDto(
    Guid PropertyId,
    int Year,
    int? Month,
    IReadOnlyList<PropertyMonthlyStatementLineDto> Lines);

internal static class PropertyContractAliasResolver
{
    public static string? ResolveAlias(string? primaryValue, IDictionary<string, JsonElement>? additionalData)
    {
        if (!string.IsNullOrWhiteSpace(primaryValue))
        {
            return primaryValue;
        }

        if (additionalData is null || !additionalData.TryGetValue("idleReason", out var aliasValue))
        {
            return primaryValue;
        }

        return aliasValue.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => aliasValue.GetString(),
            _ => primaryValue
        };
    }
}
