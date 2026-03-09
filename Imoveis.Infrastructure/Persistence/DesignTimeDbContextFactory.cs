using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Imoveis.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString =
            Environment.GetEnvironmentVariable("IMOVEIS_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=imoveis_api_dev;Username=postgres;Password=postgres";

        var normalizedConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = AppDbContext.DatabaseSchema
        }.ConnectionString;

        optionsBuilder.UseNpgsql(normalizedConnectionString, npgsql =>
            npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.DatabaseSchema));

        return new AppDbContext(optionsBuilder.Options);
    }
}
