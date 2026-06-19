using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Catalog.Events;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Messaging;

/// <summary>
/// Default realtime notifier. The API host replaces this with a SignalR implementation; hosts without a
/// realtime channel (for example the Functions worker) keep this and just log.
/// </summary>
internal sealed class NullPriceNotifier(ILogger<NullPriceNotifier> logger) : IPriceNotifier
{
    public Task PriceChangedAsync(ProductPriceChanged change, CancellationToken cancellationToken)
    {
        logger.LogInformation("Price changed for {Sku}: {Old} -> {New} {Currency} (no realtime channel)",
            change.Sku, change.OldAmount, change.NewAmount, change.Currency);
        return Task.CompletedTask;
    }
}
