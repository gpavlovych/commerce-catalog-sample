using System.Text.Json;
using Commerce.Application.Abstractions.Ports;
using StackExchange.Redis;

namespace Commerce.Infrastructure.Caching;

/// <summary>Azure Cache for Redis implementation of the cache-aside boundary. Values are JSON.</summary>
internal sealed class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private IDatabase Db => redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var value = await Db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<T>((string)value!, JsonOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) where T : class =>
        Db.StringSetAsync(key, JsonSerializer.Serialize(value, JsonOptions), ttl);

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        Db.KeyDeleteAsync(key);
}
