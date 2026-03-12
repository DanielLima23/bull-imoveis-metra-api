using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Contracts.Dashboard;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RealEstateDashboardDto> GetAsync(int month, int year, CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month));
        }

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthStartUtc = monthStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var monthEndUtc = monthEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var expenseInstallments = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.Property)
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .ToListAsync(cancellationToken);

        var receivables = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Property)
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Tenant)
            .ToListAsync(cancellationToken);

        var properties = await _dbContext.Properties
            .AsNoTracking()
            .Include(x => x.RentReferences)
            .Include(x => x.Leases)
            .ToListAsync(cancellationToken);

        var openPendencies = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .Where(x => x.Status == PendencyStatus.OPEN)
            .OrderBy(x => x.DueAtUtc)
            .Take(15)
            .ToListAsync(cancellationToken);

        var expectedRents = receivables.Where(x => x.CompetenceDate >= monthStart && x.CompetenceDate <= monthEnd).ToList();
        var receivedRents = receivables.Where(x => x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc).ToList();
        var paidExpenses = expenseInstallments.Where(x => x.PaidAtUtc.HasValue && x.PaidAtUtc >= monthStartUtc && x.PaidAtUtc <= monthEndUtc).ToList();
        var monthExpenses = expenseInstallments.Where(x => x.DueDate >= monthStart && x.DueDate <= monthEnd).ToList();

        var overdueReceivables = receivables
            .Where(x => x.DueDate < today && x.Status != ReceivableStatus.RECEIVED && x.Status != ReceivableStatus.CANCELED)
            .OrderBy(x => x.DueDate)
            .Take(10)
            .ToList();

        var overdueExpenses = expenseInstallments
            .Where(x => x.DueDate < today && x.Status != ExpenseStatus.PAID && x.Status != ExpenseStatus.CANCELED)
            .OrderBy(x => x.DueDate)
            .Take(10)
            .ToList();

        var receivedRentAmount = receivedRents.Sum(x => x.PaidAmount ?? x.ExpectedAmount);
        var expectedRentAmount = expectedRents.Sum(x => x.ExpectedAmount);
        var paidExpensesAmount = paidExpenses.Sum(x => x.PaidAmount ?? x.Amount);
        var pendingExpensesAmount = monthExpenses.Where(x => x.Status != ExpenseStatus.PAID).Sum(x => x.Amount);
        var overdueReceivableAmount = overdueReceivables.Sum(x => x.ExpectedAmount);
        var overdueExpenseAmount = overdueExpenses.Sum(x => x.Amount);

        var vacancyLoss = 0m;
        var vacantCount = 0;
        var daysInMonth = monthEnd.Day;

        foreach (var property in properties.Where(x => x.OccupancyStatus == PropertyOccupancyStatus.VACANT))
        {
            var baseRent = ResolveBaseRent(property, monthEnd);
            if (!baseRent.HasValue)
            {
                continue;
            }

            var occupiedDays = OccupiedDaysInMonth(property.Leases, monthStart, monthEnd);
            var vacantDays = Math.Max(0, daysInMonth - occupiedDays);

            if (vacantDays > 0)
            {
                vacantCount++;
                vacancyLoss += baseRent.Value * (vacantDays / (decimal)daysInMonth);
            }
        }

        var overview = new DashboardOverviewDto(
            $"{year:D4}-{month:D2}",
            Math.Round(receivedRentAmount, 2),
            Math.Round(expectedRentAmount, 2),
            Math.Round(paidExpensesAmount, 2),
            Math.Round(pendingExpensesAmount, 2),
            Math.Round(overdueReceivableAmount, 2),
            Math.Round(overdueExpenseAmount, 2),
            vacantCount,
            Math.Round(vacancyLoss, 2),
            properties.Count,
            properties.Count(x => x.OccupancyStatus == PropertyOccupancyStatus.OCCUPIED),
            properties.Count(x => x.OccupancyStatus == PropertyOccupancyStatus.VACANT),
            properties.Count(x => x.AssetState == PropertyAssetState.PREPARATION),
            properties.Count(x => x.AssetState == PropertyAssetState.RENOVATION));

        var overdueExpenseDtos = overdueExpenses.Select(x => new DashboardOverdueExpenseDto(
            x.PropertyExpenseId,
            x.PropertyExpense.Property.Title,
            x.PropertyExpense.ExpenseType.Name,
            x.PropertyExpense.Description,
            x.DueDate,
            x.Amount,
            Math.Max(0, (today.ToDateTime(TimeOnly.MinValue) - x.DueDate.ToDateTime(TimeOnly.MinValue)).Days)
        )).ToList();

        var overdueReceivableDtos = overdueReceivables.Select(x => new DashboardOverdueReceivableDto(
            x.LeaseContractId,
            x.LeaseContract.Property.Title,
            x.LeaseContract.Tenant.Name,
            x.CompetenceDate,
            x.DueDate,
            x.ExpectedAmount,
            Math.Max(0, (today.ToDateTime(TimeOnly.MinValue) - x.DueDate.ToDateTime(TimeOnly.MinValue)).Days)
        )).ToList();

        var alertDtos = openPendencies.Select(ToPendencyAlert).ToList();

        return new RealEstateDashboardDto(overview, overdueExpenseDtos, overdueReceivableDtos, alertDtos);
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

    private static DashboardPendencyAlertDto ToPendencyAlert(PendencyItem entity)
    {
        var today = DateTime.UtcNow.Date;
        var elapsedDays = Math.Max(0, (int)(today - entity.OpenedAtUtc.Date).TotalDays);
        var slaDays = Math.Max(1, entity.PendencyType.DefaultSlaDays);
        var pct = elapsedDays / (decimal)slaDays;
        var severity = pct < 0.8m ? "ATTENTION" : pct <= 1m ? "URGENT" : "CRITICAL";

        return new DashboardPendencyAlertDto(
            entity.Id,
            entity.Property.Title,
            entity.PendencyType.Code,
            entity.PendencyType.Name,
            entity.Title,
            entity.DueAtUtc,
            severity,
            elapsedDays,
            slaDays);
    }
}
