using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class LeaseContract : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal? DepositAmount { get; set; }
    public string? ContractWith { get; set; }
    public int? PaymentDay { get; set; }
    public string? PaymentLocation { get; set; }
    public string? ReadjustmentIndex { get; set; }
    public string? ContractRegistration { get; set; }
    public string? Insurance { get; set; }
    public string? SignatureRecognition { get; set; }
    public string? OptionalContactName { get; set; }
    public string? OptionalContactPhone { get; set; }
    public string? GuarantorName { get; set; }
    public string? GuarantorDocument { get; set; }
    public string? GuarantorPhone { get; set; }
    public bool CleaningIncluded { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.DRAFT;
    public string? Notes { get; set; }

    public ICollection<LeaseReceivableInstallment> ReceivableInstallments { get; set; } = new List<LeaseReceivableInstallment>();
}
