using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PropertyDocument : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "DOCUMENT";
    public string Url { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
