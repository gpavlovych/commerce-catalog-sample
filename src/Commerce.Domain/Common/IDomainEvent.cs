namespace Commerce.Domain.Common;

/// <summary>Marker for something that has happened in the domain and other parts of the system may react to.</summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}
