namespace LiteSpec.Model.Diff;

/// <summary>One parsed data class: its classification, purpose and erasure position.</summary>
/// <param name="Classification">The classification, as written.</param>
/// <param name="Purpose">The purpose, as written.</param>
/// <param name="OnErasure">The erasure position, as written.</param>
sealed record DataClassValue(string? Classification, string? Purpose, string? OnErasure);
