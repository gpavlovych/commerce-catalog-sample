using Commerce.Application.Abstractions.Messaging;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.GetProduct;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<Result<ProductDto>>;
