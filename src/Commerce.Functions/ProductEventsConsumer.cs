using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Commerce.Domain.Catalog.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Extensions.Logging;

namespace Commerce.Functions;

/// <summary>
/// Consumes catalog events from Service Bus and fans the price change out to SignalR clients. The topic
/// has duplicate detection enabled and the publisher sets MessageId to the event id, so a redelivered
/// message is dropped before it reaches this function (ADR 0003). The handler is also written to be safe
/// to run more than once: it only reads the message and pushes a notification, with no side effects to undo.
/// </summary>
public sealed class ProductEventsConsumer(ILogger<ProductEventsConsumer> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(ProductEventsConsumer))]
    [SignalROutput(HubName = "prices", ConnectionStringSetting = "AzureSignalRConnectionString")]
    public SignalRMessageAction? Run(
        [ServiceBusTrigger("%Messaging:Topic%", "forecasting", Connection = "ServiceBus")] ServiceBusReceivedMessage message)
    {
        if (!string.Equals(message.Subject, nameof(ProductPriceChanged), StringComparison.Ordinal))
        {
            logger.LogInformation("Ignoring event {Subject} {MessageId}", message.Subject, message.MessageId);
            return null;
        }

        var change = JsonSerializer.Deserialize<ProductPriceChanged>(message.Body.ToString(), JsonOptions);
        if (change is null)
        {
            logger.LogWarning("Could not deserialize ProductPriceChanged from message {MessageId}", message.MessageId);
            return null;
        }

        logger.LogInformation("Forwarding price change for {Sku} to realtime clients", change.Sku);

        return new SignalRMessageAction("priceChanged")
        {
            Arguments = [new { change.ProductId, change.Sku, change.OldAmount, change.NewAmount, change.Currency }]
        };
    }
}
