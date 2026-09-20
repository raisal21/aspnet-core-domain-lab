namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11/12 — join entity menghubungkan identity dengan role policy secara persisted.
public sealed class AuthUserRole
{
    public Guid UserId { get; set; }

    public AuthUser User { get; set; } = null!;

    public string Role { get; set; } = string.Empty;
}
