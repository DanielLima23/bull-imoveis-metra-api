using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class PropertyExpense : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid ExpenseTypeId { get; set; }
    public ExpenseType ExpenseType { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public ExpenseFrequency Frequency { get; set; } = ExpenseFrequency.ONE_TIME;
    public DateOnly DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentsCount { get; set; } = 1;
    public ExpenseStatus Status { get; set; } = ExpenseStatus.OPEN;
    public bool IsRecurring { get; set; }
    public int? YearlyMonth { get; set; }
    public string? Notes { get; set; }

    public ICollection<ExpenseInstallment> Installments { get; set; } = new List<ExpenseInstallment>();
}
