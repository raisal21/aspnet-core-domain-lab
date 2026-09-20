using System.IO.Compression;
using System.Threading.RateLimiting;
using Asp.Versioning;
using AspNetCoreDomainLab.Infrastructure.Api;
using AspNetCoreDomainLab.Infrastructure.Authentication;
using AspNetCoreDomainLab.Infrastructure.Caching;
using AspNetCoreDomainLab.Infrastructure.OpenApi;
using AspNetCoreDomainLab.Infrastructure.Persistence;
using AspNetCoreDomainLab.Modules.Banking;
using AspNetCoreDomainLab.Modules.Healthcare;
using AspNetCoreDomainLab.Modules.IndustrialAutomation;
using AspNetCoreDomainLab.Modules.Logistics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Mvc;

// Bab 1 — Application Host: builder membaca konfigurasi, menyiapkan DI, dan membangun WebApplication.
var builder = WebApplication.CreateBuilder(args);

// Bab 2 — Controllers dan Bab 8 — Validation: gunakan MVC controller pipeline dan
// ubah model-state failure menjadi contract error sesuai domain request.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = DomainValidationResponseFactory.Create;
    });
// Bab 13 — Caching: cache aplikasi dipakai oleh service dengan TTL dan invalidation eksplisit.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IDomainMemoryCache, DomainMemoryCache>();
// Bab 14 — Response Compression: host menyediakan Brotli/Gzip, lalu provider domain
// dapat mengecualikan response tertentu dari kompresi.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.AddSingleton<IResponseCompressionProvider, DomainResponseCompressionProvider>();
// Bab 13 — Caching: output-cache policy dipisahkan dari cache domain/service.
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("logistics-read", policy => policy
        .Expire(TimeSpan.FromSeconds(15))
        .Tag("logistics"));
    options.AddPolicy("industrial-telemetry", policy => policy
        .Expire(TimeSpan.FromSeconds(3))
        .Tag("industrial"));
});
// Bab 15 — Rate Limiting: setiap bounded domain memilih policy dan window sendiri.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "5";
        return ValueTask.CompletedTask;
    };
    options.AddPolicy("industrial-command", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromSeconds(30),
                QueueLimit = 0,
            }));
    options.AddPolicy("logistics-dispatch", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
    options.AddPolicy("banking-transfer", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.Identity?.Name
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});
// Bab 7 — API Versioning: versi default tidak diasumsikan; client wajib memakai URL segment.
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = false;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
// Bab 11 — Authentication/Authorization dan Bab 12 — EF Core: register boundary
// JWT/policy serta context PostgreSQL sebelum service workflow dibuat.
builder.Services.AddDomainPersistence(builder.Configuration);
builder.Services.AddDomainAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddScoped<HealthcareService>();
builder.Services.AddScoped<IndustrialService>();
builder.Services.AddScoped<LogisticsService>();
builder.Services.AddScoped<BankingService>();
// Bab 16 — API Documentation: built-in OpenAPI memakai transformer untuk bearer security
// dan operasi yang authorized, lalu Swagger UI mengekspos document tersebut.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityOpenApiTransformer>();
    options.AddOperationTransformer<BearerSecurityOpenApiTransformer>();
});

var app = builder.Build();

// Bab 12 — EF Core: migration hanya dijalankan bila configuration mengaktifkannya;
// Bab 11 — Auth: synthetic users hanya di-seed pada Development.
await app.Services.ApplyDomainMigrationsAsync(app.Configuration);
await app.Services.SeedDevelopmentDataAsync(app.Configuration, app.Environment);

// Bab 4 — Middleware: pipeline dipasang berurutan; setiap tahap membungkus tahap berikutnya.
// Bab 16 — API Documentation: OpenAPI dan Swagger UI sengaja hanya dibuka di Development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "ASP.NET Core Domain Lab API";
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "Domain Lab API v1");
    });
}

app.UseHttpsRedirection();
// Bab 14 — Response Compression: provider memutuskan apakah response boleh dikompresi.
app.UseResponseCompression();
app.UseRouting();
// Bab 4/10 — Middleware diagnostics membuat correlation scope dan timing log.
app.UseMiddleware<RequestDiagnosticsMiddleware>();
// Bab 4/9 — Middleware error mengubah DomainRuleException menjadi contract HTTP.
app.UseMiddleware<DomainErrorHandlingMiddleware>();
// Bab 11 — Authentication harus berjalan sebelum authorization policy dievaluasi.
app.UseAuthentication();
app.UseAuthorization();
// Bab 15 — Rate limiter membaca metadata policy dari endpoint.
app.UseRateLimiter();
// Bab 13 — Output cache dijalankan setelah routing dan authorization metadata tersedia.
app.UseOutputCache();

// Bab 2 — Controllers: endpoint domain dipetakan dari attribute routing.
// Bab 3 — Minimal API: sengaja tidak dipakai untuk workflow; lab membandingkannya secara teori.
// Bab 5 — Filters: tidak ada custom MVC filter; cross-cutting concern memakai middleware/metadata.
app.MapControllers();

app.Run();
