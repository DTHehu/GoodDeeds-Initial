namespace GoodDeedsApi.Services;

/// <summary>
/// Thin JSON wrapper over IDistributedCache (Redis) so services do not deal
/// with byte arrays and serialization directly.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default);

    Task RemoveAsync(string key, CancellationToken ct = default);
}
