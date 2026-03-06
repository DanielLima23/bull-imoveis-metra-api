using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Tenants;
using Imoveis.Domain.Entities;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class TenantService : ITenantService
{
    private readonly AppDbContext _dbContext;

    public TenantService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<TenantDto>> QueryAsync(TenantQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.Tenants.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search)
                || x.Email.ToLower().Contains(search)
                || x.DocumentNumber.ToLower().Contains(search));
        }

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.IsActive == request.Active.Value);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TenantDto(
                x.Id,
                x.Name,
                x.DocumentNumber,
                x.Email,
                x.Phone,
                x.IsActive,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<TenantDto>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TenantDto(
                x.Id,
                x.Name,
                x.DocumentNumber,
                x.Email,
                x.Phone,
                x.IsActive,
                x.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TenantDto> CreateAsync(TenantCreateRequest request, CancellationToken cancellationToken)
    {
        if (await _dbContext.Tenants.AnyAsync(x => x.DocumentNumber == request.DocumentNumber, cancellationToken))
        {
            throw new AppException("Tenant document already exists.", 409, "conflict_error");
        }

        var entity = new Tenant
        {
            Name = request.Name.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = true
        };

        _dbContext.Tenants.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantDto(entity.Id, entity.Name, entity.DocumentNumber, entity.Email, entity.Phone, entity.IsActive, entity.CreatedAtUtc);
    }

    public async Task<TenantDto?> UpdateAsync(Guid id, TenantUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Tenants.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (await _dbContext.Tenants.AnyAsync(x => x.Id != id && x.DocumentNumber == request.DocumentNumber, cancellationToken))
        {
            throw new AppException("Tenant document already exists.", 409, "conflict_error");
        }

        entity.Name = request.Name.Trim();
        entity.DocumentNumber = request.DocumentNumber.Trim();
        entity.Email = request.Email.Trim();
        entity.Phone = request.Phone.Trim();
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TenantDto(entity.Id, entity.Name, entity.DocumentNumber, entity.Email, entity.Phone, entity.IsActive, entity.CreatedAtUtc);
    }
}
