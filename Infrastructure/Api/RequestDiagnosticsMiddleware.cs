using System.Diagnostics;

namespace AspNetCoreDomainLab.Infrastructure.Api;

// Bab 4 — custom middleware menambahkan behavior lintas endpoint tanpa mengubah controller.
// Bab 10 — correlation scope dan elapsed time membuat satu request dapat ditelusuri di log.
public sealed class RequestDiagnosticsMiddleware(
    RequestDelegate next,
    ILogger<RequestDiagnosticsMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var supplied)
            && !string.IsNullOrWhiteSpace(supplied)
            ? supplied.ToString()
            : context.TraceIdentifier;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        var stopwatch = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = context.Request.Path.ToString(),
        });
        logger.LogInformation(
            "Started {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Completed {Method} {Path} with {StatusCode} in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
