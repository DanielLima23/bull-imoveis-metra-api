using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Visits;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class VisitService : IVisitService
{
    private readonly AppDbContext _dbContext;

    public VisitService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<VisitDto>> QueryAsync(VisitQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.PropertyVisits
            .AsNoTracking()
            .Include(x => x.Property)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.PropertyId == request.PropertyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<VisitStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.ScheduledAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x, x.Property.Title))
            .ToListAsync(cancellationToken);

        return new PagedResult<VisitDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<VisitDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyVisits
            .AsNoTracking()
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity, entity.Property.Title);
    }

    public async Task<VisitDto> CreateAsync(VisitCreateRequest request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == request.PropertyId, cancellationToken)
            ?? throw new AppException("Property not found.", 404, "not_found");

        var entity = new PropertyVisit
        {
            PropertyId = request.PropertyId,
            ScheduledAtUtc = request.ScheduledAtUtc,
            ContactName = request.ContactName.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
            ResponsibleName = request.ResponsibleName?.Trim(),
            Status = VisitStatus.SCHEDULED,
            Notes = request.Notes?.Trim()
        };

        _dbContext.PropertyVisits.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, property.Title);
    }

    public async Task<VisitDto?> UpdateAsync(Guid id, VisitUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyVisits
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.ScheduledAtUtc = request.ScheduledAtUtc;
        entity.ContactName = request.ContactName.Trim();
        entity.ContactPhone = request.ContactPhone?.Trim();
        entity.ResponsibleName = request.ResponsibleName?.Trim();
        entity.Status = ServiceHelpers.ParseEnum<VisitStatus>(request.Status, "status");
        entity.Notes = request.Notes?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title);
    }

    public async Task<VisitDto?> UpdateStatusAsync(Guid id, VisitStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyVisits
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = ServiceHelpers.ParseEnum<VisitStatus>(request.Status, "status");

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title);
    }

    private static VisitDto ToDto(PropertyVisit entity, string propertyTitle)
        => new(
            entity.Id,
            entity.PropertyId,
            propertyTitle,
            entity.ScheduledAtUtc,
            entity.ContactName,
            entity.ContactPhone,
            entity.ResponsibleName,
            entity.Status.ToString(),
            entity.Notes,
            entity.CreatedAtUtc);
}
