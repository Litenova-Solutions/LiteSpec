namespace LiteSpec.Maintain.Validate.Pages;

/// <summary>A fence-stripped line: its visible text, its 1-based number, and whether it sits inside a fence.</summary>
internal sealed record PageLine(string Line, int Number, bool Fenced);
