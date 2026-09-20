using System.Diagnostics;

namespace AspNetCoreDomainLab.Infrastructure.Api;

// Bab 4 — Middleware membungkus seluruh request pipeline.
// Bab 9 — DomainRuleException menjadi response JSON/status domain; failure tak terduga tetap dilempar.
// Bab 10 — rejection dan unhandled failure dicatat untuk diagnosis.
public sealed class DomainErrorHandlingMiddleware(
    RequestDelegate next,
    ILogger<DomainErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainRuleException exception)
        {
            logger.LogWarning(
                "Domain rule rejected {Method} {Path}: {ErrorCode}",
                context.Request.Method,
                context.Request.Path,
                exception.ErrorCode);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = exception.StatusCode;
                await context.Response.WriteAsJsonAsync(
                    DomainErrorFactory.FromException(exception),
                    context.RequestAborted);
            }
        }
        catch (Exception exception) when (!Debugger.IsAttached)
        {
            logger.LogError(
                exception,
                "Unhandled failure for {Method} {Path} with correlation {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
            throw;
        }
    }
}
