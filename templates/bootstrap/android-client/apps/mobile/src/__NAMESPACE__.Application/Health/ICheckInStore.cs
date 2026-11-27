using __NAMESPACE__.Domain.Values;

namespace __NAMESPACE__.Application.Health;

/// <summary>The typed store the Health screens read check-ins from. The store loads once at startup; screens show a loading state until it is ready.</summary>
public interface ICheckInStore
{
    /// <summary>Gets the check-ins the last load read, or an empty list before the first load.</summary>
    IReadOnlyList<CheckIn> Current { get; }

    /// <summary>Gets whether the store finished its first load.</summary>
    bool IsReady { get; }

    /// <summary>Loads the check-ins once, keeping the prior list when the load fails.</summary>
    /// <param name="cancellationToken">The token cancelling the load.</param>
    /// <returns>A task that completes when the load finishes.</returns>
    Task LoadAsync(CancellationToken cancellationToken);
}
