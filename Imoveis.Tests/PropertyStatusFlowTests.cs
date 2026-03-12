using System.Text.Json;
using Imoveis.Api.Contracts;
using Imoveis.Api.Controllers;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Properties;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class PropertyStatusFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task CrudAndStatusEndpointPersistUpdatedStatus()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var created = ExtractData(await controller.Create(BuildCreateRequest(PropertyStatusContract.Disponivel, null), CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Disponivel, created.Status);
        Assert.Null(created.MotivoOciosidade);

        var updateRequest = JsonSerializer.Deserialize<PropertyUpdateRequest>(
            $$"""
            {
              "identity": {
                "title": "Apartamento Centro Atualizado",
                "addressLine1": "Rua A, 20 (Centro)",
                "city": "Sao Paulo",
                "state": "SP",
                "zipCode": "01001000",
                "propertyType": "Apartamento",
                "status": "{{PropertyStatusContract.Ocioso}}",
                "idleReason": "RENOVATION"
              },
              "documentation": {
                "registration": "RG-1",
                "scripture": null,
                "registrationCertification": null
              },
              "characteristics": {
                "numOfRooms": 3,
                "cleaningIncluded": false,
                "elevator": true,
                "garage": true,
                "unoccupiedSince": null
              },
              "administration": {
                "proprietary": "Maria",
                "administrator": "Carlos",
                "administratorPhone": null,
                "administratorEmail": null,
                "administrateTax": null,
                "lawyer": null,
                "lawyerData": null,
                "observation": "Atualizado"
              }
            }
            """,
            JsonOptions);

        Assert.NotNull(updateRequest);
        Assert.Equal("RENOVATION", updateRequest.Identity.ResolveMotivoOciosidade());

        var updated = ExtractData(await controller.Update(created.Id, updateRequest, CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Ocioso, updated.Status);
        Assert.Equal(PropertyStatusContract.Reforma, updated.MotivoOciosidade);

        var byId = ExtractData(await controller.GetById(created.Id, CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Ocioso, byId.Status);
        Assert.Equal(PropertyStatusContract.Reforma, byId.MotivoOciosidade);

        var detail = ExtractData(await controller.GetDetail(created.Id, CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Ocioso, detail.Property.Status);
        Assert.Equal(PropertyStatusContract.Reforma, detail.Property.MotivoOciosidade);

        var query = ExtractObjectData<PagedResult<PropertyDto>>(await controller.Query(
            search: null,
            status: PropertyStatusContract.Ocioso,
            motivoOciosidade: PropertyStatusContract.Reforma,
            propertyType: null,
            city: null,
            proprietary: null,
            administrator: null,
            page: 1,
            pageSize: 20,
            cancellationToken: CancellationToken.None));

        Assert.Single(query.Items);
        Assert.Equal(created.Id, query.Items[0].Id);

        var statusRequest = JsonSerializer.Deserialize<PropertyStatusUpdateRequest>(
            """
            {
              "status": "LEASED",
              "idleReason": null
            }
            """,
            JsonOptions);

        Assert.NotNull(statusRequest);
        Assert.Null(statusRequest.ResolveMotivoOciosidade());

        var patched = ExtractData(await controller.UpdateStatus(created.Id, statusRequest, CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Alugado, patched.Status);
        Assert.Null(patched.MotivoOciosidade);

        var byIdAfterPatch = ExtractData(await controller.GetById(created.Id, CancellationToken.None));
        Assert.Equal(PropertyStatusContract.Alugado, byIdAfterPatch.Status);
        Assert.Null(byIdAfterPatch.MotivoOciosidade);
    }

    [Fact]
    public async Task StatusUpdateRejectsOciosoWithoutReason()
    {
        await using var dbContext = CreateDbContext();
        var service = new PropertyService(dbContext);

        var created = await service.CreateAsync(BuildCreateRequest(PropertyStatusContract.Disponivel, null), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.UpdateStatusAsync(created.Id, new PropertyStatusUpdateRequest(PropertyStatusContract.Ocioso, null), CancellationToken.None));

        Assert.Equal("validation_error", exception.Code);
    }

    private static PropertyCreateRequest BuildCreateRequest(string status, string? motivoOciosidade)
        => new(
            new PropertyIdentitySectionRequest(
                "Apartamento Centro",
                "Rua A, 10 (Centro)",
                "Sao Paulo",
                "SP",
                "01001000",
                "Apartamento",
                status,
                motivoOciosidade),
            new PropertyDocumentationSectionRequest(null, null, null),
            new PropertyCharacteristicsSectionRequest(2, false, false, true, null),
            new PropertyAdministrationSectionRequest(
                "Maria",
                "Carlos",
                null,
                null,
                null,
                null,
                null,
                null),
            null,
            null);

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"imoveis-tests-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static ImoveisController CreateController(AppDbContext dbContext)
    {
        var controller = new ImoveisController(new PropertyService(dbContext))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = Guid.NewGuid().ToString("N")
                }
            }
        };

        return controller;
    }

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
