using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class PropertyRentReference : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
}
