using Commerce.Domain.Common;

namespace Commerce.Domain.Catalog.Events;

/// <summary>
/// Raised when a product price changes. Carries both prices so downstream consumers
/// (forecasting, marketing analytics, the realtime UI) do not have to read the old value back.
/// </summary>
public sealed record ProductPriceChanged(
    Guid ProductId,
    string Sku,
    decimal OldAmount,
    decimal NewAmount,
    string Currency) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
