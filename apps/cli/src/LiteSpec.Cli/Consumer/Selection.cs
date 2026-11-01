namespace LiteSpec.Consumer;

/// <summary>
///     One extension selection: a bare id string normalizes to a selection carrying only the id.
/// </summary>
/// <param name="Id">The selected extension id, or null when the entry carries none.</param>
/// <param name="IdText">How the id reads inside a message.</param>
/// <param name="ReviewBy">The recorded review date, or null when none is recorded.</param>
/// <param name="ReleaseStage">The recorded release stage, or null when none is recorded.</param>
sealed record Selection(string? Id, string IdText, string? ReviewBy, string? ReleaseStage);
