using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class PendencyItem : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid PendencyTypeId { get; set; }
    public PendencyType PendencyType { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public PendencyStatus Status { get; set; } = PendencyStatus.OPEN;
}
