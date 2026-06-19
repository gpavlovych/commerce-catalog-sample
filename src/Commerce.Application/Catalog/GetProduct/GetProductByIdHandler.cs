using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.GetProduct;

public sealed class GetProductByIdHandler(
    IProductRepository products,
    ICacheService cache) : IQueryHandler<GetProductByIdQuery, Result<ProductDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Product(query.ProductId);

        var cached = await cache.GetAsync<ProductDto>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var product = await products.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<ProductDto>(Error.NotFound($"Product {query.ProductId} was not found."));
        }

        var dto = ProductDto.FromDomain(product);
        await cache.SetAsync(key, dto, CacheTtl, cancellationToken);
        return dto;
    }
}
