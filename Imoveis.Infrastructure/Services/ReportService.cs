using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Reports;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Imoveis.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private static readonly IReadOnlyList<ReportCatalogItemDto> Catalog = new[]
    {
        new ReportCatalogItemDto("pagamentos-feitos-mes", "Pagamentos feitos no mes", "Recebimentos de aluguel e pagamentos de contas liquidados no periodo.", true, true),
        new ReportCatalogItemDto("pagamentos-em-atraso-mes", "Pagamentos em atraso no mes", "Recebiveis e despesas vencidas e ainda nao liquidadas.", true, true),
        new ReportCatalogItemDto("contratos-a-vencer", "Contratos a vencer", "Locacoes com encerramento previsto no periodo informado.", true, true),
        new ReportCatalogItemDto("pagamentos-aluguel-imoveis", "Pagamentos de aluguel por imovel", "Recebiveis de aluguel por imovel e locatario.", true, true),
        new ReportCatalogItemDto("imoveis-vagos-mes", "Imoveis vagos no mes", "Vacancia e perda estimada por imovel.", true, true),
        new ReportCatalogItemDto("pendencias-imoveis", "Pendencias por imovel", "Pendencias abertas agrupadas por imovel.", false, false),
        new ReportCatalogItemDto("ganhos-e-perdas-mes", "Ganhos e perdas do mes", "Resumo por imovel de recebimentos, despesas e saldo.", true, true)
    };

    private readonly AppDbContext _dbContext;

    public ReportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<ReportCatalogItemDto>> ListCatalogAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Catalog);
    }

    public Task<ReportCsvDto> BuildBySlugAsync(string slug, int? month, int? year, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var normalizedMonth = month ?? now.Month;
        var normalizedYear = year ?? now.Year;

        return slug.Trim().ToLowerInvariant() switch
        {
            "pagamentos-feitos-mes" => BuildPaymentsMadeCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            "pagamentos-em-atraso-mes" => BuildPaymentsOverdueCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            "contratos-a-vencer" => BuildContractsExpiringCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            "pagamentos-aluguel-imoveis" => BuildRentPaymentsCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            "imoveis-vagos-mes" => BuildVacancyCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            "pendencias-imoveis" => BuildPendenciesCsvAsync(cancellationToken),
            "ganhos-e-perdas-mes" => BuildFinancialCsvAsync(normalizedMonth, normalizedYear, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported report slug '{slug}'.")
        };
    }

    public Task<ReportCsvDto> BuildFinancialCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        return BuildProfitAndLossCsvAsync(month, year, cancellationToken);
    }

    public Task<ReportCsvDto> BuildVacancyCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        return BuildVacantPropertiesCsvAsync(month, year, cancellationToken);
    }

    public async Task<ReportCsvDto> BuildPendenciesCsvAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .Where(x => x.Status == PendencyStatus.OPEN)
            .OrderBy(x => x.Property.Title)
            .ThenBy(x => x.DueAtUtc)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,pendency_code,pendency_name,title,due_at,severity,status");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(row.Property.Title),
                ServiceHelpers.EscapeCsv(row.PendencyType.Code),
                ServiceHelpers.EscapeCsv(row.PendencyType.Name),
                ServiceHelpers.EscapeCsv(row.Title),
                row.DueAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                ResolvePendencySeverity(row.OpenedAtUtc, row.PendencyType.DefaultSlaDays),
                row.Status));
        }

        return new ReportCsvDto("pendencias-imoveis.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<ReportCsvDto> BuildPaymentsMadeCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var (monthStart, monthEnd, monthStartUtc, monthEndUtc) = ResolveMonthRange(month, year);

        var receivables = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Property)
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Tenant)
            .Where(x => x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc)
            .OrderBy(x => x.PaidAtUtc)
            .ToListAsync(cancellationToken);

        var expenses = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.Property)
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .Where(x => x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc)
            .OrderBy(x => x.PaidAtUtc)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("direction,property,party,label,competence,due_date,paid_at,amount,paid_by,notes");

        foreach (var row in receivables)
        {
            csv.AppendLine(string.Join(",",
                "RECEIVABLE",
                ServiceHelpers.EscapeCsv(row.LeaseContract.Property.Title),
                ServiceHelpers.EscapeCsv(row.LeaseContract.Tenant.Name),
                "Aluguel",
                row.CompetenceDate,
                row.DueDate,
                row.PaidAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                (row.PaidAmount ?? row.ExpectedAmount).ToString("0.00"),
                ServiceHelpers.EscapeCsv(row.PaidBy ?? string.Empty),
                ServiceHelpers.EscapeCsv(row.Notes ?? string.Empty)));
        }

        foreach (var row in expenses)
        {
            csv.AppendLine(string.Join(",",
                "EXPENSE",
                ServiceHelpers.EscapeCsv(row.PropertyExpense.Property.Title),
                string.Empty,
                ServiceHelpers.EscapeCsv(row.PropertyExpense.ExpenseType.Name),
                new DateOnly(row.DueDate.Year, row.DueDate.Month, 1),
                row.DueDate,
                row.PaidAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                (row.PaidAmount ?? row.Amount).ToString("0.00"),
                ServiceHelpers.EscapeCsv(row.PaidBy ?? string.Empty),
                ServiceHelpers.EscapeCsv(row.Notes ?? row.PropertyExpense.Notes ?? string.Empty)));
        }

        return new ReportCsvDto($"pagamentos-feitos-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<ReportCsvDto> BuildPaymentsOverdueCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var (_, monthEnd, _, _) = ResolveMonthRange(month, year);

        var receivables = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Property)
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Tenant)
            .Where(x => x.DueDate <= monthEnd && x.Status != ReceivableStatus.RECEIVED && x.Status != ReceivableStatus.CANCELED)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        var expenses = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.Property)
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .Where(x => x.DueDate <= monthEnd && x.Status != ExpenseStatus.PAID && x.Status != ExpenseStatus.CANCELED)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("direction,property,party,label,competence,due_date,expected_amount,status,days_overdue");

        foreach (var row in receivables)
        {
            csv.AppendLine(string.Join(",",
                "RECEIVABLE",
                ServiceHelpers.EscapeCsv(row.LeaseContract.Property.Title),
                ServiceHelpers.EscapeCsv(row.LeaseContract.Tenant.Name),
                "Aluguel",
                row.CompetenceDate,
                row.DueDate,
                row.ExpectedAmount.ToString("0.00"),
                row.Status,
                Math.Max(0, (DateOnly.FromDateTime(DateTime.UtcNow.Date).ToDateTime(TimeOnly.MinValue) - row.DueDate.ToDateTime(TimeOnly.MinValue)).Days)));
        }

        foreach (var row in expenses)
        {
            csv.AppendLine(string.Join(",",
                "EXPENSE",
                ServiceHelpers.EscapeCsv(row.PropertyExpense.Property.Title),
                string.Empty,
                ServiceHelpers.EscapeCsv(row.PropertyExpense.ExpenseType.Name),
                new DateOnly(row.DueDate.Year, row.DueDate.Month, 1),
                row.DueDate,
                row.Amount.ToString("0.00"),
                row.Status,
                Math.Max(0, (DateOnly.FromDateTime(DateTime.UtcNow.Date).ToDateTime(TimeOnly.MinValue) - row.DueDate.ToDateTime(TimeOnly.MinValue)).Days)));
        }

        return new ReportCsvDto($"pagamentos-atraso-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<ReportCsvDto> BuildContractsExpiringCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var (monthStart, monthEnd, _, _) = ResolveMonthRange(month, year);

        var rows = await _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Where(x => x.EndDate.HasValue && x.EndDate.Value >= monthStart && x.EndDate.Value <= monthEnd)
            .OrderBy(x => x.EndDate)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,tenant,start_date,end_date,monthly_rent,payment_day,readjustment_index,status");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(row.Property.Title),
                ServiceHelpers.EscapeCsv(row.Tenant.Name),
                row.StartDate,
                row.EndDate,
                row.MonthlyRent.ToString("0.00"),
                row.PaymentDay?.ToString() ?? string.Empty,
                ServiceHelpers.EscapeCsv(row.ReadjustmentIndex ?? string.Empty),
                row.Status));
        }

        return new ReportCsvDto($"contratos-a-vencer-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<ReportCsvDto> BuildRentPaymentsCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var (monthStart, monthEnd, _, _) = ResolveMonthRange(month, year);

        var rows = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Property)
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Tenant)
            .Where(x => x.CompetenceDate >= monthStart && x.CompetenceDate <= monthEnd)
            .OrderBy(x => x.LeaseContract.Property.Title)
            .ThenBy(x => x.CompetenceDate)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,tenant,competence,due_date,expected_amount,paid_amount,status,paid_at,paid_by");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(row.LeaseContract.Property.Title),
                ServiceHelpers.EscapeCsv(row.LeaseContract.Tenant.Name),
                row.CompetenceDate,
                row.DueDate,
                row.ExpectedAmount.ToString("0.00"),
                row.PaidAmount?.ToString("0.00") ?? string.Empty,
                row.Status,
                row.PaidAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                ServiceHelpers.EscapeCsv(row.PaidBy ?? string.Empty)));
        }

        return new ReportCsvDto($"pagamentos-aluguel-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private Task<ReportCsvDto> BuildVacantPropertiesCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        return BuildVacancyCoreCsvAsync(month, year, cancellationToken);
    }

    private async Task<ReportCsvDto> BuildProfitAndLossCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var (monthStart, monthEnd, monthStartUtc, monthEndUtc) = ResolveMonthRange(month, year);

        var properties = await _dbContext.Properties
            .AsNoTracking()
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var receivables = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
            .Where(x => x.CompetenceDate >= monthStart && x.CompetenceDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var expenses = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
            .Where(x => x.DueDate >= monthStart && x.DueDate <= monthEnd)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,expected_rent,received_rent,expected_expenses,paid_expenses,balance");

        foreach (var property in properties)
        {
            var expectedRent = receivables
                .Where(x => x.LeaseContract.PropertyId == property.Id)
                .Sum(x => x.ExpectedAmount);

            var receivedRent = receivables
                .Where(x => x.LeaseContract.PropertyId == property.Id && x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc)
                .Sum(x => x.PaidAmount ?? x.ExpectedAmount);

            var expectedExpenses = expenses
                .Where(x => x.PropertyExpense.PropertyId == property.Id)
                .Sum(x => x.Amount);

            var paidExpenses = expenses
                .Where(x => x.PropertyExpense.PropertyId == property.Id && x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc)
                .Sum(x => x.PaidAmount ?? x.Amount);

            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(property.Title),
                expectedRent.ToString("0.00"),
                receivedRent.ToString("0.00"),
                expectedExpenses.ToString("0.00"),
                paidExpenses.ToString("0.00"),
                (receivedRent - paidExpenses).ToString("0.00")));
        }

        return new ReportCsvDto($"ganhos-perdas-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<ReportCsvDto> BuildVacancyCoreCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var daysInMonth = monthEnd.Day;

        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Include(x => x.RentReferences)
            .Include(x => x.Leases)
            .OrderBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,base_rent,vacant_days,occupied_days,vacancy_loss");

        foreach (var property in properties.Where(x => x.OccupancyStatus == PropertyOccupancyStatus.VACANT))
        {
            var baseRent = ResolveBaseRent(property, monthEnd);

            if (!baseRent.HasValue)
            {
                continue;
            }

            var occupiedDays = OccupiedDaysInMonth(property.Leases, monthStart, monthEnd);
            var vacantDays = Math.Max(0, daysInMonth - occupiedDays);
            var loss = baseRent.Value * (vacantDays / (decimal)daysInMonth);

            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(property.Title),
                baseRent.Value.ToString("0.00"),
                vacantDays,
                occupiedDays,
                Math.Round(loss, 2).ToString("0.00")));
        }

        return new ReportCsvDto($"vacancia-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private static (DateOnly MonthStart, DateOnly MonthEnd, DateTime MonthStartUtc, DateTime MonthEndUtc) ResolveMonthRange(int month, int year)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        return (
            monthStart,
            monthEnd,
            monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            monthEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc));
    }

    private static int OccupiedDaysInMonth(IEnumerable<LeaseContract> leases, DateOnly monthStart, DateOnly monthEnd)
    {
        var occupied = new HashSet<DateOnly>();

        foreach (var lease in leases)
        {
            if (lease.Status == LeaseStatus.CANCELED)
            {
                continue;
            }

            if (lease.StartDate > monthEnd)
            {
                continue;
            }

            var leaseEnd = lease.EndDate ?? monthEnd;
            if (leaseEnd < monthStart)
            {
                continue;
            }

            var from = lease.StartDate < monthStart ? monthStart : lease.StartDate;
            var to = leaseEnd > monthEnd ? monthEnd : leaseEnd;

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                occupied.Add(day);
            }
        }

        return occupied.Count;
    }

    private static decimal? ResolveBaseRent(Property property, DateOnly monthEnd)
    {
        var averageRecentRent = property.Leases
            .Where(x => x.StartDate <= monthEnd && x.MonthlyRent > 0 && x.Status is LeaseStatus.ACTIVE or LeaseStatus.ENDED)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => x.MonthlyRent)
            .Take(3)
            .ToList();

        if (averageRecentRent.Count > 0)
        {
            return averageRecentRent.Average();
        }

        return property.RentReferences
            .Where(x => x.EffectiveFrom <= monthEnd)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefault();
    }

    private static string ResolvePendencySeverity(DateTime openedAtUtc, int defaultSlaDays)
    {
        var elapsedDays = Math.Max(0, (int)(DateTime.UtcNow.Date - openedAtUtc.Date).TotalDays);
        var slaDays = Math.Max(1, defaultSlaDays);
        var pct = elapsedDays / (decimal)slaDays;
        return pct < 0.8m ? "ATTENTION" : pct <= 1m ? "URGENT" : "CRITICAL";
    }
}
