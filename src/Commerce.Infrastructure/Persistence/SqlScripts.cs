namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// Hand-written SQL kept in one place. Reads use an explicit column list (never SELECT *) so the
/// row shape is stable and obvious. The only dialect split is the row-limit syntax in search.
/// </summary>
internal static class SqlScripts
{
    public const string Columns =
        "Id, Sku, Name, Description, PriceAmount, Currency, SupplierId, IsActive, CreatedAt, UpdatedAt";

    public const string Insert = $"""
        INSERT INTO Products ({Columns})
        VALUES (@Id, @Sku, @Name, @Description, @PriceAmount, @Currency, @SupplierId, @IsActive, @CreatedAt, @UpdatedAt);
        """;

    public const string UpdatePrice = """
        UPDATE Products
        SET PriceAmount = @PriceAmount, UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    public const string GetById = $"SELECT {Columns} FROM Products WHERE Id = @Id;";

    public const string ExistsBySku =
        "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM Products WHERE Sku = @Sku) THEN 1 ELSE 0 END AS BIGINT);";

    public const string GetByIds = $"SELECT {Columns} FROM Products WHERE Id IN @Ids;";

    public static string Search(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => $"""
            SELECT TOP (@Take) {Columns}
            FROM Products
            WHERE @Term = '' OR Name LIKE @Like OR Sku LIKE @Like
            ORDER BY Name;
            """,
        _ => $"""
            SELECT {Columns}
            FROM Products
            WHERE @Term = '' OR Name LIKE @Like OR Sku LIKE @Like
            ORDER BY Name
            LIMIT @Take;
            """
    };

    public const string SupplierExists =
        "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM Suppliers WHERE Id = @Id) THEN 1 ELSE 0 END AS BIGINT);";
}
