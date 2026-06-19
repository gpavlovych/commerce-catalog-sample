using Commerce.Domain.Catalog;

namespace Commerce.Application.Catalog;

/// <summary>Read model returned to callers. Flat and serialisation friendly; no domain types leak out.</summary>
public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    string Currency,
    Guid SupplierId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ProductDto FromDomain(Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Price.Amount,
        product.Price.Currency,
        product.SupplierId,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt);
}
