using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Commerce.Application.Abstractions.Ports;
using Commerce.Domain.Common;

namespace Commerce.Infrastructure.Messaging;

/// <summary>
/// Publishes domain events to an Azure Service Bus topic. MessageId is set to the event id so a topic with
/// duplicate detection enabled drops re-sends, and consumers can deduplicate on the same value (ADR 0003).
/// </summary>
internal sealed class ServiceBusEventPublisher(ServiceBusClient client, string topicName) : IEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        var sender = client.CreateSender(topicName);
        try
        {
            var messages = events.Select(ToMessage).ToList();
            await sender.SendMessagesAsync(messages, cancellationToken);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    private static ServiceBusMessage ToMessage(IDomainEvent domainEvent)
    {
        var body = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
        return new ServiceBusMessage(body)
        {
            MessageId = domainEvent.EventId.ToString(),
            Subject = domainEvent.GetType().Name,
            ContentType = "application/json",
            ApplicationProperties = { ["eventType"] = domainEvent.GetType().Name }
        };
    }
}
