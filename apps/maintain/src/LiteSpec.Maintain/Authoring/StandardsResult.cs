namespace LiteSpec.Authoring;

/// <summary>
///     The repository-wide result: the sorted diagnostics, or a usage error when the root cannot be read.
/// </summary>
internal sealed record StandardsResult(IReadOnlyList<StandardsDiagnostic> Diagnostics, string? UsageError);
