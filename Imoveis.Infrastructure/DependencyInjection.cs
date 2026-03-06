using Imoveis.Application.Abstractions.Security;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Infrastructure.Options;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Security;
using Imoveis.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Imoveis.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ILeaseService, LeaseService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IPendencyService, PendencyService>();
        services.AddScoped<IVisitService, VisitService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();

        return services;
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertDatabaseUrlToConnectionString(databaseUrl);
        }

        var explicitConnectionString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return explicitConnectionString;
        }

        throw new InvalidOperationException("ConnectionStrings:Default or DATABASE_URL must be configured.");
    }

    private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
    {
        if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.Trim('/');

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException("DATABASE_URL is missing username.");
        }

        if (string.IsNullOrWhiteSpace(database))
        {
            database = "postgres";
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = username,
            Password = password,
            Database = database
        };

        return builder.ConnectionString;
    }
}
