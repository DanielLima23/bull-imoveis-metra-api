using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class MaintenanceRequest : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.MEDIUM;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.OPEN;

    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}
