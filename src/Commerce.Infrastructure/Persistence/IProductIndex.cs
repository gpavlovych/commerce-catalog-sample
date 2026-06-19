using Commerce.Domain.Catalog;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// Optional search read model. When a real index is available (RediSearch) the repository searches it
/// and loads the matched rows from SQL. <see cref="SearchAsync"/> returns null to signal "no index here,
/// fall back to a SQL scan", which is what the in-memory demo does.
/// </summary>
public interface IProductIndex
{
    Task EnsureCreatedAsync(CancellationToken cancellationToken);
    Task UpsertAsync(Product product, CancellationToken cancellationToken);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>?> SearchAsync(string term, int take, CancellationToken cancellationToken);
}
