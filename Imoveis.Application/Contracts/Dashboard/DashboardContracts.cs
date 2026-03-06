namespace Imoveis.Application.Contracts.Dashboard;

public sealed record DashboardOverviewDto(
    string Competence,
    decimal ReceivedAmount,
    decimal ExpectedAmount,
    decimal PendingAmount,
    decimal OverdueAmount,
    int VacantPropertiesCount,
    decimal VacancyLossAmount,
    int TotalProperties,
    int LeasedProperties,
    int AvailableProperties,
    int PreparationProperties);

public sealed record DashboardOverdueExpenseDto(
    Guid ExpenseId,
    string PropertyTitle,
    string ExpenseTypeName,
    string Description,
    DateOnly DueDate,
    decimal Amount,
    int OverdueDays);

public sealed record DashboardPendencyAlertDto(
    Guid PendencyId,
    string PropertyTitle,
    string PendencyTypeName,
    string Title,
    DateTime DueAtUtc,
    string Severity,
    int ElapsedDays,
    int SlaDays);

public sealed record RealEstateDashboardDto(
    DashboardOverviewDto Overview,
    IReadOnlyList<DashboardOverdueExpenseDto> OverdueExpenses,
    IReadOnlyList<DashboardPendencyAlertDto> PendencyAlerts);
