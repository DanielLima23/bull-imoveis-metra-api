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
            AdjustmentIndex = request.AdjustmentIndex?.Trim(),
            PaymentDay = request.PaymentDay,
            PaymentLocation = request.PaymentLocation?.Trim(),
            GuaranteeType = request.GuaranteeType?.Trim(),
            GuaranteeDetails = request.GuaranteeDetails?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = LeaseStatus.ACTIVE
        };

        _dbContext.LeaseContracts.Add(entity);
        property.Status = PropertyStatus.LEASED;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, property.Title, tenant.Name);
    }

    public async Task<LeaseDto?> UpdateAsync(Guid id, LeaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LeaseContracts
            .Include(x => x.Property)
            .Include(x => x.Tenant)
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
        entity.AdjustmentIndex = request.AdjustmentIndex?.Trim();
        entity.PaymentDay = request.PaymentDay;
        entity.PaymentLocation = request.PaymentLocation?.Trim();
        entity.GuaranteeType = request.GuaranteeType?.Trim();
        entity.GuaranteeDetails = request.GuaranteeDetails?.Trim();
        entity.Notes = request.Notes?.Trim();

        if (entity.Status is LeaseStatus.ENDED or LeaseStatus.CANCELED)
        {
            entity.Property.Status = PropertyStatus.AVAILABLE;
        }
        else if (entity.Status == LeaseStatus.ACTIVE)
        {
            entity.Property.Status = PropertyStatus.LEASED;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title, entity.Tenant.Name);
    }

    public async Task<LeaseDto?> CloseAsync(Guid id, LeaseCloseRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.LeaseContracts
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.EndDate = request.EndDate;
        entity.Status = LeaseStatus.ENDED;
        entity.Property.Status = PropertyStatus.AVAILABLE;

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
            entity.AdjustmentIndex,
            entity.PaymentDay,
            entity.PaymentLocation,
            entity.GuaranteeType,
            entity.GuaranteeDetails,
            entity.Notes,
            entity.CreatedAtUtc);
}
