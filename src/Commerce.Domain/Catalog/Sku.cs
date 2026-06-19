using Commerce.Domain.Common;

namespace Commerce.Domain.Catalog;

/// <summary>Stock keeping unit. Trimmed, upper-cased, and constrained so it is safe as a natural key.</summary>
public readonly record struct Sku
{
    public Sku(string value)
    {
        var normalised = Guard.AgainstNullOrWhiteSpace(value, nameof(value)).ToUpperInvariant();
        if (normalised.Length > 64)
        {
            throw new ArgumentException("SKU cannot exceed 64 characters.", nameof(value));
        }

        Value = normalised;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static implicit operator string(Sku sku) => sku.Value;
}
