using Imoveis.Api.Contracts;
using Imoveis.Api.Controllers;
using Imoveis.Application.Contracts.Settings;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Imoveis.Tests;

public sealed class SystemSettingsGuidedFlowsTests
{
    [Fact]
    public async Task GuidedFlowsFlagIsSavedAndReturnedByPublicAndPrivateEndpoints()
    {
        await using var connection = CreateOpenConnection();

        SystemSettingsDto initialPublicSettings;
        await using (var initialDbContext = CreateDbContext(connection))
        {
            var controller = CreateController(initialDbContext);
            initialPublicSettings = ExtractData(await controller.GetPublic(CancellationToken.None));
        }

        Assert.False(initialPublicSettings.EnableGuidedFlows);

        var request = new SystemSettingsUpdateRequest(
            "Bull Imoveis",
            "BULL",
            "AURORA_LIGHT",
            true)
        {
            EnableGuidedFlows = true
        };

        SystemSettingsDto updated;
        await using (var updateDbContext = CreateDbContext(connection))
        {
            var controller = CreateController(updateDbContext);
            updated = ExtractData(await controller.Update(request, CancellationToken.None));
        }

        Assert.True(updated.EnableGuidedFlows);

        SystemSettingsDto privateSettings;
        SystemSettingsDto publicSettings;
        await using (var readDbContext = CreateDbContext(connection))
        {
            var controller = CreateController(readDbContext);
            privateSettings = ExtractData(await controller.Get(CancellationToken.None));
            publicSettings = ExtractData(await controller.GetPublic(CancellationToken.None));
        }

        Assert.True(privateSettings.EnableGuidedFlows);
        Assert.True(publicSettings.EnableGuidedFlows);
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

    private static ConfiguracoesController CreateController(AppDbContext dbContext)
        => new(new SystemSettingsService(dbContext))
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
