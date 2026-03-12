using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PropertyHistoryEntry : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
