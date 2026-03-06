using Imoveis.Domain.Common;

namespace Imoveis.Domain.Entities;

public sealed class ExpenseType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsFixedCost { get; set; }

    public ICollection<PropertyExpense> Expenses { get; set; } = new List<PropertyExpense>();
}
