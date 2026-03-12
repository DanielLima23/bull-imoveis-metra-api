using Imoveis.Domain.Common;
using Imoveis.Domain.Enums;

namespace Imoveis.Domain.Entities;

public sealed class ExpenseInstallment : BaseEntity
{
    public Guid PropertyExpenseId { get; set; }
    public PropertyExpense PropertyExpense { get; set; } = null!;

    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public ExpenseStatus Status { get; set; } = ExpenseStatus.OPEN;
    public DateTime? PaidAtUtc { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PaidBy { get; set; }
    public string? Notes { get; set; }
}
