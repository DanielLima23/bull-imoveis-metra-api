using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<LeaseContract> Leases { get; set; } = new List<LeaseContract>();
}
