using Commerce.Domain.Catalog;
using Dapper;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// Creates the schema for whichever provider is configured and seeds a small demo catalog. Idempotent, so
/// it is safe to run on every startup. In production you would run real migrations instead; this keeps the
/// sample runnable with a single command.
/// </summary>
public sealed class DatabaseInitializer(ISqlConnectionFactory connectionFactory, IProductIndex? index = null)
{
    // Stable identifier so the demo catalog always references the same seeded supplier.
    private static readonly Guid DefaultSupplierId = new("0192b4d0-0000-7000-8000-0000000000a1");

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        foreach (var statement in SchemaStatements(connectionFactory.Provider))
        {
            await connection.ExecuteAsync(new CommandDefinition(statement, cancellationToken: cancellationToken));
        }

        await SeedAsync(connection, cancellationToken);

        if (index is not null)
        {
            await index.EnsureCreatedAsync(cancellationToken);
        }
    }

    private static IEnumerable<string> SchemaStatements(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer =>
        [
            """
            IF OBJECT_ID('Suppliers', 'U') IS NULL
            CREATE TABLE Suppliers (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
                Name nvarchar(200) NOT NULL,
                LeadTimeDays int NOT NULL
            );
            """,
            """
            IF OBJECT_ID('Products', 'U') IS NULL
            CREATE TABLE Products (
                Id uniqueidentifier NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
                Sku nvarchar(64) NOT NULL,
                Name nvarchar(200) NOT NULL,
                Description nvarchar(2000) NULL,
                PriceAmount decimal(18,2) NOT NULL,
                Currency char(3) NOT NULL,
                SupplierId uniqueidentifier NOT NULL,
                IsActive bit NOT NULL,
                CreatedAt datetimeoffset(7) NOT NULL,
                UpdatedAt datetimeoffset(7) NOT NULL,
                CONSTRAINT FK_Products_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
            );
            """,
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Products_Sku') CREATE UNIQUE INDEX UX_Products_Sku ON Products(Sku);",
            "IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Name') CREATE INDEX IX_Products_Name ON Products(Name);"
        ],
        _ =>
        [
            """
            CREATE TABLE IF NOT EXISTS Suppliers (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                LeadTimeDays INTEGER NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS Products (
                Id TEXT NOT NULL PRIMARY KEY,
                Sku TEXT NOT NULL,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                PriceAmount TEXT NOT NULL,
                Currency TEXT NOT NULL,
                SupplierId TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id)
            );
            """,
            "CREATE UNIQUE INDEX IF NOT EXISTS UX_Products_Sku ON Products(Sku);",
            "CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);"
        ]
    };

    private static async Task SeedAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
    {
        var supplierExists = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            SqlScripts.SupplierExists, new { Id = DefaultSupplierId }, cancellationToken: cancellationToken));

        if (supplierExists == 0)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO Suppliers (Id, Name, LeadTimeDays) VALUES (@Id, @Name, @LeadTimeDays);",
                new { Id = DefaultSupplierId, Name = "Baltic Components", LeadTimeDays = 14 },
                cancellationToken: cancellationToken));
        }

        var productCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            "SELECT CAST(COUNT(1) AS BIGINT) FROM Products;", cancellationToken: cancellationToken));

        if (productCount > 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        (string Sku, string Name, decimal Price)[] seed =
        [
            ("AL-6061-PLATE", "Aluminium 6061 Plate 10mm", 42.50m),
            ("SS-316L-ROD", "Stainless 316L Rod 12mm", 18.90m),
            ("TI-GR5-SHEET", "Titanium Grade 5 Sheet 2mm", 210.00m)
        ];

        foreach (var item in seed)
        {
            var product = Product.Create(
                Guid.CreateVersion7(), item.Sku, item.Name, null, item.Price, "EUR", DefaultSupplierId, now);

            if (product.IsFailure)
            {
                continue;
            }

            var p = product.Value;
            await connection.ExecuteAsync(new CommandDefinition(SqlScripts.Insert, new
            {
                p.Id,
                Sku = p.Sku.Value,
                p.Name,
                p.Description,
                PriceAmount = p.Price.Amount,
                Currency = p.Price.Currency,
                p.SupplierId,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            }, cancellationToken: cancellationToken));
        }
    }
}
