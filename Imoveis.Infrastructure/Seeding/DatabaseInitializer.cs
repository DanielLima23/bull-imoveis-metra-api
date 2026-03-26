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

        await dbContext.Database.ExecuteSqlRawAsync($"CREATE SCHEMA IF NOT EXISTS {AppDbContext.DatabaseSchema}");
        await dbContext.Database.MigrateAsync();

        await SeedUsersAsync(dbContext, passwordHasher);
        await SeedExpenseTypesAsync(dbContext);
        await SeedPendencyTypesAsync(dbContext);
        await SeedSystemSettingsAsync(dbContext);
    }

    private static async Task SeedUsersAsync(AppDbContext dbContext, IPasswordHasher passwordHasher)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Email == "super@dw-softwares.com.br"))
        {
            dbContext.Users.Add(new User
            {
                Name = "Super Usuario",
                Email = "super@dw-softwares.com.br",
                PasswordHash = passwordHasher.Hash("123456"),
                Role = UserRole.ADMIN,
                IsActive = true
            });
        }

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
            new ExpenseType { Name = "Gas", Category = "UTILITIES", IsFixedCost = true },
            new ExpenseType { Name = "Condominio", Category = "CONDOMINIUM", IsFixedCost = true },
            new ExpenseType { Name = "IPTU", Category = "TAX", IsFixedCost = true },
            new ExpenseType { Name = "Reforma", Category = "MAINTENANCE", IsFixedCost = false },
            new ExpenseType { Name = "Extra", Category = "MISC", IsFixedCost = false }
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
            new PendencyType { Code = "DOC", Name = "Documento", Description = "Documento pendente", DefaultSlaDays = 7 },
            new PendencyType { Code = "INFO", Name = "Informacao Essencial", Description = "Informacao essencial ausente", DefaultSlaDays = 3 },
            new PendencyType { Code = "FIN", Name = "Conta Atrasada", Description = "Conta financeira vencida", DefaultSlaDays = 2 },
            new PendencyType { Code = "VIST", Name = "Vistoria", Description = "Pendencia de vistoria", DefaultSlaDays = 5 }
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
            ThemePreset = "SAND_LIGHT",
            PrimaryColor = "#8F6A3A",
            SecondaryColor = "#5E4525",
            AccentColor = "#C69A5D",
            EnableAnimations = true
        });

        await dbContext.SaveChangesAsync();
    }
}
