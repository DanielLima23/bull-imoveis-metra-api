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
    public PropertyOccupancyStatus OccupancyStatus { get; set; } = PropertyOccupancyStatus.VACANT;
    public PropertyAssetState AssetState { get; set; } = PropertyAssetState.READY;
    public string? Registration { get; set; }
    public string? Scripture { get; set; }
    public string? RegistrationCertification { get; set; }
    public int? NumOfRooms { get; set; }
    public bool CleaningIncluded { get; set; }
    public bool Elevator { get; set; }
    public bool Garage { get; set; }
    public string? Proprietary { get; set; }
    public string? Administrator { get; set; }
    public string? AdministratorPhone { get; set; }
    public string? AdministratorEmail { get; set; }
    public string? AdministrateTax { get; set; }
    public string? Lawyer { get; set; }
    public string? LawyerData { get; set; }
    public string? Observation { get; set; }
    public string? IdleReason { get; set; }
    public DateOnly? UnoccupiedSince { get; set; }

    public ICollection<PropertyRentReference> RentReferences { get; set; } = new List<PropertyRentReference>();
    public ICollection<LeaseContract> Leases { get; set; } = new List<LeaseContract>();
    public ICollection<PropertyExpense> Expenses { get; set; } = new List<PropertyExpense>();
    public ICollection<PendencyItem> Pendencies { get; set; } = new List<PendencyItem>();
    public ICollection<PropertyVisit> Visits { get; set; } = new List<PropertyVisit>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<PropertyChargeTemplate> ChargeTemplates { get; set; } = new List<PropertyChargeTemplate>();
    public ICollection<PropertyHistoryEntry> HistoryEntries { get; set; } = new List<PropertyHistoryEntry>();
    public ICollection<PropertyAttachment> Attachments { get; set; } = new List<PropertyAttachment>();
    public ICollection<PropertyPartyLink> PartyLinks { get; set; } = new List<PropertyPartyLink>();
    public ICollection<PropertyDocument> Documents { get; set; } = new List<PropertyDocument>();
}
