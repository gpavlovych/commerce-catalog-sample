using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Catalog.Events;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.UpdatePrice;

public sealed class UpdateProductPriceHandler(
    IProductRepository products,
    ICacheService cache,
    IEventPublisher events,
    IPriceNotifier notifier,
    IClock clock) : ICommandHandler<UpdateProductPriceCommand, Result>
{
    public async Task<Result> Handle(UpdateProductPriceCommand command, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure(Error.NotFound($"Product {command.ProductId} was not found."));
        }

        var change = product.ChangePrice(command.NewPrice, clock.UtcNow);
        if (change.IsFailure)
        {
            return change;
        }

        await products.UpdateAsync(product, cancellationToken);

        // Read-through cache for this product is now stale; drop it so the next read repopulates.
        await cache.RemoveAsync(CacheKeys.Product(product.Id), cancellationToken);

        if (product.DomainEvents.Count > 0)
        {
            await events.PublishAsync(product.DomainEvents, cancellationToken);

            foreach (var priceChange in product.DomainEvents.OfType<ProductPriceChanged>())
            {
                await notifier.PriceChangedAsync(priceChange, cancellationToken);
            }

            product.ClearDomainEvents();
        }

        return Result.Success();
    }
}
