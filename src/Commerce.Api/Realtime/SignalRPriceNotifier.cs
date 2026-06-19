using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Catalog.Events;
using Microsoft.AspNetCore.SignalR;

namespace Commerce.Api.Realtime;

/// <summary>Bridges domain price changes to connected SignalR clients. Registered after AddInfrastructure so it wins.</summary>
internal sealed class SignalRPriceNotifier(IHubContext<PriceHub> hub) : IPriceNotifier
{
    public Task PriceChangedAsync(ProductPriceChanged change, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync(
            "priceChanged",
            new
            {
                change.ProductId,
                change.Sku,
                change.OldAmount,
                change.NewAmount,
                change.Currency,
                change.OccurredAt
            },
            cancellationToken);
}
