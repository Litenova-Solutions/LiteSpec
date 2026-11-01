namespace LiteSpec.Model.Checks;

/// <summary>One per-field erasure position differing from its class.</summary>
internal sealed record DataClassException(
    string Field,
    string? OnErasure,
    string? EnforcedBy,
    string? ReviewBy,
    IReadOnlyList<string> AcceptedBy);
