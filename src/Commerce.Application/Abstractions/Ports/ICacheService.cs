namespace Commerce.Application.Abstractions.Ports;

/// <summary>
/// Read-through cache boundary. Backed by Azure Cache for Redis in production and an in-memory
/// implementation in demo mode, so cache-aside code paths are exercised either way.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken);
}
