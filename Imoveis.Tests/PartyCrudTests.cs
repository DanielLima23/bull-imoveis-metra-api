using Imoveis.Api.Contracts;
using Imoveis.Api.Controllers;
using Imoveis.Application.Common;
using Imoveis.Application.Contracts.Parties;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class PartyCrudTests
{
    [Fact]
    public async Task CreateQueryGetAndUpdatePartyWorkWithSupportedKinds()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var created = ExtractData(await controller.Create(
            new PartyCreateRequest("PROPRIETARIO", "Jose da Silva", "12345678900", "jose@teste.com", "11999990000", "Contato principal"),
            CancellationToken.None));

        Assert.Equal("PROPRIETARIO", created.Kind);
        Assert.Equal("Jose da Silva", created.Name);
        Assert.True(created.IsActive);

        var byId = ExtractData(await controller.GetById(created.Id, CancellationToken.None));
        Assert.Equal(created.Id, byId.Id);
        Assert.Equal("PROPRIETARIO", byId.Kind);

        var query = ExtractObjectData<PagedResult<PartyDto>>(await controller.Query(
            search: "Jose",
            kind: "PROPRIETARIO",
            active: true,
            page: 1,
            pageSize: 20,
            cancellationToken: CancellationToken.None));

        Assert.Single(query.Items);
        Assert.Equal(created.Id, query.Items[0].Id);

        var updated = ExtractData(await controller.Update(
            created.Id,
            new PartyUpdateRequest("PRESTADOR_DE_SERVICO", "Empresa XYZ", "12345678000199", "contato@empresa.com", "1133334444", "Pessoa juridica", true),
            CancellationToken.None));

        Assert.Equal("PRESTADOR_DE_SERVICO", updated.Kind);
        Assert.Equal("Empresa XYZ", updated.Name);
        Assert.Equal("12345678000199", updated.DocumentNumber);
    }

    [Fact]
    public async Task CreatePartyAcceptsLegacyRoleAliasAndNormalizesResponse()
    {
        await using var dbContext = CreateDbContext();
        var service = new PartyService(dbContext);

        var created = await service.CreateAsync(
            new PartyCreateRequest("OWNER", "Jose da Silva", "12345678900", "jose@teste.com", "11999990000", null),
            CancellationToken.None);

        Assert.Equal("PROPRIETARIO", created.Kind);
    }

    [Theory]
    [InlineData("OWNER", "PROPRIETARIO")]
    [InlineData("ADMINISTRATOR", "ADMINISTRADOR")]
    [InlineData("GUARANTOR", "FIADOR")]
    [InlineData("LAWYER", "ADVOGADO")]
    [InlineData("BROKER", "CORRETOR")]
    [InlineData("LEGAL_REPRESENTATIVE", "REPRESENTANTE_LEGAL")]
    [InlineData("OTHER", "OUTRO")]
    public void ParseStoredValueMapsLegacyCodes(string legacyValue, string expectedCode)
    {
        var parsed = PartyKindContract.ParseStoredValue(legacyValue);

        Assert.Equal(expectedCode, PartyKindContract.GetCode(parsed));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"parties-tests-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    private static PessoasController CreateController(AppDbContext dbContext)
    {
        var controller = new PessoasController(new PartyService(dbContext))
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
