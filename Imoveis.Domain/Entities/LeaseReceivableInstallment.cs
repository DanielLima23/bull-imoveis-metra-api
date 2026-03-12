using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class LeaseReceivableInstallment : BaseEntity
{
    public Guid LeaseContractId { get; set; }
    public LeaseContract LeaseContract { get; set; } = null!;

    public DateOnly CompetenceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public ReceivableStatus Status { get; set; } = ReceivableStatus.OPEN;
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public string? PaidBy { get; set; }
    public string? Notes { get; set; }
}
