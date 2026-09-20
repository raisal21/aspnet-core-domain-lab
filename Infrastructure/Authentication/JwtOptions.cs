using System.Text;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11 — options tervalidasi saat host start agar token tidak dibuat dengan secret/lifetime invalid.
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; }

    public int RefreshTokenDays { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(SigningKey) ||
            Encoding.UTF8.GetByteCount(SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must contain at least 32 UTF-8 bytes for HS256.");
        }

        if (AccessTokenMinutes is < 1 or > 60)
        {
            throw new InvalidOperationException(
                "Jwt:AccessTokenMinutes must be between 1 and 60.");
        }

        if (RefreshTokenDays is < 1 or > 30)
        {
            throw new InvalidOperationException(
                "Jwt:RefreshTokenDays must be between 1 and 30.");
        }
    }
}
