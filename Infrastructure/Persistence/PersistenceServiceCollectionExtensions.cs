using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCoreDomainLab.Infrastructure.Persistence;

// Bab 1/12 — host mendaftarkan satu context PostgreSQL dan memindahkan migration history
// ke public agar schema bisnis tetap dimiliki oleh module masing-masing.
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDomainPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DomainDb")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DomainDb is required to configure domain persistence.");

        services.AddDbContext<DomainDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    DatabaseSchemas.Public)));

        return services;
    }
}
