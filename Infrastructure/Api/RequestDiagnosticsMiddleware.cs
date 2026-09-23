using System.Diagnostics;

namespace AspNetCoreDomainLab.Infrastructure.Api;

// Bab 4 — custom middleware menambahkan behavior lintas endpoint tanpa mengubah controller.
// Bab 10 — correlation scope dan elapsed time membuat satu request dapat ditelusuri di log.
public sealed class RequestDiagnosticsMiddleware(
    RequestDelegate next,
    ILogger<RequestDiagnosticsMiddleware> logger)
{
    private const int MaxCorrelationIdLength = 128;

    public async Task InvokeAsync(HttpContext context)
    {
        var suppliedCorrelationId = context.Request.Headers.TryGetValue(
            "X-Correlation-Id",
            out var supplied)
            ? supplied.ToString()
            : string.Empty;
        var correlationId = IsValidCorrelationId(suppliedCorrelationId)
            ? suppliedCorrelationId
            : context.TraceIdentifier;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        var stopwatch = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.ToString(),
        });
        logger.LogInformation(
            "Started {Method} {Path} with correlation {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Completed {Method} {Path} with {StatusCode} in {ElapsedMilliseconds} ms " +
                "for correlation {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static bool IsValidCorrelationId(string value)
    {
        return value.Length is > 0 and <= MaxCorrelationIdLength &&
            value.All(character =>
                char.IsLetterOrDigit(character) ||
                character is '-' or '_' or '.' or ':');
    }
}
