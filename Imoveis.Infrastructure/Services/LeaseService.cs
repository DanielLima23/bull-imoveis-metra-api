using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Leases;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class LeaseService : ILeaseService
{
    private readonly AppDbContext _dbContext;

    public LeaseService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<LeaseDto>> QueryAsync(LeaseQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.PropertyId == request.PropertyId.Value);
        }

        if (request.TenantId.HasValue)
        {
            query = query.Where(x => x.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<LeaseStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x, x.Property.Title, x.Tenant.Name))
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaseDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<LeaseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity, entity.Property.Title, entity.Tenant.Name);
    }

    public async Task<LeaseDto> CreateAsync(LeaseCreateRequest request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == request.PropertyId, cancellationToken)
            ?? throw new AppException("Property not found.", 404, "not_found");

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == request.TenantId, cancellationToken)
            ?? throw new AppException("Tenant not found.", 404, "not_found");

        var entity = new LeaseContract
        {
            PropertyId = request.PropertyId,
            TenantId = request.TenantId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MonthlyRent = request.MonthlyRent,
            DepositAmount = request.DepositAmount,
            ContractWith = NormalizeNullable(request.ContractWith),
            PaymentDay = request.PaymentDay,
            PaymentLocation = NormalizeNullable(request.PaymentLocation),
            ReadjustmentIndex = NormalizeNullable(request.ReadjustmentIndex),
            ContractRegistration = NormalizeNullable(request.ContractRegistration),
            Insurance = NormalizeNullable(request.Insurance),
            SignatureRecognition = NormalizeNullable(request.SignatureRecognition),
            OptionalContactName = NormalizeNullable(request.OptionalContactName),
            OptionalContactPhone = NormalizeNullable(request.OptionalContactPhone),
            GuarantorName = NormalizeNullable(request.GuarantorName),
            GuarantorDocument = NormalizeNullable(request.GuarantorDocument),
            GuarantorPhone = NormalizeNullable(request.GuarantorPhone),
            Notes = NormalizeNullable(request.Notes),
            Status = LeaseStatus.ACTIVE
        };

        _dbContext.LeaseContracts.Add(entity);

        property.OccupancyStatus = PropertyOccupancyStatus.OCCUPIED;
        if (property.AssetState == PropertyAssetState.PREPARATION)
        {
            property.AssetState = PropertyAssetState.READY;
        }

        GenerateReceivables(entity, DateOnly.FromDateTime(DateTime.UtcNow.Date));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, property.Title, tenant.Name);
    }

    public async Task<LeaseDto?> UpdateAsync(Guid id, LeaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LeaseContracts
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Include(x => x.ReceivableInstallments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.MonthlyRent = request.MonthlyRent;
        entity.DepositAmount = request.DepositAmount;
        entity.Status = ServiceHelpers.ParseEnum<LeaseStatus>(request.Status, "status");
        entity.ContractWith = NormalizeNullable(request.ContractWith);
        entity.PaymentDay = request.PaymentDay;
        entity.PaymentLocation = NormalizeNullable(request.PaymentLocation);
        entity.ReadjustmentIndex = NormalizeNullable(request.ReadjustmentIndex);
        entity.ContractRegistration = NormalizeNullable(request.ContractRegistration);
        entity.Insurance = NormalizeNullable(request.Insurance);
        entity.SignatureRecognition = NormalizeNullable(request.SignatureRecognition);
        entity.OptionalContactName = NormalizeNullable(request.OptionalContactName);
        entity.OptionalContactPhone = NormalizeNullable(request.OptionalContactPhone);
        entity.GuarantorName = NormalizeNullable(request.GuarantorName);
        entity.GuarantorDocument = NormalizeNullable(request.GuarantorDocument);
        entity.GuarantorPhone = NormalizeNullable(request.GuarantorPhone);
        entity.Notes = NormalizeNullable(request.Notes);

        if (entity.Status is LeaseStatus.ENDED or LeaseStatus.CANCELED)
        {
            entity.Property.OccupancyStatus = PropertyOccupancyStatus.VACANT;
        }
        else
        {
            entity.Property.OccupancyStatus = PropertyOccupancyStatus.OCCUPIED;
        }

        RebuildReceivables(entity, DateOnly.FromDateTime(DateTime.UtcNow.Date));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title, entity.Tenant.Name);
    }

    public async Task<LeaseDto?> CloseAsync(Guid id, LeaseCloseRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LeaseContracts
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Include(x => x.ReceivableInstallments)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.EndDate = request.EndDate;
        entity.Status = LeaseStatus.ENDED;
        entity.Property.OccupancyStatus = PropertyOccupancyStatus.VACANT;

        RebuildReceivables(entity, DateOnly.FromDateTime(DateTime.UtcNow.Date));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title, entity.Tenant.Name);
    }

    public async Task<IReadOnlyList<LeaseDto>> HistoryByPropertyAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => ToDto(x, x.Property.Title, x.Tenant.Name))
            .ToListAsync(cancellationToken);
    }

    private void RebuildReceivables(LeaseContract entity, DateOnly today)
    {
        var paidByCompetence = entity.ReceivableInstallments
            .Where(x => x.Status == ReceivableStatus.RECEIVED)
            .ToDictionary(x => x.CompetenceDate, x => x);

        var canceledOrOpen = entity.ReceivableInstallments
            .Where(x => x.Status != ReceivableStatus.RECEIVED)
            .ToList();

        _dbContext.LeaseReceivableInstallments.RemoveRange(canceledOrOpen);
        entity.ReceivableInstallments = paidByCompetence.Values.ToList();

        GenerateReceivables(entity, today, paidByCompetence.Keys.ToHashSet());
    }

    private void GenerateReceivables(LeaseContract entity, DateOnly today, HashSet<DateOnly>? skipCompetences = null)
    {
        var scheduleEnd = ResolveScheduleEnd(entity);
        var current = new DateOnly(entity.StartDate.Year, entity.StartDate.Month, 1);
        var endCompetence = new DateOnly(scheduleEnd.Year, scheduleEnd.Month, 1);

        while (current <= endCompetence)
        {
            if (skipCompetences is not null && skipCompetences.Contains(current))
            {
                current = current.AddMonths(1);
                continue;
            }

            var dueDay = Math.Clamp(entity.PaymentDay ?? 5, 1, DateTime.DaysInMonth(current.Year, current.Month));
            var dueDate = new DateOnly(current.Year, current.Month, dueDay);

            entity.ReceivableInstallments.Add(new LeaseReceivableInstallment
            {
                CompetenceDate = current,
                DueDate = dueDate,
                ExpectedAmount = entity.MonthlyRent,
                Status = ResolveReceivableStatus(entity.Status, dueDate, today)
            });

            current = current.AddMonths(1);
        }

        if (entity.Status is LeaseStatus.ENDED or LeaseStatus.CANCELED)
        {
            foreach (var installment in entity.ReceivableInstallments.Where(x => x.Status != ReceivableStatus.RECEIVED && x.CompetenceDate > endCompetence))
            {
                installment.Status = ReceivableStatus.CANCELED;
            }
        }
    }

    private static DateOnly ResolveScheduleEnd(LeaseContract entity)
    {
        if (entity.EndDate.HasValue)
        {
            return entity.EndDate.Value;
        }

        return entity.StartDate.AddMonths(11);
    }

    private static ReceivableStatus ResolveReceivableStatus(LeaseStatus leaseStatus, DateOnly dueDate, DateOnly today)
    {
        if (leaseStatus == LeaseStatus.CANCELED)
        {
            return ReceivableStatus.CANCELED;
        }

        return dueDate < today ? ReceivableStatus.OVERDUE : ReceivableStatus.OPEN;
    }

    private static LeaseDto ToDto(LeaseContract entity, string propertyTitle, string tenantName)
        => new(
            entity.Id,
            entity.PropertyId,
            entity.TenantId,
            propertyTitle,
            tenantName,
            entity.StartDate,
            entity.EndDate,
            entity.MonthlyRent,
            entity.DepositAmount,
            entity.Status.ToString(),
            entity.ContractWith,
            entity.PaymentDay,
            entity.PaymentLocation,
            entity.ReadjustmentIndex,
            entity.ContractRegistration,
            entity.Insurance,
            entity.SignatureRecognition,
            entity.OptionalContactName,
            entity.OptionalContactPhone,
            entity.GuarantorName,
            entity.GuarantorDocument,
            entity.GuarantorPhone,
            entity.Notes,
            entity.CreatedAtUtc);

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
