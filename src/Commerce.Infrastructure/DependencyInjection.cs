using Azure.Messaging.ServiceBus;
using Commerce.Application.Abstractions.Ports;
using Commerce.Infrastructure.Caching;
using Commerce.Infrastructure.Messaging;
using Commerce.Infrastructure.Persistence;
using Commerce.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Commerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPersistence(services, configuration);
        AddCachingAndSearch(services, configuration);
        AddMessaging(services, configuration);

        services.AddSingleton<IClock, SystemClock>();

        // Default realtime notifier. The API host registers a SignalR implementation after this call,
        // which wins because it is registered last.
        services.AddSingleton<IPriceNotifier, NullPriceNotifier>();

        services.AddScoped<DatabaseInitializer>();
        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var provider = ParseProvider(configuration["Database:Provider"]);
        var connectionString = configuration.GetConnectionString("Commerce")
            ?? "Data Source=App_Data/commerce-demo.db";

        if (provider == DatabaseProvider.Sqlite)
        {
            // SQLite needs text-based handling for GUID, decimal, and datetimeoffset.
            SqliteTypeHandlers.Register();
        }

        services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(provider, connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
    }

    private static void AddCachingAndSearch(IServiceCollection services, IConfiguration configuration)
    {
        var useRedis = string.Equals(configuration["Cache:Provider"], "Redis", StringComparison.OrdinalIgnoreCase);

        if (useRedis)
        {
            var redisConnection = configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("ConnectionStrings:Redis is required when Cache:Provider is Redis.");

            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
            services.AddSingleton<ICacheService, RedisCacheService>();
            services.AddSingleton<IProductIndex, RediSearchProductIndex>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IProductIndex, NoOpProductIndex>();
        }
    }

    private static void AddMessaging(IServiceCollection services, IConfiguration configuration)
    {
        var useServiceBus = string.Equals(configuration["Messaging:Provider"], "ServiceBus", StringComparison.OrdinalIgnoreCase);

        if (useServiceBus)
        {
            var connectionString = configuration.GetConnectionString("ServiceBus")
                ?? throw new InvalidOperationException("ConnectionStrings:ServiceBus is required when Messaging:Provider is ServiceBus.");
            var topic = configuration["Messaging:Topic"] ?? "catalog-events";

            services.AddSingleton(_ => new ServiceBusClient(connectionString));
            services.AddSingleton<IEventPublisher>(sp => new ServiceBusEventPublisher(
                sp.GetRequiredService<ServiceBusClient>(), topic));
        }
        else
        {
            services.AddSingleton<IEventPublisher, InProcessEventPublisher>();
        }
    }

    private static DatabaseProvider ParseProvider(string? value) =>
        Enum.TryParse<DatabaseProvider>(value, ignoreCase: true, out var provider)
            ? provider
            : DatabaseProvider.Sqlite;
}
