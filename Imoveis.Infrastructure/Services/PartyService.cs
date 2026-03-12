using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Parties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class PartyService : IPartyService
{
    private readonly AppDbContext _dbContext;

    public PartyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PartyDto>> QueryAsync(PartyQueryRequest request, CancellationToken cancellationToken)
    {
        var page = ServiceHelpers.NormalizePage(request.Page);
        var pageSize = ServiceHelpers.NormalizePageSize(request.PageSize);

        var query = _dbContext.Parties.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search)
                || (x.DocumentNumber != null && x.DocumentNumber.ToLower().Contains(search))
                || (x.Email != null && x.Email.ToLower().Contains(search))
                || (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Kind))
        {
            var kind = PartyKindContract.Parse(request.Kind, "kind");
            query = query.Where(x => x.Kind == kind);
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
            .Select(ToDtoExpression())
            .ToListAsync(cancellationToken);

        return new PagedResult<PartyDto>(items, page, pageSize, totalItems, (int)Math.Ceiling(totalItems / (double)pageSize));
    }

    public async Task<PartyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Parties
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PartyDto> CreateAsync(PartyCreateRequest request, CancellationToken cancellationToken)
    {
        var entity = new Party
        {
            Kind = PartyKindContract.Parse(request.Kind, "kind"),
            Name = request.Name.Trim(),
            DocumentNumber = request.DocumentNumber?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Notes = request.Notes?.Trim(),
            IsActive = true
        };

        _dbContext.Parties.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<PartyDto?> UpdateAsync(Guid id, PartyUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Parties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Kind = PartyKindContract.Parse(request.Kind, "kind");
        entity.Name = request.Name.Trim();
        entity.DocumentNumber = request.DocumentNumber?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Notes = request.Notes?.Trim();
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Parties
            .Include(x => x.PropertyLinks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        if (entity.PropertyLinks.Count > 0)
        {
            _dbContext.PropertyPartyLinks.RemoveRange(entity.PropertyLinks);
        }

        _dbContext.Parties.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static System.Linq.Expressions.Expression<Func<Party, PartyDto>> ToDtoExpression()
        => x => new PartyDto(
            x.Id,
            x.Kind.ToString(),
            x.Name,
            x.DocumentNumber,
            x.Email,
            x.Phone,
            x.Notes,
            x.IsActive,
            x.CreatedAtUtc);

    private static PartyDto ToDto(Party entity)
        => new(
            entity.Id,
            entity.Kind.ToString(),
            entity.Name,
            entity.DocumentNumber,
            entity.Email,
            entity.Phone,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAtUtc);
}
