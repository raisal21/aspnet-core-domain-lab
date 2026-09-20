namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11/12 — user, role, dan refresh-token entity disimpan di schema auth.
public sealed class AuthUser
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string NormalizedUserName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public ICollection<AuthUserRole> Roles { get; set; } = new List<AuthUserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
