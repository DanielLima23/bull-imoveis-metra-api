using Imoveis.Application.Abstractions.Security;
using Imoveis.Application.Abstractions.Services;
using Imoveis.Infrastructure.Options;
using Imoveis.Infrastructure.Persistence;
using Imoveis.Infrastructure.Security;
using Imoveis.Infrastructure.Services;
using System.Globalization;
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
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.DatabaseSchema)));

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IPartyService, PartyService>();
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
        var databaseUrl = GetFirstConfiguredValue(
            configuration,
            "DATABASE_URL",
            "POSTGRES_URL",
            "POSTGRESQL_URL",
            "DATABASE_CONNECTION_STRING",
            "POSTGRES_CONNECTION_STRING");

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return NormalizeConnectionString(ConvertDatabaseUrlToConnectionString(databaseUrl));
        }

        var postgresHost = GetFirstConfiguredValue(configuration, "POSTGRES_HOST", "DB_HOST");
        if (!string.IsNullOrWhiteSpace(postgresHost))
        {
            var username = GetFirstConfiguredValue(configuration, "POSTGRES_USER", "DB_USER") ?? "postgres";
            var password = GetFirstConfiguredValue(configuration, "POSTGRES_PASSWORD", "DB_PASSWORD") ?? string.Empty;
            var database = GetFirstConfiguredValue(configuration, "POSTGRES_DB", "DB_NAME") ?? "postgres";
            var portRaw = GetFirstConfiguredValue(configuration, "POSTGRES_PORT", "DB_PORT");
            var port = 5432;

            if (!string.IsNullOrWhiteSpace(portRaw)
                && int.TryParse(portRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort))
            {
                port = parsedPort;
            }

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = postgresHost,
                Port = port,
                Username = username,
                Password = password,
                Database = database
            };

            return builder.ConnectionString;
        }

        var explicitConnectionString = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return NormalizeConnectionString(explicitConnectionString);
        }

        throw new InvalidOperationException(
            "Database connection not configured. Provide one of: DATABASE_URL/POSTGRES_URL or ConnectionStrings:Default.");
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

        if (!string.IsNullOrWhiteSpace(uri.Query))
        {
            var queryItems = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var item in queryItems)
            {
                var parts = item.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                try
                {
                    builder[key] = value;
                }
                catch (ArgumentException)
                {
                    // Ignore unsupported query parameter keys.
                }
            }
        }

        return builder.ConnectionString;
    }

    private static string NormalizeConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.SearchPath))
        {
            builder.SearchPath = AppDbContext.DatabaseSchema;
        }

        return builder.ConnectionString;
    }

    private static string? GetFirstConfiguredValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
