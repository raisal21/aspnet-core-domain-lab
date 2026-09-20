using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreDomainLab.Infrastructure.Api;

// Bab 9 — setiap bounded domain mempertahankan bentuk error response yang berbeda.
public enum DomainArea
{
    Platform,
    Healthcare,
    Industrial,
    Logistics,
    Banking,
}

// Bab 9: exception ini membawa status, code, reference, dan retryability ke middleware.
public sealed class DomainRuleException : Exception
{
    public DomainRuleException(
        DomainArea area,
        string errorCode,
        string message,
        int statusCode = StatusCodes.Status409Conflict,
        string? resourceReference = null,
        bool retryable = false,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Area = area;
        ErrorCode = errorCode;
        StatusCode = statusCode;
        ResourceReference = resourceReference;
        Retryable = retryable;
        Errors = errors;
    }

    public DomainArea Area { get; }

    public string ErrorCode { get; }

    public int StatusCode { get; }

    public string? ResourceReference { get; }

    public bool Retryable { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }
}

public sealed record HealthcareErrorResponse(
    string ErrorCode,
    string Message,
    string? PatientReference = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record IndustrialErrorResponse(
    string ErrorCode,
    string Message,
    string? AssetCode = null,
    string Severity = "error",
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record LogisticsErrorResponse(
    string ErrorCode,
    string Message,
    string? TrackingNumber = null,
    bool Retryable = false,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public sealed record BankingErrorResponse(
    string ErrorCode,
    string Message,
    string? AccountReference = null,
    IReadOnlyList<string>? Violations = null);

public sealed record PlatformErrorResponse(
    string ErrorCode,
    string Message,
    IReadOnlyDictionary<string, string[]>? Errors = null);

// Bab 9: factory memisahkan aturan domain dari format JSON di HTTP boundary.
public static class DomainErrorFactory
{
    public static object FromException(DomainRuleException exception)
    {
        return exception.Area switch
        {
            DomainArea.Healthcare => new HealthcareErrorResponse(
                exception.ErrorCode,
                exception.Message,
                exception.ResourceReference,
                exception.Errors),
            DomainArea.Industrial => new IndustrialErrorResponse(
                exception.ErrorCode,
                exception.Message,
                exception.ResourceReference,
                exception.Retryable ? "warning" : "error",
                exception.Errors),
            DomainArea.Logistics => new LogisticsErrorResponse(
                exception.ErrorCode,
                exception.Message,
                exception.ResourceReference,
                exception.Retryable,
                exception.Errors),
            DomainArea.Banking => new BankingErrorResponse(
                exception.ErrorCode,
                exception.Message,
                exception.ResourceReference,
                exception.Errors?.SelectMany(pair => pair.Value).ToArray()),
            _ => new PlatformErrorResponse(
                exception.ErrorCode,
                exception.Message,
                exception.Errors),
        };
    }

    public static object FromValidation(
        DomainArea area,
        IReadOnlyDictionary<string, string[]> errors)
    {
        return area switch
        {
            DomainArea.Healthcare => new HealthcareErrorResponse(
                "validation_failed",
                "The healthcare request is invalid.",
                Errors: errors),
            DomainArea.Industrial => new IndustrialErrorResponse(
                "validation_failed",
                "The industrial automation request is invalid.",
                Severity: "error",
                Errors: errors),
            DomainArea.Logistics => new LogisticsErrorResponse(
                "validation_failed",
                "The logistics request is invalid.",
                Errors: errors),
            DomainArea.Banking => new BankingErrorResponse(
                "validation_failed",
                "The banking request is invalid.",
                Violations: errors.SelectMany(pair => pair.Value).ToArray()),
            _ => new PlatformErrorResponse(
                "validation_failed",
                "The request is invalid.",
                errors),
        };
    }
}

// Bab 8/9: path versioned dipakai untuk memilih validation/error contract yang tepat.
public static class DomainNameResolver
{
    public static DomainArea Resolve(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.Contains("/healthcare/", StringComparison.OrdinalIgnoreCase))
        {
            return DomainArea.Healthcare;
        }

        if (value.Contains("/industrial/", StringComparison.OrdinalIgnoreCase))
        {
            return DomainArea.Industrial;
        }

        if (value.Contains("/logistics/", StringComparison.OrdinalIgnoreCase))
        {
            return DomainArea.Logistics;
        }

        if (value.Contains("/banking/", StringComparison.OrdinalIgnoreCase))
        {
            return DomainArea.Banking;
        }

        return DomainArea.Platform;
    }
}

// Bab 8: ApiController memanggil factory ini ketika model binding/DataAnnotations gagal.
public static class DomainValidationResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(pair => pair.Value is not null)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The value is invalid."
                        : error.ErrorMessage)
                    .ToArray());
        var response = DomainErrorFactory.FromValidation(
            DomainNameResolver.Resolve(context.HttpContext.Request.Path),
            errors);
        return new BadRequestObjectResult(response);
    }
}
