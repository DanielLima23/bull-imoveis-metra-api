using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Pendencies;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class PendencyService : IPendencyService
{
    private readonly AppDbContext _dbContext;

    public PendencyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PendencyTypeDto>> ListTypesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.PendencyTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new PendencyTypeDto(x.Id, x.Name, x.DefaultSlaDays))
            .ToListAsync(cancellationToken);
    }

    public async Task<PendencyTypeDto> CreateTypeAsync(PendencyTypeCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.DefaultSlaDays <= 0)
        {
            throw new AppException("DefaultSlaDays must be greater than zero.", 400, "validation_error");
        }

        if (await _dbContext.PendencyTypes.AnyAsync(x => x.Name == request.Name, cancellationToken))
        {
            throw new AppException("Pendency type already exists.", 409, "conflict_error");
        }

        var entity = new PendencyType
        {
            Name = request.Name.Trim(),
            DefaultSlaDays = request.DefaultSlaDays
        };

        _dbContext.PendencyTypes.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PendencyTypeDto(entity.Id, entity.Name, entity.DefaultSlaDays);
    }

    public async Task<PendencyTypeDto?> UpdateTypeAsync(Guid id, PendencyTypeUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PendencyTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (request.DefaultSlaDays <= 0)
        {
            throw new AppException("DefaultSlaDays must be greater than zero.", 400, "validation_error");
        }

        if (await _dbContext.PendencyTypes.AnyAsync(x => x.Id != id && x.Name == request.Name, cancellationToken))
        {
            throw new AppException("Pendency type already exists.", 409, "conflict_error");
        }

        entity.Name = request.Name.Trim();
        entity.DefaultSlaDays = request.DefaultSlaDays;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PendencyTypeDto(entity.Id, entity.Name, entity.DefaultSlaDays);
    }

    public async Task<PagedResult<PendencyDto>> QueryAsync(PendencyQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.PropertyId == request.PropertyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<PendencyStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(x => x.DueAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(ToDto).ToList();

        return new PagedResult<PendencyDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<PendencyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<PendencyDto> CreateAsync(PendencyCreateRequest request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == request.PropertyId, cancellationToken)
            ?? throw new AppException("Property not found.", 404, "not_found");

        var pendencyType = await _dbContext.PendencyTypes.FirstOrDefaultAsync(x => x.Id == request.PendencyTypeId, cancellationToken)
            ?? throw new AppException("Pendency type not found.", 404, "not_found");

        var entity = new PendencyItem
        {
            PropertyId = property.Id,
            PendencyTypeId = pendencyType.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            DueAtUtc = request.DueAtUtc,
            OpenedAtUtc = DateTime.UtcNow,
            Status = PendencyStatus.OPEN
        };

        _dbContext.PendencyItems.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        entity.Property = property;
        entity.PendencyType = pendencyType;

        return ToDto(entity);
    }

    public async Task<PendencyDto?> UpdateAsync(Guid id, PendencyUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PendencyItems
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description?.Trim();
        entity.DueAtUtc = request.DueAtUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<PendencyDto?> ResolveAsync(Guid id, PendencyResolveRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PendencyItems
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = PendencyStatus.RESOLVED;
        entity.ResolvedAtUtc = request.ResolvedAtUtc ?? DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    private static PendencyDto ToDto(PendencyItem entity)
    {
        var today = DateTime.UtcNow.Date;
        var opened = entity.OpenedAtUtc.Date;
        var elapsedDays = Math.Max(0, (int)(today - opened).TotalDays);
        var slaDays = Math.Max(1, entity.PendencyType.DefaultSlaDays);

        var percent = elapsedDays / (decimal)slaDays;
        var severity = percent < 0.8m
            ? "ATTENTION"
            : percent <= 1m
                ? "URGENT"
                : "CRITICAL";

        return new PendencyDto(
            entity.Id,
            entity.PropertyId,
            entity.PendencyTypeId,
            entity.Property.Title,
            entity.PendencyType.Name,
            entity.Title,
            entity.Description,
            entity.OpenedAtUtc,
            entity.DueAtUtc,
            entity.ResolvedAtUtc,
            entity.Status.ToString(),
            severity,
            slaDays,
            elapsedDays,
            entity.CreatedAtUtc);
    }
}
