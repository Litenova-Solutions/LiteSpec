namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>One aggregate data class: its classification, erasure position, retention period and per-field exceptions.</summary>
internal sealed record DataClassPosition(
    string Name,
    string Classification,
    string? OnErasure,
    IReadOnlyList<string> EnforcedBy,
    IReadOnlyList<string> Fields,
    IReadOnlyList<DataClassException> Exceptions,
    string? Period,
    string? PeriodStartsAt,
    IReadOnlyList<string> Basis);
