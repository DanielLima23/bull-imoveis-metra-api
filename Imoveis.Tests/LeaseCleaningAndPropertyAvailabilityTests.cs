using Imoveis.Api.Contracts;
using Imoveis.Api.Controllers;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Leases;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class LeaseCleaningAndPropertyAvailabilityTests
{
    [Fact]
    public async Task LeaseCrudPersistsCleaningIncludedAndPropertyUnoccupiedSinceIsDerived()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;
        Guid tenantId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Apartamento Centro");
            tenantId = await SeedTenantAsync(setupDbContext, "Joao Locatario", "12345678901");
        }

        LeaseDto createdLease;
        await using (var createDbContext = CreateDbContext(connection))
        {
            var leaseController = CreateLeaseController(createDbContext);
            createdLease = ExtractData(await leaseController.Create(
                new LeaseCreateRequest(
                    propertyId,
                    tenantId,
                    new DateOnly(2026, 3, 13),
                    null,
                    2500m,
                    1000m,
                    "Joao Locatario",
                    5,
                    "Imobiliaria",
                    "IPCA",
                    "REG-001",
                    "Seguro base",
                    "Reconhecimento",
                    "Contato adicional",
                    "11999990001",
                    "Carlos Fiador",
                    "12345678902",
                    "11999990002",
                    true,
                    "Contrato inicial"),
                CancellationToken.None));
        }

        Assert.True(createdLease.CleaningIncluded);

        PagedResult<LeaseDto> queriedLeases;
        await using (var leaseQueryDbContext = CreateDbContext(connection))
        {
            var leaseController = CreateLeaseController(leaseQueryDbContext);
            queriedLeases = ExtractObjectData<PagedResult<LeaseDto>>(await leaseController.Query(
                propertyId,
                tenantId,
                null,
                1,
                20,
                CancellationToken.None));
        }

        Assert.Single(queriedLeases.Items);
        Assert.True(queriedLeases.Items[0].CleaningIncluded);

        PropertyDto occupiedProperty;
        await using (var occupiedPropertyDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(occupiedPropertyDbContext);
            occupiedProperty = ExtractData(await propertyController.GetById(propertyId, CancellationToken.None));
        }

        Assert.Null(occupiedProperty.Characteristics.UnoccupiedSince);

        LeaseDto updatedLease;
        await using (var updateDbContext = CreateDbContext(connection))
        {
            var leaseController = CreateLeaseController(updateDbContext);
            updatedLease = ExtractData(await leaseController.Update(
                createdLease.Id,
                new LeaseUpdateRequest(
                    createdLease.StartDate,
                    new DateOnly(2026, 4, 15),
                    2600m,
                    1000m,
                    LeaseStatus.ENDED.ToString(),
                    createdLease.ContractWith,
                    createdLease.PaymentDay,
                    createdLease.PaymentLocation,
                    createdLease.ReadjustmentIndex,
                    createdLease.ContractRegistration,
                    createdLease.Insurance,
                    createdLease.SignatureRecognition,
                    createdLease.OptionalContactName,
                    createdLease.OptionalContactPhone,
                    createdLease.GuarantorName,
                    createdLease.GuarantorDocument,
                    createdLease.GuarantorPhone,
                    false,
                    "Contrato encerrado"),
                CancellationToken.None));
        }

        Assert.False(updatedLease.CleaningIncluded);
        Assert.Equal(new DateOnly(2026, 4, 15), updatedLease.EndDate);

        LeaseDto leaseById;
        await using (var leaseByIdDbContext = CreateDbContext(connection))
        {
            var leaseController = CreateLeaseController(leaseByIdDbContext);
            leaseById = ExtractData(await leaseController.GetById(createdLease.Id, CancellationToken.None));
        }

        Assert.False(leaseById.CleaningIncluded);

        await using (var endedLeaseQueryDbContext = CreateDbContext(connection))
        {
            var leaseController = CreateLeaseController(endedLeaseQueryDbContext);
            queriedLeases = ExtractObjectData<PagedResult<LeaseDto>>(await leaseController.Query(
                propertyId,
                tenantId,
                LeaseStatus.ENDED.ToString(),
                1,
                20,
                CancellationToken.None));
        }

        Assert.Single(queriedLeases.Items);
        Assert.False(queriedLeases.Items[0].CleaningIncluded);

        PropertyDto propertyById;
        await using (var propertyByIdDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(propertyByIdDbContext);
            propertyById = ExtractData(await propertyController.GetById(propertyId, CancellationToken.None));
        }

        Assert.Equal(new DateOnly(2026, 4, 15), propertyById.Characteristics.UnoccupiedSince);

        PropertyDetailDto propertyDetail;
        await using (var propertyDetailDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(propertyDetailDbContext);
            propertyDetail = ExtractData(await propertyController.GetDetail(propertyId, CancellationToken.None));
        }

        Assert.Equal(new DateOnly(2026, 4, 15), propertyDetail.Property.Characteristics.UnoccupiedSince);

        PagedResult<PropertyDto> propertyQuery;
        await using (var propertyQueryDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(propertyQueryDbContext);
            propertyQuery = ExtractObjectData<PagedResult<PropertyDto>>(await propertyController.Query(
                null,
                PropertyStatusContract.Disponivel,
                null,
                null,
                null,
                null,
                null,
                1,
                20,
                CancellationToken.None));
        }

        Assert.Single(propertyQuery.Items);
        Assert.Equal(new DateOnly(2026, 4, 15), propertyQuery.Items[0].Characteristics.UnoccupiedSince);
    }

    [Fact]
    public async Task PropertyWithoutEndedLeaseReturnsNullUnoccupiedSince()
    {
        await using var connection = CreateOpenConnection();
        Guid propertyId;

        await using (var setupDbContext = CreateDbContext(connection))
        {
            propertyId = await SeedPropertyAsync(setupDbContext, "Casa sem historico");
        }

        PropertyDto propertyById;
        await using (var propertyByIdDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(propertyByIdDbContext);
            propertyById = ExtractData(await propertyController.GetById(propertyId, CancellationToken.None));
        }

        Assert.Null(propertyById.Characteristics.UnoccupiedSince);

        PropertyDetailDto propertyDetail;
        await using (var propertyDetailDbContext = CreateDbContext(connection))
        {
            var propertyController = CreatePropertyController(propertyDetailDbContext);
            propertyDetail = ExtractData(await propertyController.GetDetail(propertyId, CancellationToken.None));
        }

        Assert.Null(propertyDetail.Property.Characteristics.UnoccupiedSince);
    }

    private static async Task<Guid> SeedPropertyAsync(AppDbContext dbContext, string title)
    {
        var service = new PropertyService(dbContext);
        var created = await service.CreateAsync(
            new PropertyCreateRequest(
                new PropertyIdentitySectionRequest(title, "Rua A, 10 (Centro)", "Sao Paulo", "SP", "01001000", "Apartamento", PropertyStatusContract.Disponivel, null),
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
    {
        return new ImoveisController(new PropertyService(dbContext))
        {
            ControllerContext = BuildControllerContext()
        };
    }

    private static LocacoesController CreateLeaseController(AppDbContext dbContext)
    {
        return new LocacoesController(new LeaseService(dbContext))
        {
            ControllerContext = BuildControllerContext()
        };
    }

    private static ControllerContext BuildControllerContext()
        => new()
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString("N")
            }
        };

    private static T ExtractData<T>(ActionResult<ApiResponse<T>> actionResult)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
        var envelope = Assert.IsType<ApiResponse<T>>(objectResult.Value);
        Assert.True(envelope.Success);
        return Assert.IsType<T>(envelope.Data);
    }

    private static T ExtractObjectData<T>(ActionResult<ApiResponse<object>> actionResult)
    {
        var objectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var envelope = Assert.IsType<ApiResponse<object>>(objectResult.Value);
        Assert.True(envelope.Success);
        return Assert.IsType<T>(envelope.Data);
    }
}
