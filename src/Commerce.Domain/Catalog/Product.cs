using Commerce.Domain.Catalog.Events;
using Commerce.Domain.Common;

namespace Commerce.Domain.Catalog;

/// <summary>
/// Product is the aggregate root of the catalog. It owns its own rules: a product always has a
/// non-empty name, a valid SKU, and a non-negative price, and a price change is only recorded
/// (and only emits an event) when the amount actually moves.
/// </summary>
public sealed class Product : Entity
{
    private Product(Guid id, Sku sku, string name, string? description, Money price, Guid supplierId, bool isActive, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id)
    {
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        SupplierId = supplierId;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Sku Sku { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public Guid SupplierId { get; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Product> Create(
        Guid id,
        string sku,
        string name,
        string? description,
        decimal priceAmount,
        string currency,
        Guid supplierId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Product>(Error.Validation("Product name is required."));
        }

        if (supplierId == Guid.Empty)
        {
            return Result.Failure<Product>(Error.Validation("A product must belong to a supplier."));
        }

        try
        {
            var product = new Product(
                id,
                new Sku(sku),
                name.Trim(),
                string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                new Money(priceAmount, currency),
                supplierId,
                isActive: true,
                createdAt: now,
                updatedAt: now);

            return product;
        }
        catch (ArgumentException ex)
        {
            // Value object construction failed: turn the invariant breach into a domain validation error.
            return Result.Failure<Product>(Error.Validation(ex.Message));
        }
    }

    /// <summary>
    /// Changes the selling price. Returns success without an event when the price is unchanged,
    /// so consumers never see spurious "price changed" notifications.
    /// </summary>
    public Result ChangePrice(decimal newAmount, DateTimeOffset now)
    {
        Money next;
        try
        {
            next = Price.WithAmount(newAmount);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(Error.Validation(ex.Message));
        }

        if (next == Price)
        {
            return Result.Success();
        }

        var previous = Price;
        Price = next;
        UpdatedAt = now;

        Raise(new ProductPriceChanged(Id, Sku, previous.Amount, next.Amount, next.Currency));
        return Result.Success();
    }

    public void Rename(string name, DateTimeOffset now)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = now;
    }

    // Reconstruction from persistence. The stored row was valid when written, so creation rules are skipped.
    public static Product Rehydrate(
        Guid id,
        string sku,
        string name,
        string? description,
        decimal priceAmount,
        string currency,
        Guid supplierId,
        bool isActive,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt) =>
        new(id, new Sku(sku), name, description, new Money(priceAmount, currency), supplierId, isActive, createdAt, updatedAt);
}
