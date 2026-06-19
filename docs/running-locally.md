# Running locally

## Prerequisites

* .NET 10 SDK
* Docker (for the integration tests and the full stack compose file)
* PowerShell (`pwsh`) if you want to run the Playwright UI tests

## Demo mode (no external services)

Demo mode uses SQLite, an in memory cache, and in process messaging. The database file and a small seed catalog
are created on startup.

```bash
dotnet run --project src/Commerce.Api
```

* Console: http://localhost:5080
* API reference (Scalar): http://localhost:5080/scalar
* OpenAPI document: http://localhost:5080/openapi/v1.json
* Health: http://localhost:5080/health

The SignalR live feed works in demo mode because the API pushes price changes directly. The Service Bus to
SignalR path in the Functions worker is the alternative production wiring.

## Full stack with SQL Server and Redis

```bash
docker compose up --build
```

This starts SQL Server, Redis Stack (which provides the RediSearch module), and the API configured to use them.
Open http://localhost:5080.

## Configuration

Settings are read from `appsettings.json` and environment variables. The provider switches are:

| Key | Values | Demo default |
| --- | --- | --- |
| `Database:Provider` | `SqlServer`, `Sqlite` | `Sqlite` |
| `Cache:Provider` | `Redis`, `InMemory` | `InMemory` |
| `Messaging:Provider` | `ServiceBus`, `InProcess` | `InProcess` |
| `ConnectionStrings:Commerce` | connection string | local SQLite file |
| `ConnectionStrings:Redis` | connection string | required when cache is Redis |
| `ConnectionStrings:ServiceBus` | connection string | required when messaging is Service Bus |

Override with double underscore environment variables, for example `Database__Provider=SqlServer`.

## Tests

```bash
dotnet test tests/Commerce.UnitTests
dotnet test tests/Commerce.IntegrationTests   # requires Docker
```

UI tests need a running instance and the Playwright browsers:

```bash
dotnet build -c Release
pwsh tests/Commerce.UiTests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Commerce.Api &
DEMO_BASE_URL=http://localhost:5080 dotnet test tests/Commerce.UiTests
```
