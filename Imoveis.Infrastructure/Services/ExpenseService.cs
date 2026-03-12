using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Expenses;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class ExpenseService : IExpenseService
{
    private readonly AppDbContext _dbContext;

    public ExpenseService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ExpenseTypeDto>> ListTypesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.ExpenseTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new ExpenseTypeDto(x.Id, x.Name, x.Category, x.IsFixedCost))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExpenseTypeDto> CreateTypeAsync(ExpenseTypeCreateRequest request, CancellationToken cancellationToken)
    {
        if (await _dbContext.ExpenseTypes.AnyAsync(x => x.Name == request.Name, cancellationToken))
        {
            throw new AppException("Expense type already exists.", 409, "conflict_error");
        }

        var entity = new ExpenseType
        {
            Name = request.Name.Trim(),
            Category = request.Category.Trim(),
            IsFixedCost = request.IsFixedCost
        };

        _dbContext.ExpenseTypes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ExpenseTypeDto(entity.Id, entity.Name, entity.Category, entity.IsFixedCost);
    }

    public async Task<ExpenseTypeDto?> UpdateTypeAsync(Guid id, ExpenseTypeUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (await _dbContext.ExpenseTypes.AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken))
        {
            throw new AppException("Expense type already exists.", 409, "conflict_error");
        }

        entity.Name = request.Name.Trim();
        entity.Category = request.Category.Trim();
        entity.IsFixedCost = request.IsFixedCost;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ExpenseTypeDto(entity.Id, entity.Name, entity.Category, entity.IsFixedCost);
    }

    public async Task<PagedResult<ExpenseDto>> QueryAsync(ExpenseQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.PropertyExpenses
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.ExpenseType)
            .Include(x => x.Installments)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.PropertyId == request.PropertyId.Value);
        }

        if (request.ExpenseTypeId.HasValue)
        {
            query = query.Where(x => x.ExpenseTypeId == request.ExpenseTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<ExpenseStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(x => x.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToDto).ToList();

        return new PagedResult<ExpenseDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyExpenses
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.ExpenseType)
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<ExpenseDto> CreateAsync(ExpenseCreateRequest request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == request.PropertyId, cancellationToken)
            ?? throw new AppException("Property not found.", 404, "not_found");

        var expenseType = await _dbContext.ExpenseTypes.FirstOrDefaultAsync(x => x.Id == request.ExpenseTypeId, cancellationToken)
            ?? throw new AppException("Expense type not found.", 404, "not_found");

        var frequency = ServiceHelpers.ParseEnum<ExpenseFrequency>(request.Frequency, "frequency");
        var installmentsCount = request.InstallmentsCount <= 0 ? 1 : request.InstallmentsCount;

        var entity = new PropertyExpense
        {
            PropertyId = property.Id,
            ExpenseTypeId = expenseType.Id,
            Description = request.Description.Trim(),
            Frequency = frequency,
            DueDate = request.DueDate,
            TotalAmount = request.TotalAmount,
            InstallmentsCount = installmentsCount,
            IsRecurring = request.IsRecurring,
            YearlyMonth = request.YearlyMonth,
            Notes = NormalizeNullable(request.Notes),
            Status = ExpenseStatus.OPEN
        };

        _dbContext.PropertyExpenses.Add(entity);
        entity.Installments = GenerateInstallments(entity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        entity.Property = property;
        entity.ExpenseType = expenseType;

        return ToDto(entity);
    }

    public async Task<ExpenseDto?> UpdateAsync(Guid id, ExpenseUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyExpenses
            .Include(x => x.Property)
            .Include(x => x.ExpenseType)
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var frequency = ServiceHelpers.ParseEnum<ExpenseFrequency>(request.Frequency, "frequency");
        var status = ServiceHelpers.ParseEnum<ExpenseStatus>(request.Status, "status");
        var installmentsCount = request.InstallmentsCount <= 0 ? 1 : request.InstallmentsCount;

        var shouldRebuildInstallments =
            entity.InstallmentsCount != installmentsCount
            || entity.TotalAmount != request.TotalAmount
            || entity.DueDate != request.DueDate;

        entity.Description = request.Description.Trim();
        entity.Frequency = frequency;
        entity.DueDate = request.DueDate;
        entity.TotalAmount = request.TotalAmount;
        entity.InstallmentsCount = installmentsCount;
        entity.IsRecurring = request.IsRecurring;
        entity.YearlyMonth = request.YearlyMonth;
        entity.Status = status;
        entity.Notes = NormalizeNullable(request.Notes);

        if (shouldRebuildInstallments)
        {
            _dbContext.ExpenseInstallments.RemoveRange(entity.Installments.Where(x => x.Status != ExpenseStatus.PAID));
            entity.Installments = entity.Installments.Where(x => x.Status == ExpenseStatus.PAID).ToList();

            foreach (var installment in GenerateInstallments(entity))
            {
                if (entity.Installments.Any(x => x.InstallmentNumber == installment.InstallmentNumber))
                {
                    continue;
                }

                entity.Installments.Add(installment);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<ExpenseDto?> MarkPaidAsync(Guid id, ExpenseMarkPaidRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyExpenses
            .Include(x => x.Property)
            .Include(x => x.ExpenseType)
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var installment = entity.Installments
            .Where(x => x.Status is ExpenseStatus.OPEN or ExpenseStatus.OVERDUE)
            .OrderBy(x => x.InstallmentNumber)
            .FirstOrDefault();

        if (installment is null)
        {
            throw new AppException("No open installment available.", 400, "business_error");
        }

        installment.Status = ExpenseStatus.PAID;
        installment.PaidAtUtc = request.PaidAtUtc ?? DateTime.UtcNow;
        installment.PaidAmount = request.PaidAmount ?? installment.Amount;
        installment.PaidBy = NormalizeNullable(request.PaidBy);
        installment.Notes = NormalizeNullable(request.Notes);

        if (entity.Installments.All(x => x.Status == ExpenseStatus.PAID))
        {
            entity.Status = ExpenseStatus.PAID;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ExpenseDto>> GetOverdueAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var entities = await _dbContext.PropertyExpenses
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.ExpenseType)
            .Include(x => x.Installments)
            .Where(x => x.Installments.Any(i => i.DueDate < today && i.Status != ExpenseStatus.PAID))
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.TotalAmount)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToList();
    }

    private static List<ExpenseInstallment> GenerateInstallments(PropertyExpense expense)
    {
        var installments = new List<ExpenseInstallment>();
        var installmentCount = expense.InstallmentsCount <= 0 ? 1 : expense.InstallmentsCount;
        var baseAmount = Math.Round(expense.TotalAmount / installmentCount, 2, MidpointRounding.AwayFromZero);
        var totalAccumulated = 0m;
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        for (var i = 1; i <= installmentCount; i++)
        {
            var amount = i == installmentCount
                ? Math.Round(expense.TotalAmount - totalAccumulated, 2, MidpointRounding.AwayFromZero)
                : baseAmount;

            totalAccumulated += amount;
            var dueDate = expense.DueDate.AddMonths(i - 1);

            installments.Add(new ExpenseInstallment
            {
                InstallmentNumber = i,
                DueDate = dueDate,
                Amount = amount,
                Status = dueDate < today ? ExpenseStatus.OVERDUE : ExpenseStatus.OPEN
            });
        }

        return installments;
    }

    private static ExpenseDto ToDto(PropertyExpense entity)
    {
        var installments = entity.Installments
            .OrderBy(x => x.InstallmentNumber)
            .Select(x => new ExpenseInstallmentDto(
                x.Id,
                x.InstallmentNumber,
                x.DueDate,
                x.Amount,
                x.Status.ToString(),
                x.PaidAtUtc,
                x.PaidAmount,
                x.PaidBy,
                x.Notes))
            .ToList();

        return new ExpenseDto(
            entity.Id,
            entity.PropertyId,
            entity.ExpenseTypeId,
            entity.Property.Title,
            entity.ExpenseType.Name,
            entity.Description,
            entity.Frequency.ToString(),
            entity.DueDate,
            entity.TotalAmount,
            entity.InstallmentsCount,
            entity.IsRecurring,
            entity.YearlyMonth,
            entity.Status.ToString(),
            entity.Notes,
            installments,
            entity.CreatedAtUtc);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
