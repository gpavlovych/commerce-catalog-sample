using Commerce.Application.Abstractions.Messaging;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.CreateProduct;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    Guid SupplierId) : ICommand<Result<Guid>>;
