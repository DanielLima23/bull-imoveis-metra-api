using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Imoveis.Infrastructure.Services;

public sealed class PropertyService : IPropertyService
{
    private readonly AppDbContext _dbContext;

    public PropertyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PropertyDto>> QueryAsync(PropertyQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.Properties.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search)
                || x.City.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = ServiceHelpers.ParseEnum<PropertyStatus>(request.Status, "status");
            query = query.Where(x => x.Status == status);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                Property = x,
                CurrentRent = x.RentReferences
                    .OrderByDescending(r => r.EffectiveFrom)
                    .Select(r => (decimal?)r.Amount)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = entities
            .Select(x => ToDto(x.Property, x.CurrentRent))
            .ToList();

        return new PagedResult<PropertyDto>(
            items,
            page,
            pageSize,
            totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<PropertyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Properties
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Property = x,
                CurrentRent = x.RentReferences
                    .OrderByDescending(r => r.EffectiveFrom)
                    .Select(r => (decimal?)r.Amount)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDto(entity.Property, entity.CurrentRent);
    }

    public async Task<PropertyDto> CreateAsync(PropertyCreateRequest request, CancellationToken cancellationToken)
    {
        if (request.InitialRentAmount.HasValue ^ request.InitialRentEffectiveFrom.HasValue)
        {
            throw new AppException("Initial rent amount and effective date must be provided together.", 400, "validation_error");
        }

        if (request.InitialRentAmount.HasValue && request.InitialRentAmount.Value <= 0)
        {
            throw new AppException("Initial rent amount must be greater than zero.", 400, "validation_error");
        }

        var internalCode = await GenerateUniqueCodeAsync(request.Title, cancellationToken);

        var entity = new Property
        {
            Code = internalCode,
            Title = request.Title.Trim(),
            AddressLine1 = request.AddressLine1.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim().ToUpperInvariant(),
            ZipCode = request.ZipCode.Trim(),
            PropertyType = request.PropertyType.Trim(),
            Notes = request.Notes?.Trim(),
            Status = ServiceHelpers.ParseEnum<PropertyStatus>(request.Status, "status")
        };

        _dbContext.Properties.Add(entity);

        if (request.InitialRentAmount.HasValue && request.InitialRentEffectiveFrom.HasValue)
        {
            _dbContext.PropertyRentReferences.Add(new PropertyRentReference
            {
                Property = entity,
                Amount = request.InitialRentAmount.Value,
                EffectiveFrom = request.InitialRentEffectiveFrom.Value
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, request.InitialRentAmount);
    }

    public async Task<PropertyDto?> UpdateAsync(Guid id, PropertyUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Title.Trim();
        entity.AddressLine1 = request.AddressLine1.Trim();
        entity.City = request.City.Trim();
        entity.State = request.State.Trim().ToUpperInvariant();
        entity.ZipCode = request.ZipCode.Trim();
        entity.PropertyType = request.PropertyType.Trim();
        entity.Status = ServiceHelpers.ParseEnum<PropertyStatus>(request.Status, "status");
        entity.Notes = request.Notes?.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        var currentRent = await _dbContext.PropertyRentReferences
            .Where(x => x.PropertyId == entity.Id)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        return ToDto(entity, currentRent);
    }

    public async Task<PropertyDto?> UpdateStatusAsync(Guid id, PropertyStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Properties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = ServiceHelpers.ParseEnum<PropertyStatus>(request.Status, "status");
        await _dbContext.SaveChangesAsync(cancellationToken);

        var currentRent = await _dbContext.PropertyRentReferences
            .Where(x => x.PropertyId == entity.Id)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        return ToDto(entity, currentRent);
    }

    public async Task<PropertyRentReferenceDto?> AddRentReferenceAsync(Guid propertyId, PropertyRentReferenceCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var entity = new PropertyRentReference
        {
            PropertyId = propertyId,
            Amount = request.Amount,
            EffectiveFrom = request.EffectiveFrom
        };

        _dbContext.PropertyRentReferences.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyRentReferenceDto(entity.Id, entity.Amount, entity.EffectiveFrom);
    }

    public async Task<IReadOnlyList<PropertyRentReferenceDto>> GetRentHistoryAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.PropertyRentReferences
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new PropertyRentReferenceDto(x.Id, x.Amount, x.EffectiveFrom))
            .ToListAsync(cancellationToken);

        return items;
    }

    private static PropertyDto ToDto(Property entity, decimal? currentBaseRent)
        => new(
            entity.Id,
            entity.Code,
            entity.Title,
            entity.AddressLine1,
            entity.City,
            entity.State,
            entity.ZipCode,
            entity.PropertyType,
            entity.Status.ToString(),
            entity.Notes,
            currentBaseRent,
            entity.CreatedAtUtc);

    private async Task<string> GenerateUniqueCodeAsync(string title, CancellationToken cancellationToken)
    {
        var titleKey = new string(
            title
                .Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                .ToArray()
        );

        var normalizedTitle = new string(titleKey.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            normalizedTitle = "IMV";
        }

        var aliasPart = normalizedTitle.Length >= 6
            ? normalizedTitle[..6]
            : normalizedTitle.PadRight(6, 'X');

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"IMV-{aliasPart}-{DateTime.UtcNow:yyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
            var exists = await _dbContext.Properties
                .AsNoTracking()
                .AnyAsync(x => x.Code == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"IMV-{Guid.NewGuid():N}".ToUpperInvariant();
    }
}
