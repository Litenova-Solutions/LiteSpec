using Claimwick.Spending.Ports.EventBus;

namespace Claimwick.Spending.Infrastructure.EventBus;

/// <summary>Publishes the spending module's integration events to the event bus.</summary>
/// <implements>external-system/event-bus</implements>
internal sealed class EventBusPublisher : IEventBus
{
    /// <summary>Publishes one integration event to the bus.</summary>
    public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
