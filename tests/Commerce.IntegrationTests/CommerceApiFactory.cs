using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;
using Testcontainers.Redis;
using Xunit;

namespace Commerce.IntegrationTests;

/// <summary>
/// Boots the real API against throwaway SQL Server and Redis Stack containers, so integration tests exercise
/// the same Dapper queries, RediSearch index, and cache-aside paths that run in production. Nothing is mocked
/// below the HTTP boundary.
/// </summary>
public sealed class CommerceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    // Redis Stack ships the RediSearch module the index depends on.
    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis/redis-stack-server:latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SqlServer",
                ["ConnectionStrings:Commerce"] = _sql.GetConnectionString(),
                ["Cache:Provider"] = "Redis",
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString() + ",abortConnect=false",
                ["Messaging:Provider"] = "InProcess"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await _redis.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sql.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
