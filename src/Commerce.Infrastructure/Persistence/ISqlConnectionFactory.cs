using System.Data.Common;

namespace Commerce.Infrastructure.Persistence;

public enum DatabaseProvider
{
    SqlServer,
    Sqlite
}

/// <summary>Hands out open ADO.NET connections. The provider drives dialect-specific SQL choices.</summary>
public interface ISqlConnectionFactory
{
    DatabaseProvider Provider { get; }
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
