namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>One per-field erasure position differing from its class.</summary>
internal sealed record DataClassException(
    string Field,
    string? OnErasure,
    string? EnforcedBy);
