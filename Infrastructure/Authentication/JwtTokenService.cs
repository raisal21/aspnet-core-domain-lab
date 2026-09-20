using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 11 — service membuat access JWT berisi identity/role claims dan refresh token
// berentropi tinggi yang hanya disimpan sebagai SHA-256 hash.
public sealed class JwtTokenService(JwtOptions options, TimeProvider timeProvider)
{
    private readonly SymmetricSecurityKey _signingKey =
        new(Encoding.UTF8.GetBytes(options.SigningKey));

    // Bab 11: claims ini dibaca oleh User/Authorize policy pada request berikutnya.
    public IssuedAccessToken CreateAccessToken(AuthUser user)
    {
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt.AddMinutes(options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("D")),
            new(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new("name", user.UserName),
            new("display_name", user.DisplayName),
        };

        claims.AddRange(
            user.Roles
                .Select(role => new Claim("role", role.Role))
                .DistinctBy(claim => claim.Value));

        var credentials = new SigningCredentials(
            _signingKey,
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            issuedAt,
            expiresAt);
    }

    // Bab 11: access token dan refresh token memiliki lifetime berbeda.
    public GeneratedRefreshToken CreateRefreshToken()
    {
        var createdAt = timeProvider.GetUtcNow();
        var expiresAt = createdAt.AddDays(options.RefreshTokenDays);
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

        return new GeneratedRefreshToken(
            value,
            HashRefreshToken(value),
            createdAt,
            expiresAt);
    }

    public string HashRefreshToken(string refreshToken)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}

public sealed record IssuedAccessToken(
    string Value,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

public sealed record GeneratedRefreshToken(
    string Value,
    string Hash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc);
