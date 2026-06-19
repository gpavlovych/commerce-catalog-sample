using Commerce.Application.Abstractions.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace Commerce.Infrastructure.Caching;

/// <summary>Demo-mode cache. Same cache-aside contract as Redis, so the code paths are identical.</summary>
internal sealed class InMemoryCacheService(IMemoryCache cache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class =>
        Task.FromResult(cache.TryGetValue(key, out var value) ? value as T : null);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class
    {
        cache.Set(key, value, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl });
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
