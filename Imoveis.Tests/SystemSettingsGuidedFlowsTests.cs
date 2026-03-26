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

        Assert.Equal("SAND_LIGHT", initialPublicSettings.ThemePreset);
        Assert.False(initialPublicSettings.EnableGuidedFlows);

        var request = new SystemSettingsUpdateRequest(
            "Bull Imoveis",
            "BULL",
            "SAND_LIGHT",
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

    [Fact]
    public async Task LegacyAuroraDefaultIsConvertedToSandLightButCustomizedThemeRemainsUntouched()
    {
        await using var connection = CreateOpenConnection();

        await using (var seedDbContext = CreateDbContext(connection))
        {
            seedDbContext.SystemSettings.AddRange(
                new Imoveis.Domain.Entities.SystemSettings
                {
                    BrandName = "Bull Imoveis",
                    BrandShortName = "BULL",
                    ThemePreset = "AURORA_LIGHT",
                    PrimaryColor = "#1176EE",
                    SecondaryColor = "#0A58BA",
                    AccentColor = "#06B6D4",
                    EnableAnimations = true
                },
                new Imoveis.Domain.Entities.SystemSettings
                {
                    BrandName = "Bull Imoveis",
                    BrandShortName = "BULL",
                    ThemePreset = "AURORA_LIGHT",
                    PrimaryColor = "#2255AA",
                    SecondaryColor = "#1D3C7A",
                    AccentColor = "#5BB4FF",
                    EnableAnimations = true
                });

            await seedDbContext.SaveChangesAsync();
        }

        SystemSettingsDto latestSettings;
        await using (var dbContext = CreateDbContext(connection))
        {
            var controller = CreateController(dbContext);
            latestSettings = ExtractData(await controller.Get(CancellationToken.None));
        }

        Assert.Equal("AURORA_LIGHT", latestSettings.ThemePreset);
        Assert.Equal("#2255AA", latestSettings.PrimaryColor);
        Assert.Equal("#1D3C7A", latestSettings.SecondaryColor);
        Assert.Equal("#5BB4FF", latestSettings.AccentColor);

        Imoveis.Domain.Entities.SystemSettings[] persisted;
        await using (var assertDbContext = CreateDbContext(connection))
        {
            persisted = await assertDbContext.SystemSettings.OrderBy(x => x.CreatedAtUtc).ToArrayAsync();
        }

        Assert.Equal("SAND_LIGHT", persisted[0].ThemePreset);
        Assert.Equal("#8F6A3A", persisted[0].PrimaryColor);
        Assert.Equal("#5E4525", persisted[0].SecondaryColor);
        Assert.Equal("#C69A5D", persisted[0].AccentColor);

        Assert.Equal("AURORA_LIGHT", persisted[1].ThemePreset);
        Assert.Equal("#2255AA", persisted[1].PrimaryColor);
        Assert.Equal("#1D3C7A", persisted[1].SecondaryColor);
        Assert.Equal("#5BB4FF", persisted[1].AccentColor);
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
