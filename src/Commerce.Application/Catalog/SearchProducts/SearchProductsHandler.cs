using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.SearchProducts;

public sealed class SearchProductsHandler(IProductRepository products)
    : IQueryHandler<SearchProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(SearchProductsQuery query, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(query.Take, 1, 100);
        var matches = await products.SearchAsync(query.Term ?? string.Empty, take, cancellationToken);
        IReadOnlyList<ProductDto> result = matches.Select(ProductDto.FromDomain).ToList();
        return Result.Success(result);
    }
}
