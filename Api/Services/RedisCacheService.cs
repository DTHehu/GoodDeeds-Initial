using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace GoodDeedsApi.Services;

/// <summary>
/// A small helper for reading and writing Redis.
///
/// Redis is an in-memory key-value store: you hand it a string key, it hands
/// back a value, and because it lives in RAM the answer arrives in well under a
/// millisecond. It is not a replacement for Postgres — it holds no
/// relationships and forgets things on purpose. It is somewhere to park an
/// answer you already worked out, so you do not have to work it out again.
///
/// Redis only stores text, so this class converts objects to and from JSON for
/// you. See UserService.GetByIdAsync for how it is used.
/// </summary>
public class RedisCacheService
{
    // How long a cached value survives before Redis deletes it by itself.
    // This is a safety net: even if we forget to clear a stale entry somewhere,
    // it fixes itself within five minutes instead of being wrong forever.
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Reads a value from Redis, or returns null if the key is not there.
    /// The &lt;T&gt; means "works with any type" — you say what you expect back:
    ///     await _cache.GetAsync&lt;UserDto&gt;("user:123");
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            string? json = await _cache.GetStringAsync(key);

            if (json == null)
            {
                return default;   // "default" is null for classes and records
            }

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            // If Redis is down we do NOT want the whole request to fail. Saying
            // "not in the cache" makes the caller fall back to the database, so
            // the site gets a little slower instead of breaking.
            _logger.LogWarning(ex, "Could not read {Key} from Redis", key);
            return default;
        }
    }

    /// <summary>Stores a value in Redis for five minutes.</summary>
    public async Task SetAsync<T>(string key, T value)
    {
        try
        {
            string json = JsonSerializer.Serialize(value);

            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            };

            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            // Failing to cache something is not worth failing a request over.
            _logger.LogWarning(ex, "Could not write {Key} to Redis", key);
        }
    }

    /// <summary>
    /// Deletes a key. Call this whenever you change the underlying data,
    /// otherwise the cache will keep serving the old version.
    /// </summary>
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
