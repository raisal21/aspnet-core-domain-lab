using System.Security.Claims;
using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDomainLab.Controllers;

// Bab 2 — Controllers: endpoint tipis memanggil application service dan mengembalikan ActionResult.
// Bab 6 — REST: login/refresh/revoke memakai POST karena mengubah atau menerbitkan credential state.
// Bab 7 — API Versioning: route version berada di URL, bukan header tersembunyi.
// Bab 8 — Validation: [ApiController] mengaktifkan model binding/model-state handling dari host.
// Bab 11 — Authentication/Authorization: AllowAnonymous dan Authorize membedakan boundary credential.
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(ILocalAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new AuthErrorResponse(
                "invalid_credentials",
                "The supplied credentials are not valid."))
            : Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthTokenResponse>> Refresh(
        RefreshRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return response is null
            ? Unauthorized(new AuthErrorResponse(
                "invalid_refresh_token",
                "The refresh token is invalid, expired, or revoked."))
            : Ok(response);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke(
        RevokeRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        await authService.RevokeAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentIdentityResponse> Me()
    {
        var subject = User.FindFirstValue("sub");
        var userName = User.FindFirstValue("name") ?? string.Empty;
        var roles = User.FindAll("role")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(role => role, StringComparer.Ordinal)
            .ToArray();

        return Ok(new CurrentIdentityResponse(subject ?? string.Empty, userName, roles));
    }

    [HttpGet("probes/healthcare")]
    [Authorize(Policy = AuthPolicies.HealthcareRead)]
    public ActionResult<AuthorizationProbeResponse> HealthcareProbe()
    {
        return Ok(new AuthorizationProbeResponse(
            AuthPolicies.HealthcareRead,
            User.Identity?.Name ?? string.Empty));
    }

    [HttpGet("probes/banking-transfer")]
    [Authorize(Policy = AuthPolicies.BankingTransfer)]
    public ActionResult<AuthorizationProbeResponse> BankingTransferProbe()
    {
        return Ok(new AuthorizationProbeResponse(
            AuthPolicies.BankingTransfer,
            User.Identity?.Name ?? string.Empty));
    }
}
