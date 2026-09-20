using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AspNetCoreDomainLab.Infrastructure.Persistence;

// Bab 12 — migration dan seeding dipisahkan dari request pipeline; keduanya opt-in
// melalui configuration agar startup normal tidak memutasi database diam-diam.
public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDomainMigrationsAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Database:ApplyMigrations"))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() ||
            !configuration.GetValue<bool>("Database:SeedDevelopmentData"))
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<Microsoft.AspNetCore.Identity.IPasswordHasher<AuthUser>>();
        await DevelopmentDataSeeder.SeedAsync(dbContext, passwordHasher, cancellationToken);
    }
}
