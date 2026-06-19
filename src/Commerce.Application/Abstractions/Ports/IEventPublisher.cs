using Commerce.Domain.Common;

namespace Commerce.Application.Abstractions.Ports;

/// <summary>
/// Publishes domain events to the rest of the system. Backed by Azure Service Bus in production.
/// Messages carry a stable id so consumers can deduplicate (see ADR 0003).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken);
}
