using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Commerce.Infrastructure.Messaging;

/// <summary>Demo-mode publisher. Logs events instead of putting them on a broker, so the API runs with no Azure dependency.</summary>
internal sealed class InProcessEventPublisher(ILogger<InProcessEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            logger.LogInformation("Domain event {EventType} {EventId} (in-process)", domainEvent.GetType().Name, domainEvent.EventId);
        }

        return Task.CompletedTask;
    }
}
