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
    public LeaseStatus Status { get; set; } = LeaseStatus.DRAFT;
    public string? Notes { get; set; }
}
