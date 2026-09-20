using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCoreDomainLab.Infrastructure.Persistence;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11 — service ini memisahkan credential workflow dari AuthController.
public interface ILocalAuthService
{
    Task<AuthTokenResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResponse?> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid userId,
        RevokeRequest request,
        CancellationToken cancellationToken = default);
}

// Bab 10/11/12 — log auth events tanpa secret; refresh rotation berjalan dalam EF transaction
// dan optimistic concurrency mencegah dua request memakai refresh token yang sama.
public sealed class LocalAuthService(
    DomainDbContext dbContext,
    IPasswordHasher<AuthUser> passwordHasher,
    JwtTokenService tokenService,
    TimeProvider timeProvider,
    ILogger<LocalAuthService> logger) : ILocalAuthService
{
    // Bab 11: password diverifikasi dengan hasher; database hanya menyimpan hash refresh token.
    public async Task<AuthTokenResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var normalizedUserName = NormalizeUserName(request.UserName);
        var user = await dbContext.Users
            .Include(candidate => candidate.Roles)
            .SingleOrDefaultAsync(
                candidate => candidate.NormalizedUserName == normalizedUserName,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        }

        var issuedTokens = IssueTokens(user);
        dbContext.RefreshTokens.Add(issuedTokens.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return issuedTokens.Response;
    }

    // Bab 11/12: refresh token di-rotate atomically; reuse mendeteksi compromise dan mencabut token aktif.
    public async Task<AuthTokenResponse?> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

            var storedToken = await dbContext.RefreshTokens
                .Include(token => token.User)
                .ThenInclude(user => user.Roles)
                .SingleOrDefaultAsync(
                    token => token.TokenHash == tokenHash,
                    cancellationToken);
            var now = timeProvider.GetUtcNow();

            if (storedToken is null ||
                storedToken.ExpiresAtUtc <= now ||
                !storedToken.User.IsActive)
            {
                return null;
            }

            if (storedToken.RevokedAtUtc is not null)
            {
                var activeTokens = await dbContext.RefreshTokens
                    .Where(token =>
                        token.UserId == storedToken.UserId &&
                        token.RevokedAtUtc == null)
                    .ToListAsync(cancellationToken);
                foreach (var activeToken in activeTokens)
                {
                    activeToken.RevokedAtUtc = now;
                    activeToken.Version++;
                    activeToken.RevocationReason = "refresh-token-reuse-detected";
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                logger.LogWarning(
                    "Rejected a previously used refresh token for user {UserId} and revoked active refresh tokens.",
                    storedToken.UserId);
                return null;
            }

            var issuedTokens = IssueTokens(storedToken.User);
            storedToken.RevokedAtUtc = now;
            storedToken.Version++;
            storedToken.RevocationReason = "rotated";
            storedToken.ReplacedByTokenHash = issuedTokens.RefreshToken.TokenHash;
            dbContext.RefreshTokens.Add(issuedTokens.RefreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return issuedTokens.Response;
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Rejected a concurrent refresh attempt.");
            return null;
        }
    }

    public async Task<bool> RevokeAsync(
        Guid userId,
        RevokeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return false;
        }

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.UserId == userId && token.TokenHash == tokenHash,
            cancellationToken);
        if (storedToken is null || storedToken.RevokedAtUtc is not null)
        {
            return false;
        }

        storedToken.RevokedAtUtc = timeProvider.GetUtcNow();
        storedToken.Version++;
        storedToken.RevocationReason = "user-requested";
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private (AuthTokenResponse Response, RefreshToken RefreshToken) IssueTokens(AuthUser user)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken();
        var roles = user.Roles
            .Select(role => role.Role)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();
        var storedRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshToken.Hash,
            CreatedAtUtc = refreshToken.CreatedAtUtc,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc,
        };

        return (
            new AuthTokenResponse(
                accessToken.Value,
                "Bearer",
                accessToken.ExpiresAtUtc,
                refreshToken.Value,
                refreshToken.ExpiresAtUtc,
                user.Id,
                user.UserName,
                roles),
            storedRefreshToken);
    }

    private static string NormalizeUserName(string userName)
    {
        return userName.Trim().ToUpperInvariant();
    }
}
