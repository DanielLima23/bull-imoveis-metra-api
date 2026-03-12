namespace Imoveis.Application.Contracts.Dashboard;

public sealed record DashboardOverviewDto(
    string Competence,
    decimal ReceivedRentAmount,
    decimal ExpectedRentAmount,
    decimal PaidExpensesAmount,
    decimal PendingExpensesAmount,
    decimal OverdueReceivablesAmount,
    decimal OverdueExpensesAmount,
    int VacantPropertiesCount,
    decimal VacancyLossAmount,
    int TotalProperties,
    int OccupiedProperties,
    int VacantProperties,
    int PropertiesInPreparation,
    int PropertiesInRenovation);

public sealed record DashboardOverdueExpenseDto(
    Guid ExpenseId,
    string PropertyTitle,
    string ExpenseTypeName,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    int OverdueDays);

public sealed record DashboardOverdueReceivableDto(
    Guid LeaseId,
    string PropertyTitle,
    string TenantName,
    DateOnly CompetenceDate,
    DateOnly DueDate,
    decimal ExpectedAmount,
    int OverdueDays);

public sealed record DashboardPendencyAlertDto(
    Guid PendencyId,
    string PropertyTitle,
    string PendencyTypeCode,
    string PendencyTypeName,
    string Title,
    DateTime DueAtUtc,
    string Severity,
    int ElapsedDays,
    int SlaDays);

public sealed record RealEstateDashboardDto(
    DashboardOverviewDto Overview,
    IReadOnlyList<DashboardOverdueExpenseDto> OverdueExpenses,
    IReadOnlyList<DashboardOverdueReceivableDto> OverdueReceivables,
    IReadOnlyList<DashboardPendencyAlertDto> PendencyAlerts);
