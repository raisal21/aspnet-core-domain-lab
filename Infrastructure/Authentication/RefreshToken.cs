namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11/12 — hanya hash refresh token yang dipersist; Version dipakai optimistic concurrency.
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AuthUser User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? RevocationReason { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
