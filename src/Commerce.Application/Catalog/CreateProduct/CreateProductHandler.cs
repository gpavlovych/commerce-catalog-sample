using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Catalog;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository products,
    ISupplierRepository suppliers,
    IEventPublisher events,
    IClock clock) : ICommandHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        if (!await suppliers.ExistsAsync(command.SupplierId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound($"Supplier {command.SupplierId} does not exist."));
        }

        if (await products.ExistsBySkuAsync(command.Sku, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict($"A product with SKU '{command.Sku}' already exists."));
        }

        // Version 7 GUIDs are time-ordered, which keeps the clustered index from fragmenting on insert.
        var creation = Product.Create(
            Guid.CreateVersion7(),
            command.Sku,
            command.Name,
            command.Description,
            command.Price,
            command.Currency,
            command.SupplierId,
            clock.UtcNow);

        if (creation.IsFailure)
        {
            return Result.Failure<Guid>(creation.Error);
        }

        var product = creation.Value;
        await products.AddAsync(product, cancellationToken);

        if (product.DomainEvents.Count > 0)
        {
            await events.PublishAsync(product.DomainEvents, cancellationToken);
            product.ClearDomainEvents();
        }

        return Result.Success(product.Id);
    }
}
