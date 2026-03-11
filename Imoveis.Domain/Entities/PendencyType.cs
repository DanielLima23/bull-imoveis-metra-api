using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PendencyType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Acronym { get; set; } = string.Empty;
    public string Category { get; set; } = "GENERAL";
    public string? Description { get; set; }
    public int DefaultSlaDays { get; set; } = 7;

    public ICollection<PendencyItem> Pendencies { get; set; } = new List<PendencyItem>();
}
