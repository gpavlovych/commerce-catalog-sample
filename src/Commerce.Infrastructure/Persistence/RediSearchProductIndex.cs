using System.Globalization;
using Commerce.Domain.Catalog;
using Microsoft.Extensions.Logging;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using StackExchange.Redis;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// RediSearch-backed product index. Products are stored as Redis hashes under the "product:" prefix and
/// queried through a secondary index. SQL Server stays the source of truth; this is a derived read model
/// that keeps full-text and prefix search off the relational database.
/// </summary>
internal sealed class RediSearchProductIndex(IConnectionMultiplexer redis, ILogger<RediSearchProductIndex> logger)
    : IProductIndex
{
    private const string IndexName = "idx:products";
    private const string KeyPrefix = "product:";

    private IDatabase Db => redis.GetDatabase();

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        var ft = Db.FT();
        try
        {
            var schema = new Schema()
                .AddTextField(new FieldName("name"), weight: 2.0)
                .AddTextField(new FieldName("sku"))
                .AddTagField(new FieldName("supplierId"))
                .AddNumericField(new FieldName("price"));

            var parameters = FTCreateParams.CreateParams()
                .On(IndexDataType.HASH)
                .Prefix(KeyPrefix);

            await ft.CreateAsync(IndexName, parameters, schema);
            logger.LogInformation("Created RediSearch index {Index}", IndexName);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("Index already exists", StringComparison.OrdinalIgnoreCase))
        {
            // Index already present, nothing to do.
        }
    }

    public Task UpsertAsync(Product product, CancellationToken cancellationToken)
    {
        var entries = new HashEntry[]
        {
            new("sku", product.Sku.Value),
            new("name", product.Name),
            new("supplierId", product.SupplierId.ToString()),
            new("price", product.Price.Amount.ToString(CultureInfo.InvariantCulture)),
            new("active", product.IsActive ? "1" : "0")
        };

        return Db.HashSetAsync(KeyPrefix + product.Id, entries);
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken) =>
        Db.KeyDeleteAsync(KeyPrefix + id);

    public async Task<IReadOnlyList<Guid>?> SearchAsync(string term, int take, CancellationToken cancellationToken)
    {
        var ft = Db.FT();
        var expression = BuildExpression(term);
        var query = new Query(expression).Limit(0, take);

        var result = await ft.SearchAsync(IndexName, query);

        return result.Documents
            .Select(d => d.Id.StartsWith(KeyPrefix, StringComparison.Ordinal) ? d.Id[KeyPrefix.Length..] : d.Id)
            .Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToList();
    }

    private static string BuildExpression(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return "*";
        }

        var escaped = term.Trim().Replace("\"", string.Empty, StringComparison.Ordinal);
        return $"(@name:{escaped}*) | (@sku:{escaped}*)";
    }
}
