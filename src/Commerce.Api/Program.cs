using Commerce.Api;
using Commerce.Api.Endpoints;
using Commerce.Api.Realtime;
using Commerce.Application;
using Commerce.Application.Abstractions.Ports;
using Commerce.Infrastructure;
using Commerce.Infrastructure.Persistence;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Realtime over SignalR. Registered after AddInfrastructure so it replaces the default notifier.
builder.Services.AddSignalR();
builder.Services.AddSingleton<IPriceNotifier, SignalRPriceNotifier>();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create the schema and seed the demo catalog. Idempotent; see DatabaseInitializer.
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.UseExceptionHandler();
app.UseCors("frontend");

// Serve the demo console (operations UI) at the site root. The files are linked in from /frontend and
// copied to the output's wwwroot at build time (see Commerce.Api.csproj). Resolve that folder explicitly
// off the app's base directory so it serves under `dotnet run` (where the content root is the project
// folder, not the output) and in the published container alike.
var consoleRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
if (Directory.Exists(consoleRoot))
{
    var consoleFiles = new PhysicalFileProvider(consoleRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = consoleFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = consoleFiles });
}

app.MapOpenApi();
app.MapScalarApiReference(options => options
    .WithTitle("Commerce Catalog API")
    .WithTheme(ScalarTheme.Mars));

app.MapProductEndpoints();
app.MapHub<PriceHub>("/hubs/prices");
app.MapHealthChecks("/health");

app.Run();

// Exposed so the integration test project can reference the entry point via WebApplicationFactory.
public partial class Program;
