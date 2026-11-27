namespace __NAMESPACE__.Health.Domain.HealthChecks.Parts;

/// <summary>
/// The recorded state: the check-in is stored with its time and note.
/// </summary>
internal sealed record RecordedState : HealthCheckState
{
    /// <summary>The stored code of the recorded state.</summary>
    public const string RecordedCode = "recorded";

    /// <inheritdoc />
    public override string Code => RecordedCode;
}
