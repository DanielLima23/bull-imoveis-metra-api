using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class PropertyVisit : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public DateTime ScheduledAtUtc { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ResponsibleName { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.SCHEDULED;
    public string? Notes { get; set; }
}
