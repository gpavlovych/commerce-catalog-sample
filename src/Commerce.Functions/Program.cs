using Commerce.Application;
using Commerce.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Isolated worker. The same Application and Infrastructure layers the API uses are reused here,
// so business rules and data access are identical across HTTP and serverless hosts.
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
    })
    .Build();

host.Run();
