using Commerce.Application.Abstractions.Messaging;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.SearchProducts;

public sealed record SearchProductsQuery(string Term, int Take = 20) : IQuery<Result<IReadOnlyList<ProductDto>>>;
