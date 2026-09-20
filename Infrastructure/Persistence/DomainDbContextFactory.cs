using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AspNetCoreDomainLab.Infrastructure.Persistence;

// Bab 12 — EF tooling memakai factory ini agar migration dapat dibuat tanpa menyalakan host web.
public sealed class DomainDbContextFactory : IDesignTimeDbContextFactory<DomainDbContext>
{
    private const string DevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=domainlab;Username=domainlab;Password=domainlab_dev_only;GSS Encryption Mode=Disable";

    public DomainDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DomainDb")
            ?? DevelopmentConnectionString;

        var options = new DbContextOptionsBuilder<DomainDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory",
                    DatabaseSchemas.Public))
            .Options;

        return new DomainDbContext(options);
    }
}
