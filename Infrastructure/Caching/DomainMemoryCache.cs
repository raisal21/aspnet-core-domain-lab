using Microsoft.Extensions.Caching.Memory;

namespace AspNetCoreDomainLab.Infrastructure.Caching;

// Bab 13 — abstraction kecil menjaga service tetap bebas dari detail IMemoryCache.
public interface IDomainMemoryCache
{
    bool TryGet<T>(string key, out T? value);

    void Set<T>(string key, T value, TimeSpan lifetime);

    void Remove(string key);
}

// Bab 10/13 — cache hit/miss/set/remove dicatat pada Debug untuk diagnosis TTL dan invalidation.
public sealed class DomainMemoryCache(
    IMemoryCache cache,
    ILogger<DomainMemoryCache> logger) : IDomainMemoryCache
{
    public bool TryGet<T>(string key, out T? value)
    {
        if (cache.TryGetValue(key, out value))
        {
            logger.LogDebug("Memory cache hit for {CacheKey}", key);
            return true;
        }

        logger.LogDebug("Memory cache miss for {CacheKey}", key);
        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan lifetime)
    {
        cache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime,
        });
        logger.LogDebug(
            "Stored {CacheKey} in memory cache for {LifetimeSeconds} seconds",
            key,
            lifetime.TotalSeconds);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        logger.LogDebug("Removed {CacheKey} from memory cache", key);
    }
}
