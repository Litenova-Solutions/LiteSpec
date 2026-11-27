namespace __NAMESPACE__.Domain.Values;

/// <summary>The recorded state: the check-in is stored as written.</summary>
public sealed record RecordedState : CheckInState
{
    /// <summary>The stored code of the recorded state.</summary>
    public const string RecordedCode = "recorded";

    /// <inheritdoc />
    public override string Code => RecordedCode;
}
