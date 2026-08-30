using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GoodDeedsApi.Services;

public class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var payload = await cache.GetStringAsync(key, ct);
            return payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
        }
        catch (Exception ex)
        {
            // A cache outage should degrade to a database read, not a 500.
            logger.LogWarning(ex, "Redis read failed for key {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
            };
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value, JsonOptions), options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis write failed for key {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis eviction failed for key {CacheKey}", key);
        }
    }
}
