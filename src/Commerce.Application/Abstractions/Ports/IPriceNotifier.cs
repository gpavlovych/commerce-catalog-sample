using Commerce.Domain.Catalog.Events;

namespace Commerce.Application.Abstractions.Ports;

/// <summary>Pushes a price change to connected clients in real time. Implemented over SignalR in the API host.</summary>
public interface IPriceNotifier
{
    Task PriceChangedAsync(ProductPriceChanged change, CancellationToken cancellationToken);
}
