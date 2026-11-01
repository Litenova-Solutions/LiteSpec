namespace LiteSpec.Authoring;

/// <summary>An H2 section: its anchor-stripped name and its 1-based line.</summary>
internal sealed record PageSection(string Name, int Line);
