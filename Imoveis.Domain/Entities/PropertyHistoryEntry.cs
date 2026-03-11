using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PropertyHistoryEntry : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
