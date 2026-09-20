using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCoreDomainLab.Infrastructure.Persistence;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11/12 — seed hanya untuk Development agar lab punya identity sintetis yang dapat diuji.
public static class DevelopmentDataSeeder
{
    private static readonly SeedUser[] Users =
    [
        new(
            "healthcare.demo",
            "Healthcare Demo User",
            "HealthDemo!123",
            [AuthRoles.HealthcareReader, AuthRoles.HealthcareWriter]),
        new(
            "industrial.demo",
            "Industrial Demo User",
            "IndustrialDemo!123",
            [AuthRoles.IndustrialOperator, AuthRoles.IndustrialMaintainer]),
        new(
            "logistics.demo",
            "Logistics Demo User",
            "LogisticsDemo!123",
            [AuthRoles.LogisticsReader, AuthRoles.LogisticsDispatcher]),
        new(
            "banking.demo",
            "Banking Demo User",
            "BankingDemo!123",
            [AuthRoles.BankingReader, AuthRoles.BankingTransfer, AuthRoles.BankingRiskReviewer]),
        new(
            "platform.admin",
            "Platform Demo Administrator",
            "PlatformDemo!123",
            [AuthRoles.Administrator]),
    ];

    public static async Task SeedAsync(
        DomainDbContext dbContext,
        IPasswordHasher<AuthUser> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        foreach (var seedUser in Users)
        {
            var normalizedUserName = seedUser.UserName.ToUpperInvariant();
            if (await dbContext.Users.AnyAsync(
                    user => user.NormalizedUserName == normalizedUserName,
                    cancellationToken))
            {
                continue;
            }

            var user = new AuthUser
            {
                Id = Guid.NewGuid(),
                UserName = seedUser.UserName,
                NormalizedUserName = normalizedUserName,
                DisplayName = seedUser.DisplayName,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, seedUser.Password);
            user.Roles = seedUser.Roles
                .Select(role => new AuthUserRole
                {
                    UserId = user.Id,
                    Role = role,
                })
                .ToList();
            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record SeedUser(
        string UserName,
        string DisplayName,
        string Password,
        IReadOnlyList<string> Roles);
}
