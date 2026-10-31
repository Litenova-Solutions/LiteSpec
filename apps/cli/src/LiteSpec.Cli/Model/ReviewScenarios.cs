namespace LiteSpec.Model;

/// <summary>One record's scenario evidence in the review queue.</summary>
/// <param name="Total">The scenarios counted for the record.</param>
/// <param name="Confirmed">The counted scenarios with a confirmed proof.</param>
/// <param name="Passing">The counted scenarios with a passing, current proof.</param>
/// <param name="Stale">The counted scenarios whose proof went stale.</param>
sealed record ReviewScenarios(int Total, int Confirmed, int Passing, int Stale);
