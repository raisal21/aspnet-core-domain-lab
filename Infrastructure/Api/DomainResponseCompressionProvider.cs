using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace AspNetCoreDomainLab.Infrastructure.Api;

// Bab 14 — provider membungkus provider bawaan dan menerapkan keputusan compression per domain.
public sealed class DomainResponseCompressionProvider : IResponseCompressionProvider
{
    private readonly ResponseCompressionProvider inner;

    public DomainResponseCompressionProvider(
        IServiceProvider services,
        IOptions<ResponseCompressionOptions> options)
    {
        inner = new ResponseCompressionProvider(services, options);
    }

    public ICompressionProvider? GetCompressionProvider(HttpContext context)
    {
        return IsBankingRequest(context) ? null : inner.GetCompressionProvider(context);
    }

    public bool ShouldCompressResponse(HttpContext context)
    {
        return !IsBankingRequest(context) && inner.ShouldCompressResponse(context);
    }

    public bool CheckRequestAcceptsCompression(HttpContext context)
    {
        return inner.CheckRequestAcceptsCompression(context);
    }

    private static bool IsBankingRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api/v1/banking");
    }
}
