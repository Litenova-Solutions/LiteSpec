namespace LiteSpec.Model;

/// <summary>One step call inside a scenario.</summary>
sealed record ScenarioStep(string Phase, string Receiver, string? ReceiverType, string Method)
{
    /// <summary>Gets the string literals the step passes, in order, such as the record kind a probe step asks for.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];
}
