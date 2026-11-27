namespace __NAMESPACE__.Health.Domain.CheckIns.Parts;

/// <summary>The recorded state: the check-in is stored with its time and note.</summary>
public sealed record RecordedState : CheckInState
{
    /// <summary>The stored code of the recorded state.</summary>
    public const string RecordedCode = "recorded";

    /// <inheritdoc />
    public override string Code => RecordedCode;
}
