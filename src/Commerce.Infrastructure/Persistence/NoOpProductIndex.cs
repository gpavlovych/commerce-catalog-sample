using Commerce.Domain.Catalog;

namespace Commerce.Infrastructure.Persistence;

/// <summary>Used in demo mode. Search returns null so the repository falls back to a SQL scan.</summary>
internal sealed class NoOpProductIndex : IProductIndex
{
    public Task EnsureCreatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task UpsertAsync(Product product, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task RemoveAsync(Guid id, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IReadOnlyList<Guid>?> SearchAsync(string term, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>?>(null);
}
