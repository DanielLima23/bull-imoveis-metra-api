using Imoveis.Api.Contracts;
using Imoveis.Api.Controllers;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Leases;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class PropertyLeaseGuardrailsTests
{
    [Fact]
    public async Task UpdatingPropertyToLeasedWithoutActiveLeaseFails()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento sem locacao");
        }

        await using var dbContext = CreateDbContext(connection);
        var service = new PropertyService(dbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(
                propertyId,
                new PropertyStatusUpdateRequest("LEASED", null),
                CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(PropertyLeaseErrorCodes.PropertyRequiresActiveLease, exception.Code);
    }

    [Fact]
    public async Task UpdatingPropertyToLeasedWithActiveLeaseWorks()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;
        Guid tenantId;
        LeaseDto activeLease;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento com locacao");
            tenantId = await SeedTenantAsync(setupDbContext, "Joao Locatario", "12345678901");
        }

        await using (var leaseDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(leaseDbContext);
            activeLease = await leaseService.CreateAsync(
                BuildLeaseCreateRequest(propertyId, tenantId, 2500m),
                CancellationToken.None);
        }

        PropertyDto updated;
        await using (var propertyDbContext = CreateDbContext(connection))
        {
            var controller = CreatePropertyController(propertyDbContext);
            updated = ExtractData(await controller.Update(
                propertyId,
                BuildPropertyUpdateRequest("LEASED", null),
                CancellationToken.None));
        }

        Assert.Equal(PropertyStatusContract.Alugado, updated.Status);
        Assert.True(updated.HasActiveLease);
        Assert.Equal(activeLease.Id, updated.ActiveLeaseId);
    }

    [Fact]
    public async Task UpdatingLeasedPropertyToAvailableWithActiveLeaseFails()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;
        Guid tenantId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento ocupado");
            tenantId = await SeedTenantAsync(setupDbContext, "Maria Locataria", "12345678902");
        }

        await using (var leaseDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(leaseDbContext);
            await leaseService.CreateAsync(
                BuildLeaseCreateRequest(propertyId, tenantId, 3200m),
                CancellationToken.None);
        }

        await using var propertyDbContext = CreateDbContext(connection);
        var service = new PropertyService(propertyDbContext);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(
                propertyId,
                new PropertyStatusUpdateRequest("AVAILABLE", null),
                CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal(PropertyLeaseErrorCodes.PropertyHasActiveLease, exception.Code);
    }

    [Fact]
    public async Task EndingActiveLeaseThenChangingPropertyStatusWorks()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;
        Guid tenantId;
        Guid leaseId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento para encerrar");
            tenantId = await SeedTenantAsync(setupDbContext, "Carlos Locatario", "12345678903");
        }

        await using (var leaseDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(leaseDbContext);
            leaseId = (await leaseService.CreateAsync(
                BuildLeaseCreateRequest(propertyId, tenantId, 2800m),
                CancellationToken.None)).Id;
        }

        await using (var closeDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(closeDbContext);
            await leaseService.CloseAsync(
                leaseId,
                new LeaseCloseRequest(new DateOnly(2026, 4, 15)),
                CancellationToken.None);
        }

        await using var propertyDbContext = CreateDbContext(connection);
        var service = new PropertyService(propertyDbContext);
        var updated = await service.UpdateStatusAsync(
            propertyId,
            new PropertyStatusUpdateRequest("INACTIVE", null),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal(PropertyStatusContract.Inativo, updated!.Status);
        Assert.False(updated.HasActiveLease);
        Assert.Null(updated.ActiveLeaseId);
    }

    [Fact]
    public async Task CreatingOrActivatingSecondActiveLeaseForSamePropertyFails()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;
        Guid firstTenantId;
        Guid secondTenantId;
        Guid draftLeaseId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento com restricao");
            firstTenantId = await SeedTenantAsync(setupDbContext, "Primeiro Locatario", "12345678904");
            secondTenantId = await SeedTenantAsync(setupDbContext, "Segundo Locatario", "12345678905");
        }

        await using (var firstLeaseDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(firstLeaseDbContext);
            await leaseService.CreateAsync(
                BuildLeaseCreateRequest(propertyId, firstTenantId, 3000m),
                CancellationToken.None);
        }

        await using (var createConflictDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(createConflictDbContext);

            var exception = await Assert.ThrowsAsync<AppException>(() =>
                leaseService.CreateAsync(
                    BuildLeaseCreateRequest(propertyId, secondTenantId, 3100m),
                    CancellationToken.None));

            Assert.Equal(409, exception.StatusCode);
            Assert.Equal(PropertyLeaseErrorCodes.PropertyAlreadyHasActiveLease, exception.Code);
        }

        await using (var draftLeaseDbContext = CreateDbContext(connection))
        {
            var draftLease = new LeaseContract
            {
                PropertyId = propertyId,
                TenantId = secondTenantId,
                StartDate = new DateOnly(2026, 5, 1),
                EndDate = null,
                MonthlyRent = 3100m,
                DepositAmount = 800m,
                Status = LeaseStatus.DRAFT,
                PaymentDay = 5,
                Notes = "Rascunho"
            };

            draftLeaseDbContext.LeaseContracts.Add(draftLease);
            await draftLeaseDbContext.SaveChangesAsync();
            draftLeaseId = draftLease.Id;
        }

        await using (var activateConflictDbContext = CreateDbContext(connection))
        {
            var leaseService = new LeaseService(activateConflictDbContext);

            var exception = await Assert.ThrowsAsync<AppException>(() =>
                leaseService.UpdateAsync(
                    draftLeaseId,
                    BuildLeaseUpdateRequest(LeaseStatus.ACTIVE),
                    CancellationToken.None));

            Assert.Equal(409, exception.StatusCode);
            Assert.Equal(PropertyLeaseErrorCodes.PropertyAlreadyHasActiveLease, exception.Code);
        }
    }

    private static PropertyUpdateRequest BuildPropertyUpdateRequest(string status, string? idleReason)
        => new(
            new PropertyIdentitySectionRequest(
                "Apartamento atualizado",
                "Rua A, 20",
                "Sao Paulo",
                "SP",
                "01001000",
                "Apartamento",
                status,
                idleReason),
            new PropertyDocumentationSectionRequest("REG-1", null, null),
            new PropertyCharacteristicsSectionRequest(3, true, true),
            new PropertyAdministrationSectionRequest(
                "Maria",
                null,
                "Carlos",
                null,
                "10%",
                null,
                null,
                "Observacao"));

    private static LeaseCreateRequest BuildLeaseCreateRequest(Guid propertyId, Guid tenantId, decimal monthlyRent)
        => new(
            propertyId,
            tenantId,
            new DateOnly(2026, 3, 13),
            null,
            monthlyRent,
            1000m,
            "Locatario",
            5,
            "Imobiliaria",
            "IPCA",
            "REG-LOC",
            "Seguro",
            "Cartorio",
            null,
            null,
            null,
            null,
            null,
            false,
            "Contrato");

    private static LeaseUpdateRequest BuildLeaseUpdateRequest(LeaseStatus status)
        => new(
            new DateOnly(2026, 5, 1),
            null,
            3100m,
            800m,
            status.ToString(),
            "Locatario",
            5,
            "Imobiliaria",
            "IPCA",
            "REG-LOC",
            "Seguro",
            "Cartorio",
            null,
            null,
            null,
            null,
            null,
            false,
            "Contrato");

    private static async Task<Guid> SeedPropertyAsync(AppDbContext dbContext, string title)
    {
        var service = new PropertyService(dbContext);
        var created = await service.CreateAsync(
            new PropertyCreateRequest(
                new PropertyIdentitySectionRequest(title, "Rua A, 10", "Sao Paulo", "SP", "01001000", "Apartamento", "AVAILABLE", null),
                new PropertyDocumentationSectionRequest(null, null, null),
                new PropertyCharacteristicsSectionRequest(2, false, true),
                new PropertyAdministrationSectionRequest("Maria", null, "Carlos", null, "10%", null, null, null),
                null,
                null),
            CancellationToken.None);

        return created.Id;
    }

    private static async Task<Guid> SeedTenantAsync(AppDbContext dbContext, string name, string documentNumber)
    {
        var tenant = new Tenant
        {
            Name = name,
            DocumentNumber = documentNumber,
            Email = $"{documentNumber}@teste.com",
            Phone = "11999990000",
            IsActive = true
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();
        return tenant.Id;
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private static ImoveisController CreatePropertyController(AppDbContext dbContext)
        => new(new PropertyService(dbContext))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = Guid.NewGuid().ToString("N")
                }
            }
        };

    private static T ExtractData<T>(ActionResult<ApiResponse<T>> actionResult)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var envelope = Assert.IsType<ApiResponse<T>>(objectResult.Value);
        Assert.True(envelope.Success);
        return Assert.IsType<T>(envelope.Data);
    }
}
