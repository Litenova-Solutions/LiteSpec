namespace LiteSpec.Model;

/// <summary>One code location no scenario test reaches, read from scenario-only coverage.</summary>
sealed record CoverageGap(string File, int Line, string Member);
