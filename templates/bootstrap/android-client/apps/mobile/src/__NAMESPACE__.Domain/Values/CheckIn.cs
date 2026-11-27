namespace __NAMESPACE__.Domain.Values;

/// <summary>One recorded health check-in, as the Health sample speaks of it.</summary>
public sealed class CheckIn
{
    /// <summary>Initializes a new instance of the <see cref="CheckIn"/> class.</summary>
    /// <param name="id">The check-in identifier the backend sample assigned.</param>
    /// <param name="state">The lifecycle state of the check-in.</param>
    public CheckIn(string id, CheckInState state)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentNullException.ThrowIfNull(state);
        Id = id;
        State = state;
    }

    /// <summary>Gets the check-in identifier the backend sample assigned.</summary>
    public string Id { get; }

    /// <summary>Gets the lifecycle state of the check-in.</summary>
    public CheckInState State { get; }
}
