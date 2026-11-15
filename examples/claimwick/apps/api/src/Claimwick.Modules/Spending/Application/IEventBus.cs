namespace Claimwick.Spending.Application;

/// <summary>The event bus carrying the spending module's integration events to the modules that read them.</summary>
/// <implements>external-system/event-bus</implements>
public interface IEventBus
{
    /// <summary>Publishes one integration event to the bus.</summary>
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
