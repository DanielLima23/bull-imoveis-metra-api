using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class Party : BaseEntity
{
    public PartyKind Kind { get; set; } = PartyKind.PERSON;
    public string Name { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<PropertyPartyLink> PropertyLinks { get; set; } = new List<PropertyPartyLink>();
}
