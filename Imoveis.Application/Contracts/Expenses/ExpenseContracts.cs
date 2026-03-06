namespace Imoveis.Application.Contracts.Expenses;

public sealed record ExpenseTypeCreateRequest(string Name, string Category, bool IsFixedCost);

public sealed record ExpenseTypeUpdateRequest(string Name, string Category, bool IsFixedCost);

public sealed record ExpenseTypeDto(Guid Id, string Name, string Category, bool IsFixedCost);

public sealed record ExpenseQueryRequest(Guid? PropertyId, Guid? ExpenseTypeId, string? Status, int Page = 1, int PageSize = 20);

public sealed record ExpenseCreateRequest(
    Guid PropertyId,
    Guid ExpenseTypeId,
    string Description,
    string Frequency,
    DateOnly DueDate,
    decimal TotalAmount,
    int InstallmentsCount,
    bool IsRecurring,
    int? YearlyMonth,
    string? Notes);

public sealed record ExpenseUpdateRequest(
    string Description,
    string Frequency,
    DateOnly DueDate,
    decimal TotalAmount,
    int InstallmentsCount,
    bool IsRecurring,
    int? YearlyMonth,
    string Status,
    string? Notes);

public sealed record ExpenseMarkPaidRequest(decimal? PaidAmount, DateTime? PaidAtUtc);

public sealed record ExpenseInstallmentDto(
    Guid Id,
    int InstallmentNumber,
    DateOnly DueDate,
    decimal Amount,
    string Status,
    DateTime? PaidAtUtc,
    decimal? PaidAmount);

public sealed record ExpenseDto(
    Guid Id,
    Guid PropertyId,
    Guid ExpenseTypeId,
    string PropertyTitle,
    string ExpenseTypeName,
    string Description,
    string Frequency,
    DateOnly DueDate,
    decimal TotalAmount,
    int InstallmentsCount,
    bool IsRecurring,
    int? YearlyMonth,
    string Status,
    string? Notes,
    IReadOnlyList<ExpenseInstallmentDto> Installments,
    DateTime CreatedAtUtc);
