using Imoveis.Application.Abstractions.Security;
using Imoveis.Domain.Entities;
using Imoveis.Domain.Enums;
using Imoveis.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Imoveis.Infrastructure.Seeding;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var dbContext = provider.GetRequiredService<AppDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();

        await dbContext.Database.MigrateAsync();

        await SeedUsersAsync(dbContext, passwordHasher);
        await SeedExpenseTypesAsync(dbContext);
        await SeedPendencyTypesAsync(dbContext);
        await SeedSystemSettingsAsync(dbContext);
    }

    private static async Task SeedUsersAsync(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Email == "admin@imoveis.dev"))
        {
            dbContext.Users.Add(new User
            {
                Name = "Administrador",
                Email = "admin@imoveis.dev",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.ADMIN,
                IsActive = true
            });
        }

        if (!await dbContext.Users.AnyAsync(x => x.Email == "operador@imoveis.dev"))
        {
            dbContext.Users.Add(new User
            {
                Name = "Operador",
                Email = "operador@imoveis.dev",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.OPERATOR,
                IsActive = true
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedExpenseTypesAsync(AppDbContext dbContext)
    {
        if (await dbContext.ExpenseTypes.AnyAsync())
        {
            return;
        }

        dbContext.ExpenseTypes.AddRange(
            new ExpenseType { Name = "Agua", Category = "UTILITIES", IsFixedCost = true },
            new ExpenseType { Name = "Luz", Category = "UTILITIES", IsFixedCost = true },
            new ExpenseType { Name = "Condominio", Category = "CONDOMINIUM", IsFixedCost = true },
            new ExpenseType { Name = "IPTU", Category = "TAX", IsFixedCost = true },
            new ExpenseType { Name = "Reforma", Category = "MAINTENANCE", IsFixedCost = false }
        );

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedPendencyTypesAsync(AppDbContext dbContext)
    {
        if (await dbContext.PendencyTypes.AnyAsync())
        {
            return;
        }

        dbContext.PendencyTypes.AddRange(
            new PendencyType { Name = "Documento", DefaultSlaDays = 7 },
            new PendencyType { Name = "Informacao Essencial", DefaultSlaDays = 3 },
            new PendencyType { Name = "Conta Atrasada", DefaultSlaDays = 2 },
            new PendencyType { Name = "Vistoria", DefaultSlaDays = 5 }
        );

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedSystemSettingsAsync(AppDbContext dbContext)
    {
        if (await dbContext.SystemSettings.AnyAsync())
        {
            return;
        }

        dbContext.SystemSettings.Add(new SystemSettings
        {
            BrandName = "Imoveis Hub",
            BrandShortName = "IH",
            ThemePreset = "AURORA_LIGHT",
            PrimaryColor = "#1176EE",
            SecondaryColor = "#0A58BA",
            AccentColor = "#06B6D4",
            EnableAnimations = true
        });

        await dbContext.SaveChangesAsync();
    }
}
