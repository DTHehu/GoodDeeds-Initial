using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GoodDeedsApi.Services;

/// <summary>
/// JSON wrapper over Redis. Every method swallows connection failures: a cache
/// outage should make the app slower, not broken, so a failed read looks like a
/// miss and the caller falls through to Postgres.
/// </summary>
public class RedisCacheService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var json = await _cache.GetStringAsync(key);

            return json == null ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read {Key} from Redis", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write {Key} to Redis", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete {Key} from Redis", key);
        }
    }
}
