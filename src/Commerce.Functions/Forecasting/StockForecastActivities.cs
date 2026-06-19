using Commerce.Application.Abstractions.Ports;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Commerce.Functions.Forecasting;

/// <summary>
/// Activities do the real work and are allowed to touch infrastructure. They reuse the same repository
/// abstraction as the rest of the platform, injected through the worker's DI container.
/// </summary>
public sealed class StockForecastActivities(IProductRepository products, ILogger<StockForecastActivities> logger)
{
    [Function(nameof(GetActiveSkus))]
    public async Task<IReadOnlyList<string>> GetActiveSkus([ActivityTrigger] object? input, CancellationToken cancellationToken)
    {
        var catalog = await products.SearchAsync(string.Empty, take: 100, cancellationToken);
        var skus = catalog.Where(p => p.IsActive).Select(p => p.Sku.Value).ToList();
        logger.LogInformation("Forecasting {Count} active products", skus.Count);
        return skus;
    }

    [Function(nameof(ForecastProduct))]
    public ProductForecast ForecastProduct([ActivityTrigger] string sku)
    {
        // Placeholder model. A real implementation would pull demand history and lead time; the point here
        // is the durable fan-out/fan-in shape, not the statistics.
        var reorder = Math.Abs(sku.GetHashCode()) % 50 + 10;
        return new ProductForecast(sku, reorder);
    }
}
