using Commerce.Domain.Catalog;

namespace Commerce.Application.Abstractions.Ports;

/// <summary>
/// Persistence boundary for the product aggregate. The implementation is Dapper over SQL;
/// the application never sees a connection, a transaction, or a SQL string.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task UpdateAsync(Product product, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> SearchAsync(string term, int take, CancellationToken cancellationToken);
}
