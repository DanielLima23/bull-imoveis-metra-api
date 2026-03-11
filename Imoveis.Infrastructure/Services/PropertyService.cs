using System.Globalization;
using System.Text;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Leases;
using Imoveis.Application.Contracts.Pendencies;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search)
                || x.City.ToLower().Contains(search)
                || x.AddressLine1.ToLower().Contains(search)
                || (x.RegistrationNumber != null && x.RegistrationNumber.ToLower().Contains(search))
                || x.Code.ToLower().Contains(search));
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

        var items = entities.Select(x => ToDto(x.Property, x.CurrentRent)).ToList();

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

    public async Task<PropertyDetailDto?> GetDetailAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == propertyId, cancellationToken);

        if (property is null)
        {
            return null;
        }

        var currentBaseRent = await _dbContext.PropertyRentReferences
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefaultAsync(cancellationToken);

        var activeLeaseEntity = await _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Where(x => x.PropertyId == propertyId && x.Status == LeaseStatus.ACTIVE)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var leaseHistoryEntities = await _dbContext.LeaseContracts
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.Tenant)
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var openPendenciesEntities = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.Property)
            .Include(x => x.PendencyType)
            .Where(x => x.PropertyId == propertyId && x.Status == PendencyStatus.OPEN)
            .OrderBy(x => x.DueAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var history = await GetHistoryAsync(propertyId, cancellationToken);
        var documents = await GetDocumentsAsync(propertyId, cancellationToken);
        var relationships = await GetRelationshipsAsync(propertyId, cancellationToken);
        var rentHistory = await GetRentHistoryAsync(propertyId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var unpaidInstallments = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .Where(x => x.PropertyExpense.PropertyId == propertyId && x.Status != ExpenseStatus.PAID && x.Status != ExpenseStatus.CANCELED)
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.InstallmentNumber)
            .ToListAsync(cancellationToken);

        var upcomingInstallments = unpaidInstallments
            .Take(12)
            .Select(x => new PropertyFinancialInstallmentDto(
                x.Id,
                x.PropertyExpense.ExpenseType.Name,
                x.PropertyExpense.Description,
                x.DueDate,
                x.Amount,
                ResolveInstallmentStatus(x, today)))
            .ToList();

        var financial = new PropertyFinancialOverviewDto(
            currentBaseRent,
            activeLeaseEntity?.MonthlyRent,
            Math.Round(unpaidInstallments.Sum(x => x.Amount), 2),
            Math.Round(unpaidInstallments.Where(x => x.DueDate < today).Sum(x => x.Amount), 2),
            upcomingInstallments);

        return new PropertyDetailDto(
            ToDto(property, currentBaseRent),
            activeLeaseEntity is null ? null : ToLeaseDto(activeLeaseEntity, activeLeaseEntity.Property.Title, activeLeaseEntity.Tenant.Name),
            leaseHistoryEntities.Select(x => ToLeaseDto(x, x.Property.Title, x.Tenant.Name)).ToList(),
            financial,
            openPendenciesEntities.Select(ToPendencyDto).ToList(),
            history,
            documents,
            relationships,
            rentHistory);
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
            RegistrationNumber = request.RegistrationNumber?.Trim(),
            DeedNumber = request.DeedNumber?.Trim(),
            RegistrationCertificate = request.RegistrationCertificate?.Trim(),
            Bedrooms = request.Bedrooms,
            HasElevator = request.HasElevator,
            HasGarage = request.HasGarage,
            VacatedAt = request.VacatedAt,
            VacancyReason = request.VacancyReason?.Trim(),
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
        entity.RegistrationNumber = request.RegistrationNumber?.Trim();
        entity.DeedNumber = request.DeedNumber?.Trim();
        entity.RegistrationCertificate = request.RegistrationCertificate?.Trim();
        entity.Bedrooms = request.Bedrooms;
        entity.HasElevator = request.HasElevator;
        entity.HasGarage = request.HasGarage;
        entity.VacatedAt = request.VacatedAt;
        entity.VacancyReason = request.VacancyReason?.Trim();

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
        if (request.Amount <= 0)
        {
            throw new AppException("Amount must be greater than zero.", 400, "validation_error");
        }

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
        return await _dbContext.PropertyRentReferences
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new PropertyRentReferenceDto(x.Id, x.Amount, x.EffectiveFrom))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyHistoryEntryDto>> GetHistoryAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyHistoryEntries
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new PropertyHistoryEntryDto(x.Id, x.Title, x.Description, x.OccurredAtUtc, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyHistoryEntryDto?> AddHistoryAsync(Guid propertyId, PropertyHistoryEntryCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var entity = new PropertyHistoryEntry
        {
            PropertyId = propertyId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            OccurredAtUtc = request.OccurredAtUtc
        };

        _dbContext.PropertyHistoryEntries.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyHistoryEntryDto(entity.Id, entity.Title, entity.Description, entity.OccurredAtUtc, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<PropertyDocumentDto>> GetDocumentsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyDocuments
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PropertyDocumentDto(x.Id, x.Name, x.Kind, x.Url, x.Notes, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyDocumentDto?> AddDocumentAsync(Guid propertyId, PropertyDocumentCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var entity = new PropertyDocument
        {
            PropertyId = propertyId,
            Name = request.Name.Trim(),
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "DOCUMENT" : request.Kind.Trim().ToUpperInvariant(),
            Url = request.Url.Trim(),
            Notes = request.Notes?.Trim()
        };

        _dbContext.PropertyDocuments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyDocumentDto(entity.Id, entity.Name, entity.Kind, entity.Url, entity.Notes, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<PropertyPartyLinkDto>> GetRelationshipsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyPartyLinks
            .AsNoTracking()
            .Include(x => x.Party)
            .Where(x => x.PropertyId == propertyId)
            .OrderBy(x => x.Role)
            .ThenByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Party.Name)
            .Select(x => new PropertyPartyLinkDto(
                x.Id,
                x.PropertyId,
                x.PartyId,
                x.Role.ToString(),
                x.IsPrimary,
                x.StartsAtUtc,
                x.EndsAtUtc,
                x.Notes,
                x.Party.Kind.ToString(),
                x.Party.Name,
                x.Party.DocumentNumber,
                x.Party.Email,
                x.Party.Phone))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyPartyLinkDto?> LinkPartyAsync(Guid propertyId, PropertyPartyLinkCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var party = await _dbContext.Parties.FirstOrDefaultAsync(x => x.Id == request.PartyId, cancellationToken);
        if (party is null)
        {
            throw new AppException("Party not found.", 404, "not_found");
        }

        var role = ServiceHelpers.ParseEnum<PropertyPartyRole>(request.Role, "role");

        var entity = new PropertyPartyLink
        {
            PropertyId = propertyId,
            PartyId = party.Id,
            Role = role,
            IsPrimary = request.IsPrimary,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            Notes = request.Notes?.Trim()
        };

        _dbContext.PropertyPartyLinks.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyPartyLinkDto(
            entity.Id,
            entity.PropertyId,
            entity.PartyId,
            entity.Role.ToString(),
            entity.IsPrimary,
            entity.StartsAtUtc,
            entity.EndsAtUtc,
            entity.Notes,
            party.Kind.ToString(),
            party.Name,
            party.DocumentNumber,
            party.Email,
            party.Phone);
    }

    private static string ResolveInstallmentStatus(ExpenseInstallment installment, DateOnly today)
    {
        if (installment.Status == ExpenseStatus.PAID)
        {
            return ExpenseStatus.PAID.ToString();
        }

        return installment.DueDate < today ? ExpenseStatus.OVERDUE.ToString() : installment.Status.ToString();
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
            entity.RegistrationNumber,
            entity.DeedNumber,
            entity.RegistrationCertificate,
            entity.Bedrooms,
            entity.HasElevator,
            entity.HasGarage,
            entity.VacatedAt,
            entity.VacancyReason,
            currentBaseRent,
            entity.CreatedAtUtc);

    private static LeaseDto ToLeaseDto(LeaseContract entity, string propertyTitle, string tenantName)
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

    private static PendencyDto ToPendencyDto(PendencyItem entity)
    {
        var today = DateTime.UtcNow.Date;
        var opened = entity.OpenedAtUtc.Date;
        var elapsedDays = Math.Max(0, (int)(today - opened).TotalDays);
        var slaDays = Math.Max(1, entity.PendencyType.DefaultSlaDays);
        var percent = elapsedDays / (decimal)slaDays;
        var severity = percent < 0.8m ? "ATTENTION" : percent <= 1m ? "URGENT" : "CRITICAL";

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
