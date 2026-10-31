namespace LiteSpec.Model.Readers.Coverage;

/// <summary>What the coverage read found: unreached lines and whether any file was read.</summary>
/// <param name="Gaps">One entry per line no scenario test reaches.</param>
/// <param name="Read">Whether at least one coverage file was read.</param>
sealed record CoverageRead(IReadOnlyList<CoverageGap> Gaps, bool Read);
