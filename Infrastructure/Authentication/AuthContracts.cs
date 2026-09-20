namespace AspNetCoreDomainLab.Infrastructure.Authentication;

// Bab 8/11 — contract credential dipakai model binding, lalu service mengeluarkan token/identity DTO.
public sealed class LoginRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RevokeRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    Guid UserId,
    string UserName,
    IReadOnlyList<string> Roles);

public sealed record AuthErrorResponse(string Error, string Message);

public sealed record CurrentIdentityResponse(
    string Subject,
    string UserName,
    IReadOnlyList<string> Roles);

public sealed record AuthorizationProbeResponse(string Policy, string UserName);
