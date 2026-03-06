using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Maintenance;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class MaintenanceService : IMaintenanceService
{
    private readonly AppDbContext _dbContext;

    public MaintenanceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<MaintenanceDto>> QueryAsync(MaintenanceQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.MaintenanceRequests
            .AsNoTracking()
            .Include(x => x.Property)
            .AsQueryable();

        if (request.PropertyId.HasValue)
        {
            query = query.Where(x => x.PropertyId == request.PropertyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<MaintenanceStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            var priority = ServiceHelpers.ParseEnum<MaintenancePriority>(request.Priority, "priority");
            query = query.Where(x => x.Priority == priority);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.RequestedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => ToDto(x, x.Property.Title))
            .ToListAsync(cancellationToken);

        return new PagedResult<MaintenanceDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<MaintenanceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MaintenanceRequests
            .AsNoTracking()
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity, entity.Property.Title);
    }

    public async Task<MaintenanceDto> CreateAsync(MaintenanceCreateRequest request, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == request.PropertyId, cancellationToken)
            ?? throw new AppException("Property not found.", 404, "not_found");

        var entity = new MaintenanceRequest
        {
            PropertyId = request.PropertyId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Priority = ServiceHelpers.ParseEnum<MaintenancePriority>(request.Priority, "priority"),
            Status = MaintenanceStatus.OPEN,
            EstimatedCost = request.EstimatedCost,
            RequestedAtUtc = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Description = $"{entity.Description}\n\nNotes: {request.Notes.Trim()}";
        }

        _dbContext.MaintenanceRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, property.Title);
    }

    public async Task<MaintenanceDto?> UpdateAsync(Guid id, MaintenanceUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MaintenanceRequests
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.Priority = ServiceHelpers.ParseEnum<MaintenancePriority>(request.Priority, "priority");
        entity.Status = ServiceHelpers.ParseEnum<MaintenanceStatus>(request.Status, "status");
        entity.EstimatedCost = request.EstimatedCost;
        entity.ActualCost = request.ActualCost;
        entity.StartedAtUtc = request.StartedAtUtc;
        entity.FinishedAtUtc = request.FinishedAtUtc;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Description = $"{entity.Description}\n\nNotes: {request.Notes.Trim()}";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title);
    }

    public async Task<MaintenanceDto?> UpdateStatusAsync(Guid id, MaintenanceStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.MaintenanceRequests
            .Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Status = ServiceHelpers.ParseEnum<MaintenanceStatus>(request.Status, "status");

        if (entity.Status == MaintenanceStatus.IN_PROGRESS && !entity.StartedAtUtc.HasValue)
        {
            entity.StartedAtUtc = DateTime.UtcNow;
        }

        if (entity.Status == MaintenanceStatus.DONE && !entity.FinishedAtUtc.HasValue)
        {
            entity.FinishedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, entity.Property.Title);
    }

    private static MaintenanceDto ToDto(MaintenanceRequest entity, string propertyTitle)
    {
        string? notes = null;
        var description = entity.Description;
        const string marker = "\n\nNotes:";
        var index = description.IndexOf(marker, StringComparison.Ordinal);
        if (index >= 0)
        {
            notes = description[(index + marker.Length)..].Trim();
            description = description[..index];
        }

        return new MaintenanceDto(
            entity.Id,
            entity.PropertyId,
            propertyTitle,
            entity.Title,
            description,
            entity.Priority.ToString(),
            entity.Status.ToString(),
            entity.EstimatedCost,
            entity.ActualCost,
            entity.RequestedAtUtc,
            entity.StartedAtUtc,
            entity.FinishedAtUtc,
            notes,
            entity.CreatedAtUtc);
    }
}
