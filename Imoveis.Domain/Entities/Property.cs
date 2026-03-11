using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class Property : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; } = PropertyStatus.AVAILABLE;
    public string? Notes { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? DeedNumber { get; set; }
    public string? RegistrationCertificate { get; set; }
    public int? Bedrooms { get; set; }
    public bool HasElevator { get; set; }
    public bool HasGarage { get; set; }
    public DateOnly? VacatedAt { get; set; }
    public string? VacancyReason { get; set; }

    public ICollection<PropertyRentReference> RentReferences { get; set; } = new List<PropertyRentReference>();
    public ICollection<LeaseContract> Leases { get; set; } = new List<LeaseContract>();
    public ICollection<PropertyExpense> Expenses { get; set; } = new List<PropertyExpense>();
    public ICollection<PendencyItem> Pendencies { get; set; } = new List<PendencyItem>();
    public ICollection<PropertyVisit> Visits { get; set; } = new List<PropertyVisit>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<PropertyPartyLink> PartyLinks { get; set; } = new List<PropertyPartyLink>();
    public ICollection<PropertyHistoryEntry> HistoryEntries { get; set; } = new List<PropertyHistoryEntry>();
    public ICollection<PropertyDocument> Documents { get; set; } = new List<PropertyDocument>();
}
