namespace Commerce.Application.Catalog;

/// <summary>Single source of truth for cache key shapes, so reads and invalidations cannot drift apart.</summary>
public static class CacheKeys
{
    public static string Product(Guid id) => $"catalog:product:{id}";
}
