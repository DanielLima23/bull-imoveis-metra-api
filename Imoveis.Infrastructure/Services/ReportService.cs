using System.Text;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Reports;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class ReportService : IReportService
{
    private readonly AppDbContext _dbContext;

    public ReportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportCsvDto> BuildFinancialCsvAsync(int month, int year, CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var rows = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.Property)
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .Where(x => x.DueDate >= monthStart && x.DueDate <= monthEnd)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,expense_type,description,installment,due_date,amount,status,paid_at,paid_amount");

        foreach (var row in rows)
        {
            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(row.PropertyExpense.Property.Title),
                ServiceHelpers.EscapeCsv(row.PropertyExpense.ExpenseType.Name),
                ServiceHelpers.EscapeCsv(row.PropertyExpense.Description),
                row.InstallmentNumber,
                row.DueDate,
                row.Amount,
                row.Status,
                row.PaidAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? string.Empty,
                row.PaidAmount?.ToString("0.00") ?? string.Empty));
        }

        return new ReportCsvDto($"financial-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<ReportCsvDto> BuildVacancyCsvAsync(int month, int year, CancellationToken cancellationToken)
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

        foreach (var property in properties)
        {
            if (property.Status == PropertyStatus.LEASED)
            {
                continue;
            }

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

        return new ReportCsvDto($"vacancy-{year:D4}-{month:D2}.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    public async Task<ReportCsvDto> BuildPendenciesCsvAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .Where(x => x.Status == PendencyStatus.OPEN)
            .OrderBy(x => x.DueAtUtc)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("property,pendency_type,title,due_at,severity,elapsed_days,sla_days,status");

        foreach (var row in rows)
        {
            var elapsedDays = Math.Max(0, (int)(DateTime.UtcNow.Date - row.OpenedAtUtc.Date).TotalDays);
            var slaDays = Math.Max(1, row.PendencyType.DefaultSlaDays);
            var pct = elapsedDays / (decimal)slaDays;
            var severity = pct < 0.8m ? "ATTENTION" : pct <= 1m ? "URGENT" : "CRITICAL";

            csv.AppendLine(string.Join(",",
                ServiceHelpers.EscapeCsv(row.Property.Title),
                ServiceHelpers.EscapeCsv(row.PendencyType.Name),
                ServiceHelpers.EscapeCsv(row.Title),
                row.DueAtUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                severity,
                elapsedDays,
                slaDays,
                row.Status));
        }

        return new ReportCsvDto("pendencies-open.csv", "text/csv", Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private static int OccupiedDaysInMonth(IEnumerable<Domain.Entities.LeaseContract> leases, DateOnly monthStart, DateOnly monthEnd)
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

    private static decimal? ResolveBaseRent(Domain.Entities.Property property, DateOnly monthEnd)
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
}
