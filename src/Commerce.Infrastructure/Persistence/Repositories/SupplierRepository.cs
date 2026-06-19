using Commerce.Application.Abstractions.Ports;
using Dapper;

namespace Commerce.Infrastructure.Persistence.Repositories;

internal sealed class SupplierRepository(ISqlConnectionFactory connectionFactory) : ISupplierRepository
{
    public async Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(SqlScripts.SupplierExists, new { Id = supplierId }, cancellationToken: cancellationToken));

        return exists != 0;
    }
}
