using Commerce.Domain.Common;

namespace Commerce.Domain.Catalog;

/// <summary>A supplier of products. Lead time feeds the procurement and forecasting side of the platform.</summary>
public sealed class Supplier : Entity
{
    private Supplier(Guid id, string name, int leadTimeDays)
        : base(id)
    {
        Name = name;
        LeadTimeDays = leadTimeDays;
    }

    public string Name { get; private set; }
    public int LeadTimeDays { get; private set; }

    public static Result<Supplier> Create(Guid id, string name, int leadTimeDays)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Supplier>(Error.Validation("Supplier name is required."));
        }

        if (leadTimeDays is < 0 or > 365)
        {
            return Result.Failure<Supplier>(Error.Validation("Lead time must be between 0 and 365 days."));
        }

        return new Supplier(id, name.Trim(), leadTimeDays);
    }

    // Reconstruction from persistence. Bypasses creation rules because the data was already valid when stored.
    public static Supplier Rehydrate(Guid id, string name, int leadTimeDays) => new(id, name, leadTimeDays);
}
