namespace LiteSpec.Authoring;

/// <summary>A fence-stripped line: its visible text, its 1-based number, and whether it sits inside a fence.</summary>
internal sealed record LineItem(string Line, int Number, bool Fenced);
