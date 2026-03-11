using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class PropertyPartyLink : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid PartyId { get; set; }
    public Party Party { get; set; } = null!;

    public PropertyPartyRole Role { get; set; } = PropertyPartyRole.OTHER;
    public bool IsPrimary { get; set; }
    public DateTime? StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public string? Notes { get; set; }
}
