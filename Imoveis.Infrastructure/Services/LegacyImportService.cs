using Imoveis.Application.Abstractions.Services;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Legacy;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Imoveis.Infrastructure.Services;

public sealed class LegacyImportService : ILegacyImportService
{
    private readonly AppDbContext _dbContext;

    public LegacyImportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LegacyImportResultDto> ImportAsync(LegacyImportRequest request, CancellationToken cancellationToken)
    {
        var importedProperties = 0;
        var importedTenants = 0;
        var importedLeases = 0;
        var importedChargeTemplates = 0;
        var importedExpenses = 0;
        var importedHistoryEntries = 0;
        var importedAttachments = 0;
        var importedPendencies = 0;

        var propertyMap = new Dictionary<int, Property>();
        var leaseMap = new Dictionary<int, LeaseContract>();
        var pendencyTypeMap = await EnsurePendencyTypesAsync(request.PendencyAcronyms, cancellationToken);
        var expenseTypeMap = await EnsureExpenseTypesAsync(cancellationToken);

        foreach (var estate in request.Estates)
        {
            var status = MapLegacyBusinessStatus(estate.Status);
            var motivoOciosidade = MapLegacyIdleReason(estate.Status);

            var property = new Property
            {
                Code = $"LEGACY-{estate.Id}",
                Title = NormalizeOrFallback(estate.Nickname, $"Imovel {estate.Id}"),
                AddressLine1 = NormalizeOrFallback(estate.Address, "Endereco legado"),
                City = ExtractCity(estate.Address),
                State = "SP",
                ZipCode = "00000000",
                PropertyType = NormalizeOrFallback(estate.Type, "Nao informado"),
                Registration = NormalizeNullable(estate.Registration),
                Scripture = NormalizeNullable(estate.Scripture),
                RegistrationCertification = NormalizeNullable(estate.RegistrationCertification),
                NumOfRooms = estate.NumOfRooms,
                CleaningIncluded = estate.CleaningIncluded ?? false,
                Elevator = estate.Elevator ?? false,
                Garage = estate.Garage ?? false,
                Proprietary = NormalizeNullable(estate.Proprietary),
                Administrator = NormalizeNullable(estate.Administrator),
                AdministratorPhone = NormalizeNullable(estate.AdministratorPhone),
                AdministratorEmail = NormalizeNullable(estate.AdministratorEmail),
                AdministrateTax = NormalizeNullable(estate.AdministrateTax),
                Lawyer = NormalizeNullable(estate.Lawyer),
                LawyerData = NormalizeNullable(estate.LawyerData),
                Observation = NormalizeNullable(estate.Observation),
                UnoccupiedSince = estate.Unoccupied
            };

            PropertyStatusContract.Apply(property, status, motivoOciosidade);

            _dbContext.Properties.Add(property);
            propertyMap[estate.Id] = property;
            importedProperties++;

            importedChargeTemplates += AddChargeTemplates(property, estate);
            importedAttachments += AddAttachments(property, estate);

            if (!string.IsNullOrWhiteSpace(estate.Lessee))
            {
                var tenant = new Tenant
                {
                    Name = estate.Lessee!.Trim(),
                    DocumentNumber = $"LEGACY-{estate.Id}",
                    Email = $"legacy-tenant-{estate.Id}@import.local",
                    Phone = NormalizeOrFallback(estate.LesseePhone, "00000000000"),
                    IsActive = true
                };

                _dbContext.Tenants.Add(tenant);
                importedTenants++;

                var lease = new LeaseContract
                {
                    Property = property,
                    Tenant = tenant,
                    StartDate = estate.StartDate ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
                    EndDate = estate.EndDate,
                    MonthlyRent = estate.RentValue ?? 0m,
                    ContractWith = NormalizeNullable(estate.ContractWith),
                    PaymentDay = estate.PaymentDay,
                    PaymentLocation = NormalizeNullable(estate.PaymentLocation),
                    ReadjustmentIndex = NormalizeNullable(estate.ReadjustmentIndex),
                    ContractRegistration = NormalizeNullable(estate.ContractRegistration),
                    Insurance = NormalizeNullable(estate.Insurance),
                    SignatureRecognition = NormalizeNullable(estate.SignatureRecognition),
                    OptionalContactName = NormalizeNullable(estate.OptionalContactName),
                    OptionalContactPhone = NormalizeNullable(estate.OptionalContactNumber),
                    GuarantorName = NormalizeNullable(estate.Guarantor),
                    GuarantorDocument = NormalizeNullable(estate.GuarantorData),
                    GuarantorPhone = NormalizeNullable(estate.GuarantorNumber),
                    CleaningIncluded = estate.CleaningIncluded ?? false,
                    Status = property.OccupancyStatus == PropertyOccupancyStatus.OCCUPIED ? LeaseStatus.ACTIVE : LeaseStatus.ENDED,
                    Notes = "Importado do legado"
                };

                GenerateLegacyReceivables(lease);
                _dbContext.LeaseContracts.Add(lease);
                leaseMap[estate.Id] = lease;
                importedLeases++;
            }

            foreach (var history in request.Histories.Where(x => x.EstateId == estate.Id && !string.IsNullOrWhiteSpace(x.Data)))
            {
                _dbContext.PropertyHistoryEntries.Add(new PropertyHistoryEntry
                {
                    Property = property,
                    Content = history.Data!.Trim(),
                    OccurredAtUtc = history.CreatedAtUtc ?? DateTime.UtcNow
                });
                importedHistoryEntries++;
            }
        }

        foreach (var record in request.FinancialRecords)
        {
            if (!propertyMap.TryGetValue(record.EstateId, out var property))
            {
                continue;
            }

            if (leaseMap.TryGetValue(record.EstateId, out var lease))
            {
                UpsertReceivable(lease, record);
            }

            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["IPTU"], "IPTU", record.PropertyTaxIPTU, record.PropertyTaxIPTUValue, record.StatusPropertyTaxIPTU, record.PropertyTaxIPTUPerson, record.Observations);
            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["CONDOMINIO"], "Condominio", record.Condominium, record.CondominiumValue, record.StatusCondominium, record.CondominiumPerson, record.Observations);
            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["AGUA"], "Agua", record.Sabesp, record.SabespValue, record.StatusSabesp, record.SabespPerson, record.Observations);
            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["LUZ"], "Luz", record.Enel, record.EnelValue, record.StatusEnel, record.EnelPerson, record.Observations);
            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["GAS"], "Gas", record.Gas, record.GasValue, record.StatusGas, record.GasPerson, record.Observations);
            importedExpenses += ImportExpenseIfAny(property, expenseTypeMap["EXTRA"], "Extra", record.Extra, record.ExtraValue, record.ExtraStatus, record.ExtraPerson, record.Observations);
        }

        foreach (var pendencyState in request.PendencyStates)
        {
            if (!propertyMap.TryGetValue(pendencyState.IdState, out var property))
            {
                continue;
            }

            var code = pendencyState.PendencyAcronym.Trim().ToUpperInvariant();
            if (!pendencyTypeMap.TryGetValue(code, out var pendencyType))
            {
                continue;
            }

            _dbContext.PendencyItems.Add(new PendencyItem
            {
                Property = property,
                PendencyType = pendencyType,
                Title = $"Pendencia {pendencyState.PendencyAcronym}",
                Description = "Importada do legado",
                OpenedAtUtc = pendencyState.Date,
                DueAtUtc = pendencyState.Date,
                Status = PendencyStatus.OPEN
            });
            importedPendencies++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new LegacyImportResultDto(
            importedProperties,
            importedTenants,
            importedLeases,
            importedChargeTemplates,
            importedExpenses,
            importedHistoryEntries,
            importedAttachments,
            importedPendencies);
    }

    private async Task<Dictionary<string, PendencyType>> EnsurePendencyTypesAsync(IReadOnlyList<LegacyPendencyAcronymRecord> records, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PendencyTypes.ToListAsync(cancellationToken);
        var result = existing.ToDictionary(x => x.Code, x => x);

        foreach (var record in records)
        {
            var code = record.Acronym.Trim().ToUpperInvariant();
            if (result.ContainsKey(code))
            {
                continue;
            }

            var entity = new PendencyType
            {
                Code = code,
                Name = record.Acronym.Trim(),
                Description = NormalizeNullable(record.Description),
                DefaultSlaDays = 7
            };

            _dbContext.PendencyTypes.Add(entity);
            result[code] = entity;
        }

        return result;
    }

    private async Task<Dictionary<string, ExpenseType>> EnsureExpenseTypesAsync(CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ExpenseTypes.ToListAsync(cancellationToken);
        var result = existing.ToDictionary(x => NormalizeKey(x.Name), x => x);

        EnsureExpenseType(result, "IPTU", "IPTU", "TAX");
        EnsureExpenseType(result, "CONDOMINIO", "Condominio", "CONDOMINIUM");
        EnsureExpenseType(result, "AGUA", "Agua", "UTILITIES");
        EnsureExpenseType(result, "LUZ", "Luz", "UTILITIES");
        EnsureExpenseType(result, "GAS", "Gas", "UTILITIES");
        EnsureExpenseType(result, "EXTRA", "Extra", "MISC");

        return result;

        void EnsureExpenseType(Dictionary<string, ExpenseType> dictionary, string key, string name, string category)
        {
            if (dictionary.ContainsKey(key))
            {
                return;
            }

            var entity = new ExpenseType
            {
                Name = name,
                Category = category,
                IsFixedCost = key != "EXTRA"
            };

            _dbContext.ExpenseTypes.Add(entity);
            dictionary[key] = entity;
        }
    }

    private int AddChargeTemplates(Property property, LegacyEstateRecord estate)
    {
        var added = 0;
        added += AddChargeTemplateIfAny(property, ChargeTemplateKind.CONDOMINIUM, "Condominio", estate.Condominium, null, null);
        added += AddChargeTemplateIfAny(property, ChargeTemplateKind.IPTU, estate.IPTU ?? "IPTU", estate.TaxIPTU, null, estate.IPTU);
        added += AddChargeTemplateIfAny(property, ChargeTemplateKind.WATER, "Agua", estate.Water, estate.WaterInformation, null);
        added += AddChargeTemplateIfAny(property, ChargeTemplateKind.LIGHT, "Luz", estate.Light, estate.LightInformation, null);
        added += AddChargeTemplateIfAny(property, ChargeTemplateKind.GAS, "Gas", estate.Gas, estate.GasInformation, null);
        return added;
    }

    private int AddChargeTemplateIfAny(Property property, ChargeTemplateKind kind, string title, decimal? amount, string? providerInformation, string? notes)
    {
        if (!amount.HasValue || amount.Value <= 0)
        {
            return 0;
        }

        _dbContext.PropertyChargeTemplates.Add(new PropertyChargeTemplate
        {
            Property = property,
            Kind = kind,
            Title = title,
            DefaultAmount = amount.Value,
            DefaultResponsibility = ChargeResponsibility.OWNER,
            ProviderInformation = NormalizeNullable(providerInformation),
            Notes = NormalizeNullable(notes),
            IsActive = true
        });

        return 1;
    }

    private int AddAttachments(Property property, LegacyEstateRecord estate)
    {
        var added = 0;

        if (!string.IsNullOrWhiteSpace(estate.BeforePhoto))
        {
            _dbContext.PropertyAttachments.Add(new PropertyAttachment
            {
                Property = property,
                Category = "PHOTO_BEFORE_ENTRY",
                Title = "Foto antes da entrada",
                ResourceLocation = estate.BeforePhoto!.Trim()
            });
            added++;
        }

        if (!string.IsNullOrWhiteSpace(estate.AfterPhoto))
        {
            _dbContext.PropertyAttachments.Add(new PropertyAttachment
            {
                Property = property,
                Category = "PHOTO_BEFORE_EXIT",
                Title = "Foto antes da saida",
                ResourceLocation = estate.AfterPhoto!.Trim()
            });
            added++;
        }

        return added;
    }

    private void GenerateLegacyReceivables(LeaseContract lease)
    {
        var end = lease.EndDate ?? lease.StartDate.AddMonths(11);
        var competence = new DateOnly(lease.StartDate.Year, lease.StartDate.Month, 1);
        var lastCompetence = new DateOnly(end.Year, end.Month, 1);

        while (competence <= lastCompetence)
        {
            var dueDay = Math.Clamp(lease.PaymentDay ?? 5, 1, DateTime.DaysInMonth(competence.Year, competence.Month));
            lease.ReceivableInstallments.Add(new LeaseReceivableInstallment
            {
                CompetenceDate = competence,
                DueDate = new DateOnly(competence.Year, competence.Month, dueDay),
                ExpectedAmount = lease.MonthlyRent,
                Status = lease.Status == LeaseStatus.ACTIVE ? ReceivableStatus.OPEN : ReceivableStatus.CANCELED
            });
            competence = competence.AddMonths(1);
        }
    }

    private void UpsertReceivable(LeaseContract lease, LegacyFinancialRecord record)
    {
        var competence = new DateOnly(record.Year, record.Month, 1);
        var installment = lease.ReceivableInstallments.FirstOrDefault(x => x.CompetenceDate == competence);

        if (installment is null)
        {
            installment = new LeaseReceivableInstallment
            {
                LeaseContract = lease,
                CompetenceDate = competence,
                DueDate = record.Rent ?? new DateOnly(record.Year, record.Month, Math.Clamp(lease.PaymentDay ?? 5, 1, DateTime.DaysInMonth(record.Year, record.Month))),
                ExpectedAmount = record.RentValue ?? lease.MonthlyRent,
                Status = MapReceivableStatus(record.StatusRent),
                PaidAmount = ShouldMarkAsPaid(record.StatusRent) ? record.RentValue : null,
                PaidAtUtc = record.Rent?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                PaidBy = NormalizeNullable(record.RentPerson),
                Notes = NormalizeNullable(record.Observations)
            };

            _dbContext.LeaseReceivableInstallments.Add(installment);
            lease.ReceivableInstallments.Add(installment);
            return;
        }

        installment.DueDate = record.Rent ?? installment.DueDate;
        installment.ExpectedAmount = record.RentValue ?? installment.ExpectedAmount;
        installment.Status = MapReceivableStatus(record.StatusRent);
        installment.PaidAmount = ShouldMarkAsPaid(record.StatusRent) ? (record.RentValue ?? installment.ExpectedAmount) : null;
        installment.PaidAtUtc = record.Rent?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        installment.PaidBy = NormalizeNullable(record.RentPerson);
        installment.Notes = NormalizeNullable(record.Observations);
    }

    private int ImportExpenseIfAny(Property property, ExpenseType expenseType, string label, DateOnly? dueDate, decimal? amount, string? status, string? paidBy, string? observations)
    {
        if (!amount.HasValue || amount.Value <= 0)
        {
            return 0;
        }

        var expense = new PropertyExpense
        {
            Property = property,
            ExpenseType = expenseType,
            Description = $"{label} importado do legado",
            Frequency = ExpenseFrequency.ONE_TIME,
            DueDate = dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date),
            TotalAmount = amount.Value,
            InstallmentsCount = 1,
            IsRecurring = false,
            Status = MapExpenseStatus(status),
            Notes = NormalizeNullable(observations)
        };

        expense.Installments.Add(new ExpenseInstallment
        {
            InstallmentNumber = 1,
            DueDate = expense.DueDate,
            Amount = amount.Value,
            Status = MapExpenseStatus(status),
            PaidAmount = ShouldMarkAsPaid(status) ? amount.Value : null,
            PaidAtUtc = dueDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            PaidBy = NormalizeNullable(paidBy),
            Notes = NormalizeNullable(observations)
        });

        _dbContext.PropertyExpenses.Add(expense);
        return 1;
    }

    private static string MapLegacyBusinessStatus(string? status)
    {
        var normalized = NormalizeKey(status);
        if (normalized.Contains("ALUG") || normalized.Contains("LOC"))
        {
            return PropertyStatusContract.Alugado;
        }

        if (normalized.Contains("VENDA"))
        {
            return PropertyStatusContract.A_Venda;
        }

        if (normalized.Contains("INATIV"))
        {
            return PropertyStatusContract.Inativo;
        }

        if (normalized.Contains("REFOR") || normalized.Contains("RESCIS") || normalized.Contains("JURID"))
        {
            return PropertyStatusContract.Ocioso;
        }

        if (normalized.Contains("CONSTRU") || normalized.Contains("PREPAR") || normalized.Contains("DEMAND"))
        {
            return PropertyStatusContract.Demandas;
        }

        return PropertyStatusContract.Disponivel;
    }

    private static string? MapLegacyIdleReason(string? status)
    {
        var normalized = NormalizeKey(status);
        if (normalized.Contains("REFOR"))
        {
            return PropertyStatusContract.Reforma;
        }

        if (normalized.Contains("RESCIS"))
        {
            return PropertyStatusContract.Rescisao;
        }

        if (normalized.Contains("JURID"))
        {
            return PropertyStatusContract.PendenciaJuridica;
        }

        return null;
    }

    private static ReceivableStatus MapReceivableStatus(string? status)
    {
        return ShouldMarkAsPaid(status)
            ? ReceivableStatus.RECEIVED
            : NormalizeKey(status).Contains("ATRAS") ? ReceivableStatus.OVERDUE : ReceivableStatus.OPEN;
    }

    private static ExpenseStatus MapExpenseStatus(string? status)
    {
        return ShouldMarkAsPaid(status)
            ? ExpenseStatus.PAID
            : NormalizeKey(status).Contains("ATRAS") ? ExpenseStatus.OVERDUE : ExpenseStatus.OPEN;
    }

    private static bool ShouldMarkAsPaid(string? status)
    {
        var normalized = NormalizeKey(status);
        return normalized.Contains("PAGO") && !normalized.Contains("NAO");
    }

    private static string NormalizeKey(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("Ã", "A")
            .Replace("Á", "A")
            .Replace("Â", "A")
            .Replace("É", "E")
            .Replace("Ê", "E")
            .Replace("Í", "I")
            .Replace("Ó", "O")
            .Replace("Ô", "O")
            .Replace("Õ", "O")
            .Replace("Ú", "U")
            .Replace("Ç", "C");
    }

    private static string ExtractCity(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return "Sao Paulo";
        }

        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : "Sao Paulo";
    }

    private static string NormalizeOrFallback(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
