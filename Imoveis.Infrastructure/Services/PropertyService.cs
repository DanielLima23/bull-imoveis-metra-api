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

        var query = _dbContext.Properties
            .AsNoTracking()
            .Include(x => x.RentReferences)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search)
                || x.City.ToLower().Contains(search)
                || (x.Proprietary != null && x.Proprietary.ToLower().Contains(search))
                || (x.Administrator != null && x.Administrator.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.OccupancyStatus))
        {
            var occupancyStatus = ServiceHelpers.ParseEnum<PropertyOccupancyStatus>(request.OccupancyStatus, "occupancyStatus");
            query = query.Where(x => x.OccupancyStatus == occupancyStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.AssetState))
        {
            var assetState = ServiceHelpers.ParseEnum<PropertyAssetState>(request.AssetState, "assetState");
            query = query.Where(x => x.AssetState == assetState);
        }

        if (!string.IsNullOrWhiteSpace(request.PropertyType))
        {
            var propertyType = request.PropertyType.Trim().ToLowerInvariant();
            query = query.Where(x => x.PropertyType.ToLower() == propertyType);
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLowerInvariant();
            query = query.Where(x => x.City.ToLower().Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.Proprietary))
        {
            var proprietary = request.Proprietary.Trim().ToLowerInvariant();
            query = query.Where(x => x.Proprietary != null && x.Proprietary.ToLower().Contains(proprietary));
        }

        if (!string.IsNullOrWhiteSpace(request.Administrator))
        {
            var administrator = request.Administrator.Trim().ToLowerInvariant();
            query = query.Where(x => x.Administrator != null && x.Administrator.ToLower().Contains(administrator));
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities
            .Select(x => ToDto(x, ResolveCurrentBaseRent(x)))
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
            .Include(x => x.RentReferences)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : ToDto(entity, ResolveCurrentBaseRent(entity));
    }

    public async Task<PropertyDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Properties
            .AsNoTracking()
            .Include(x => x.RentReferences)
            .Include(x => x.ChargeTemplates)
            .Include(x => x.HistoryEntries)
            .Include(x => x.Attachments)
            .Include(x => x.Pendencies)
                .ThenInclude(x => x.PendencyType)
            .Include(x => x.Visits)
            .Include(x => x.MaintenanceRequests)
            .Include(x => x.Leases)
                .ThenInclude(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var currentLease = entity.Leases
            .Where(x => x.Status == LeaseStatus.ACTIVE)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new PropertyCurrentLeaseDto(
                x.Id,
                x.TenantId,
                x.Tenant.Name,
                x.StartDate,
                x.EndDate,
                x.MonthlyRent,
                x.Status.ToString(),
                x.PaymentDay,
                x.PaymentLocation))
            .FirstOrDefault();

        var detail = new PropertyDetailDto(
            ToDto(entity, ResolveCurrentBaseRent(entity)),
            currentLease,
            entity.RentReferences
                .OrderByDescending(x => x.EffectiveFrom)
                .Select(x => new PropertyRentReferenceDto(x.Id, x.Amount, x.EffectiveFrom))
                .ToList(),
            entity.ChargeTemplates
                .OrderBy(x => x.Kind)
                .ThenBy(x => x.Title)
                .Select(ToChargeTemplateDto)
                .ToList(),
            entity.HistoryEntries
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(x => new PropertyHistoryEntryDto(x.Id, x.Content, x.OccurredAtUtc, x.CreatedAtUtc))
                .ToList(),
            entity.Attachments
                .OrderByDescending(x => x.ReferenceDateUtc ?? x.CreatedAtUtc)
                .Select(x => new PropertyAttachmentDto(x.Id, x.Category, x.Title, x.ResourceLocation, x.Notes, x.ReferenceDateUtc, x.CreatedAtUtc))
                .ToList(),
            entity.Pendencies
                .Where(x => x.Status == PendencyStatus.OPEN)
                .OrderBy(x => x.DueAtUtc)
                .Select(x => new PropertyOpenPendencyDto(
                    x.Id,
                    x.PendencyType.Code,
                    x.PendencyType.Name,
                    x.Title,
                    x.DueAtUtc,
                    ResolvePendencySeverity(x.OpenedAtUtc, x.PendencyType.DefaultSlaDays),
                    x.Status.ToString()))
                .ToList(),
            entity.Visits
                .Where(x => x.Status != VisitStatus.CANCELED)
                .OrderBy(x => x.ScheduledAtUtc)
                .Take(10)
                .Select(x => new PropertyUpcomingVisitDto(x.Id, x.ScheduledAtUtc, x.ContactName, x.Status.ToString()))
                .ToList(),
            entity.MaintenanceRequests
                .OrderByDescending(x => x.RequestedAtUtc)
                .Take(10)
                .Select(x => new PropertyMaintenanceSummaryDto(x.Id, x.Title, x.Priority.ToString(), x.Status.ToString(), x.EstimatedCost, x.ActualCost))
                .ToList());

        return detail;
    }

    public async Task<PropertyDto> CreateAsync(PropertyCreateRequest request, CancellationToken cancellationToken)
    {
        ValidateInitialRent(request.InitialRentAmount, request.InitialRentEffectiveFrom);

        var internalCode = await GenerateUniqueCodeAsync(request.Identity.Title, cancellationToken);

        var entity = new Property
        {
            Code = internalCode,
            Title = request.Identity.Title.Trim(),
            AddressLine1 = request.Identity.AddressLine1.Trim(),
            City = request.Identity.City.Trim(),
            State = request.Identity.State.Trim().ToUpperInvariant(),
            ZipCode = request.Identity.ZipCode.Trim(),
            PropertyType = request.Identity.PropertyType.Trim(),
            OccupancyStatus = PropertyOccupancyStatus.VACANT,
            AssetState = PropertyAssetState.READY,
            Registration = NormalizeNullable(request.Documentation.Registration),
            Scripture = NormalizeNullable(request.Documentation.Scripture),
            RegistrationCertification = NormalizeNullable(request.Documentation.RegistrationCertification),
            NumOfRooms = request.Characteristics.NumOfRooms,
            CleaningIncluded = request.Characteristics.CleaningIncluded,
            Elevator = request.Characteristics.Elevator,
            Garage = request.Characteristics.Garage,
            UnoccupiedSince = request.Characteristics.UnoccupiedSince,
            Proprietary = NormalizeNullable(request.Administration.Proprietary),
            Administrator = NormalizeNullable(request.Administration.Administrator),
            AdministratorPhone = NormalizeNullable(request.Administration.AdministratorPhone),
            AdministratorEmail = NormalizeNullable(request.Administration.AdministratorEmail),
            AdministrateTax = NormalizeNullable(request.Administration.AdministrateTax),
            Lawyer = NormalizeNullable(request.Administration.Lawyer),
            LawyerData = NormalizeNullable(request.Administration.LawyerData),
            Observation = NormalizeNullable(request.Administration.Observation)
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
        var entity = await _dbContext.Properties
            .Include(x => x.RentReferences)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Title = request.Identity.Title.Trim();
        entity.AddressLine1 = request.Identity.AddressLine1.Trim();
        entity.City = request.Identity.City.Trim();
        entity.State = request.Identity.State.Trim().ToUpperInvariant();
        entity.ZipCode = request.Identity.ZipCode.Trim();
        entity.PropertyType = request.Identity.PropertyType.Trim();
        entity.Registration = NormalizeNullable(request.Documentation.Registration);
        entity.Scripture = NormalizeNullable(request.Documentation.Scripture);
        entity.RegistrationCertification = NormalizeNullable(request.Documentation.RegistrationCertification);
        entity.NumOfRooms = request.Characteristics.NumOfRooms;
        entity.CleaningIncluded = request.Characteristics.CleaningIncluded;
        entity.Elevator = request.Characteristics.Elevator;
        entity.Garage = request.Characteristics.Garage;
        entity.UnoccupiedSince = request.Characteristics.UnoccupiedSince;
        entity.Proprietary = NormalizeNullable(request.Administration.Proprietary);
        entity.Administrator = NormalizeNullable(request.Administration.Administrator);
        entity.AdministratorPhone = NormalizeNullable(request.Administration.AdministratorPhone);
        entity.AdministratorEmail = NormalizeNullable(request.Administration.AdministratorEmail);
        entity.AdministrateTax = NormalizeNullable(request.Administration.AdministrateTax);
        entity.Lawyer = NormalizeNullable(request.Administration.Lawyer);
        entity.LawyerData = NormalizeNullable(request.Administration.LawyerData);
        entity.Observation = NormalizeNullable(request.Administration.Observation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, ResolveCurrentBaseRent(entity));
    }

    public async Task<PropertyDto?> UpdateStatusAsync(Guid id, PropertyStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.Properties
            .Include(x => x.RentReferences)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.OccupancyStatus = ServiceHelpers.ParseEnum<PropertyOccupancyStatus>(request.OccupancyStatus, "occupancyStatus");
        entity.AssetState = ServiceHelpers.ParseEnum<PropertyAssetState>(request.AssetState, "assetState");

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity, ResolveCurrentBaseRent(entity));
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
        return await _dbContext.PropertyRentReferences
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new PropertyRentReferenceDto(x.Id, x.Amount, x.EffectiveFrom))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PropertyChargeTemplateDto>> ListChargeTemplatesAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyChargeTemplates
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Title)
            .Select(x => ToChargeTemplateDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyChargeTemplateDto?> AddChargeTemplateAsync(Guid propertyId, PropertyChargeTemplateCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var entity = new PropertyChargeTemplate
        {
            PropertyId = propertyId,
            Kind = ServiceHelpers.ParseEnum<ChargeTemplateKind>(request.Kind, "kind"),
            Title = request.Title.Trim(),
            DefaultAmount = request.DefaultAmount,
            DueDay = request.DueDay,
            DefaultResponsibility = ServiceHelpers.ParseEnum<ChargeResponsibility>(request.DefaultResponsibility, "defaultResponsibility"),
            ProviderInformation = NormalizeNullable(request.ProviderInformation),
            Notes = NormalizeNullable(request.Notes),
            IsActive = request.IsActive
        };

        _dbContext.PropertyChargeTemplates.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToChargeTemplateDto(entity);
    }

    public async Task<PropertyChargeTemplateDto?> UpdateChargeTemplateAsync(Guid propertyId, Guid templateId, PropertyChargeTemplateUpdateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _dbContext.PropertyChargeTemplates
            .FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.Id == templateId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Kind = ServiceHelpers.ParseEnum<ChargeTemplateKind>(request.Kind, "kind");
        entity.Title = request.Title.Trim();
        entity.DefaultAmount = request.DefaultAmount;
        entity.DueDay = request.DueDay;
        entity.DefaultResponsibility = ServiceHelpers.ParseEnum<ChargeResponsibility>(request.DefaultResponsibility, "defaultResponsibility");
        entity.ProviderInformation = NormalizeNullable(request.ProviderInformation);
        entity.Notes = NormalizeNullable(request.Notes);
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToChargeTemplateDto(entity);
    }

    public async Task<IReadOnlyList<PropertyHistoryEntryDto>> ListHistoryAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyHistoryEntries
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new PropertyHistoryEntryDto(x.Id, x.Content, x.OccurredAtUtc, x.CreatedAtUtc))
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
            Content = request.Content.Trim(),
            OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow
        };

        _dbContext.PropertyHistoryEntries.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyHistoryEntryDto(entity.Id, entity.Content, entity.OccurredAtUtc, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<PropertyAttachmentDto>> ListAttachmentsAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        return await _dbContext.PropertyAttachments
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId)
            .OrderByDescending(x => x.ReferenceDateUtc ?? x.CreatedAtUtc)
            .Select(x => new PropertyAttachmentDto(x.Id, x.Category, x.Title, x.ResourceLocation, x.Notes, x.ReferenceDateUtc, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PropertyAttachmentDto?> AddAttachmentAsync(Guid propertyId, PropertyAttachmentCreateRequest request, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var entity = new PropertyAttachment
        {
            PropertyId = propertyId,
            Category = request.Category.Trim(),
            Title = request.Title.Trim(),
            ResourceLocation = request.ResourceLocation.Trim(),
            Notes = NormalizeNullable(request.Notes),
            ReferenceDateUtc = request.ReferenceDateUtc
        };

        _dbContext.PropertyAttachments.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyAttachmentDto(entity.Id, entity.Category, entity.Title, entity.ResourceLocation, entity.Notes, entity.ReferenceDateUtc, entity.CreatedAtUtc);
    }

    public async Task<PropertyMonthlyStatementDto?> GetMonthlyStatementAsync(Guid propertyId, int year, int? month, CancellationToken cancellationToken)
    {
        var propertyExists = await _dbContext.Properties.AnyAsync(x => x.Id == propertyId, cancellationToken);
        if (!propertyExists)
        {
            return null;
        }

        var start = new DateOnly(year, month ?? 1, 1);
        var end = month.HasValue
            ? start.AddMonths(1).AddDays(-1)
            : new DateOnly(year, 12, 31);

        var receivables = await _dbContext.LeaseReceivableInstallments
            .AsNoTracking()
            .Include(x => x.LeaseContract)
                .ThenInclude(x => x.Property)
            .Where(x => x.LeaseContract.PropertyId == propertyId && x.CompetenceDate >= start && x.CompetenceDate <= end)
            .OrderBy(x => x.CompetenceDate)
            .ToListAsync(cancellationToken);

        var expenseInstallments = await _dbContext.ExpenseInstallments
            .AsNoTracking()
            .Include(x => x.PropertyExpense)
                .ThenInclude(x => x.ExpenseType)
            .Where(x => x.PropertyExpense.PropertyId == propertyId && x.DueDate >= start && x.DueDate <= end)
            .OrderBy(x => x.DueDate)
            .ToListAsync(cancellationToken);

        var pendencies = await _dbContext.PendencyItems
            .AsNoTracking()
            .Include(x => x.PendencyType)
            .Where(x => x.PropertyId == propertyId && x.DueAtUtc.Date >= start.ToDateTime(TimeOnly.MinValue).Date && x.DueAtUtc.Date <= end.ToDateTime(TimeOnly.MaxValue).Date)
            .OrderBy(x => x.DueAtUtc)
            .ToListAsync(cancellationToken);

        var chargeTemplates = await _dbContext.PropertyChargeTemplates
            .AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.IsActive)
            .OrderBy(x => x.Kind)
            .ToListAsync(cancellationToken);

        var lines = new List<PropertyMonthlyStatementLineDto>();

        lines.AddRange(receivables.Select(x => new PropertyMonthlyStatementLineDto(
            "LEASE_RECEIVABLE",
            "RENT",
            "Aluguel",
            x.CompetenceDate,
            x.DueDate,
            x.ExpectedAmount,
            x.PaidAmount,
            x.Status.ToString(),
            x.PaidBy,
            x.Notes)));

        lines.AddRange(expenseInstallments.Select(x => new PropertyMonthlyStatementLineDto(
            "EXPENSE_INSTALLMENT",
            NormalizeExpenseKind(x.PropertyExpense.ExpenseType.Name),
            x.PropertyExpense.ExpenseType.Name,
            new DateOnly(x.DueDate.Year, x.DueDate.Month, 1),
            x.DueDate,
            x.Amount,
            x.PaidAmount,
            x.Status.ToString(),
            x.PaidBy,
            CombineNotes(x.PropertyExpense.Description, x.Notes ?? x.PropertyExpense.Notes))));

        if (month.HasValue)
        {
            foreach (var template in chargeTemplates)
            {
                var hasExpenseInMonth = expenseInstallments.Any(x =>
                    x.DueDate.Year == year
                    && x.DueDate.Month == month.Value
                    && MatchesTemplateKind(template.Kind, x.PropertyExpense.ExpenseType.Name));

                if (hasExpenseInMonth)
                {
                    continue;
                }

                var dueDay = Math.Clamp(template.DueDay ?? 10, 1, DateTime.DaysInMonth(year, month.Value));
                lines.Add(new PropertyMonthlyStatementLineDto(
                    "CHARGE_TEMPLATE",
                    template.Kind.ToString(),
                    template.Title,
                    new DateOnly(year, month.Value, 1),
                    new DateOnly(year, month.Value, dueDay),
                    template.DefaultAmount,
                    null,
                    "BASELINE",
                    template.DefaultResponsibility.ToString(),
                    CombineNotes(template.ProviderInformation, template.Notes)));
            }
        }

        lines.AddRange(pendencies.Select(x => new PropertyMonthlyStatementLineDto(
            "PENDENCY",
            "PENDENCY",
            $"{x.PendencyType.Code} - {x.Title}",
            new DateOnly(x.DueAtUtc.Year, x.DueAtUtc.Month, 1),
            DateOnly.FromDateTime(x.DueAtUtc.Date),
            0m,
            null,
            x.Status.ToString(),
            null,
            x.Description ?? x.PendencyType.Description)));

        return new PropertyMonthlyStatementDto(
            propertyId,
            year,
            month,
            lines
                .OrderBy(x => x.CompetenceDate)
                .ThenBy(x => x.DueDate)
                .ThenBy(x => x.Label)
                .ToList());
    }

    private static void ValidateInitialRent(decimal? amount, DateOnly? effectiveFrom)
    {
        if (amount.HasValue ^ effectiveFrom.HasValue)
        {
            throw new AppException("Initial rent amount and effective date must be provided together.", 400, "validation_error");
        }

        if (amount.HasValue && amount.Value <= 0)
        {
            throw new AppException("Initial rent amount must be greater than zero.", 400, "validation_error");
        }
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
            entity.OccupancyStatus.ToString(),
            entity.AssetState.ToString(),
            ComposeLegacyStatus(entity),
            entity.Proprietary,
            entity.Administrator,
            currentBaseRent,
            entity.CreatedAtUtc,
            new PropertyDocumentationSectionDto(entity.Registration, entity.Scripture, entity.RegistrationCertification),
            new PropertyCharacteristicsSectionDto(entity.NumOfRooms, entity.CleaningIncluded, entity.Elevator, entity.Garage, entity.UnoccupiedSince),
            new PropertyAdministrationSectionDto(
                entity.Proprietary,
                entity.Administrator,
                entity.AdministratorPhone,
                entity.AdministratorEmail,
                entity.AdministrateTax,
                entity.Lawyer,
                entity.LawyerData,
                entity.Observation));

    private static PropertyChargeTemplateDto ToChargeTemplateDto(PropertyChargeTemplate entity)
        => new(
            entity.Id,
            entity.Kind.ToString(),
            entity.Title,
            entity.DefaultAmount,
            entity.DueDay,
            entity.DefaultResponsibility.ToString(),
            entity.ProviderInformation,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAtUtc);

    private static string ComposeLegacyStatus(Property entity)
    {
        if (entity.OccupancyStatus == PropertyOccupancyStatus.OCCUPIED)
        {
            return "LEASED";
        }

        return entity.AssetState switch
        {
            PropertyAssetState.PREPARATION => "PREPARATION",
            PropertyAssetState.RENOVATION => "RENOVATION",
            PropertyAssetState.CONSTRUCTION => "CONSTRUCTION",
            PropertyAssetState.FOR_SALE => "FOR_SALE",
            PropertyAssetState.NEW => "NEW",
            _ => "AVAILABLE"
        };
    }

    private static decimal? ResolveCurrentBaseRent(Property entity)
        => entity.RentReferences
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => (decimal?)x.Amount)
            .FirstOrDefault();

    private static string ResolvePendencySeverity(DateTime openedAtUtc, int defaultSlaDays)
    {
        var elapsedDays = Math.Max(0, (int)(DateTime.UtcNow.Date - openedAtUtc.Date).TotalDays);
        var slaDays = Math.Max(1, defaultSlaDays);
        var percent = elapsedDays / (decimal)slaDays;

        return percent < 0.8m ? "ATTENTION" : percent <= 1m ? "URGENT" : "CRITICAL";
    }

    private static string NormalizeExpenseKind(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized switch
        {
            "AGUA" => "WATER",
            "LUZ" => "LIGHT",
            "GAS" => "GAS",
            "CONDOMINIO" => "CONDOMINIUM",
            _ => normalized
        };
    }

    private static bool MatchesTemplateKind(ChargeTemplateKind kind, string expenseTypeName)
    {
        var normalized = expenseTypeName.Trim().ToUpperInvariant();
        return kind switch
        {
            ChargeTemplateKind.WATER => normalized.Contains("AGUA"),
            ChargeTemplateKind.LIGHT => normalized.Contains("LUZ") || normalized.Contains("ENERG"),
            ChargeTemplateKind.GAS => normalized.Contains("GAS"),
            ChargeTemplateKind.CONDOMINIUM => normalized.Contains("CONDOM"),
            ChargeTemplateKind.IPTU => normalized.Contains("IPTU"),
            ChargeTemplateKind.EXTRA => !normalized.Contains("AGUA")
                && !normalized.Contains("LUZ")
                && !normalized.Contains("GAS")
                && !normalized.Contains("CONDOM")
                && !normalized.Contains("IPTU"),
            _ => false
        };
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? CombineNotes(string? primary, string? secondary)
    {
        var values = new[] { NormalizeNullable(primary), NormalizeNullable(secondary) }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        return values.Length == 0 ? null : string.Join(" | ", values);
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
