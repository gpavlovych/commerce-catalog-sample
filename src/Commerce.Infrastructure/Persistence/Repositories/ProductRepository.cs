using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Catalog;
using Dapper;

namespace Commerce.Infrastructure.Persistence.Repositories;

/// <summary>
/// Dapper over SQL. The aggregate has private setters and no parameterless constructor, so rows are read
/// into an explicit <see cref="ProductRow"/> and rebuilt through <see cref="Product.Rehydrate"/> rather
/// than letting an ORM reflect into private state.
/// </summary>
internal sealed class ProductRepository(ISqlConnectionFactory connectionFactory, IProductIndex index) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            new CommandDefinition(SqlScripts.GetById, new { Id = id }, cancellationToken: cancellationToken));

        return row?.ToDomain();
    }

    public async Task<bool> ExistsBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(SqlScripts.ExistsBySku, new { Sku = sku.ToUpperInvariant() }, cancellationToken: cancellationToken));

        return exists != 0;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(SqlScripts.Insert, ToParameters(product), cancellationToken: cancellationToken));

        await index.UpsertAsync(product, cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            SqlScripts.UpdatePrice,
            new { product.Id, PriceAmount = product.Price.Amount, product.UpdatedAt },
            cancellationToken: cancellationToken));

        await index.UpsertAsync(product, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string term, int take, CancellationToken cancellationToken)
    {
        var indexedIds = await index.SearchAsync(term, take, cancellationToken);

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        if (indexedIds is not null)
        {
            if (indexedIds.Count == 0)
            {
                return [];
            }

            var byId = (await connection.QueryAsync<ProductRow>(
                    new CommandDefinition(SqlScripts.GetByIds, new { Ids = indexedIds }, cancellationToken: cancellationToken)))
                .ToDictionary(r => r.Id);

            // Preserve the relevance order the index returned.
            return indexedIds
                .Where(byId.ContainsKey)
                .Select(id => byId[id].ToDomain())
                .ToList();
        }

        var normalised = (term ?? string.Empty).Trim();
        var rows = await connection.QueryAsync<ProductRow>(new CommandDefinition(
            SqlScripts.Search(connectionFactory.Provider),
            new { Term = normalised, Like = $"%{normalised}%", Take = take },
            cancellationToken: cancellationToken));

        return rows.Select(r => r.ToDomain()).ToList();
    }

    private static object ToParameters(Product product) => new
    {
        product.Id,
        Sku = product.Sku.Value,
        product.Name,
        product.Description,
        PriceAmount = product.Price.Amount,
        Currency = product.Price.Currency,
        product.SupplierId,
        product.IsActive,
        product.CreatedAt,
        product.UpdatedAt
    };

    // Column names line up with these property names, so Dapper maps the row without configuration.
    // This is a property-mapped class (parameterless constructor + settable members) rather than a
    // positional record on purpose: Dapper only applies the registered SQLite type handlers
    // (Guid/decimal/DateTimeOffset stored as text) on the property path, not when materializing through
    // a record constructor. Against SQL Server the native column types map directly either way.
    private sealed class ProductRow
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal PriceAmount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Product ToDomain() =>
            Product.Rehydrate(Id, Sku, Name, Description, PriceAmount, Currency, SupplierId, IsActive, CreatedAt, UpdatedAt);
    }
}
