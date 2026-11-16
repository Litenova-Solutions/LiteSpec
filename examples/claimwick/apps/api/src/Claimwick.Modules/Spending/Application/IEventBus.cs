namespace Claimwick.Spending.Application;

/// <summary>The event bus carrying the spending module's expense-submitted event to the readers outside the product.</summary>
/// <implements>external-system/event-bus</implements>
public interface IEventBus
{
    /// <summary>Publishes one integration event to the bus.</summary>
    Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}
