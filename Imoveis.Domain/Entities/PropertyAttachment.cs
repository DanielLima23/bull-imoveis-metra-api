using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PropertyAttachment : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ResourceLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? ReferenceDateUtc { get; set; }
}
