namespace LiteSpec.Model.Checks;

/// <summary>One data class erasure position with its per-field exceptions.</summary>
internal sealed record DataClassPosition(
    string Name,
    string Classification,
    string? OnErasure,
    string? EnforcedBy,
    string? ReviewBy,
    IReadOnlyList<string> AcceptedBy,
    IReadOnlyList<string> Fields,
    IReadOnlyList<DataClassException> Exceptions);
