namespace LiteSpec.Cli.Model.Checks;

/// <summary>One data class erasure position with its per-field exceptions.</summary>
internal sealed record DataClassPosition(
    string Name,
    string Classification,
    string? OnErasure,
    string? EnforcedBy,
    IReadOnlyList<string> Fields,
    IReadOnlyList<DataClassException> Exceptions);
