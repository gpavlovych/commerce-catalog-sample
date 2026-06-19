using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

namespace Commerce.Infrastructure.Persistence;

internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(DatabaseProvider provider, string connectionString)
    {
        Provider = provider;
        _connectionString = connectionString;

        if (provider == DatabaseProvider.Sqlite)
        {
            EnsureSqliteDirectory(connectionString);
        }
    }

    public DatabaseProvider Provider { get; }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = Provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(_connectionString),
            DatabaseProvider.Sqlite => new SqliteConnection(_connectionString),
            _ => throw new InvalidOperationException($"Unsupported provider {Provider}.")
        };

        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void EnsureSqliteDirectory(string connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
