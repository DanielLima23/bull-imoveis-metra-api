using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class PropertyChargeTemplate : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public ChargeTemplateKind Kind { get; set; } = ChargeTemplateKind.EXTRA;
    public string Title { get; set; } = string.Empty;
    public decimal DefaultAmount { get; set; }
    public int? DueDay { get; set; }
    public ChargeResponsibility DefaultResponsibility { get; set; } = ChargeResponsibility.OWNER;
    public string? ProviderInformation { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
