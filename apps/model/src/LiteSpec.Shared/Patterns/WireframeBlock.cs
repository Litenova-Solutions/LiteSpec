namespace LiteSpec.Patterns;

/// <summary>One wireframe block as written: its text and the page line its first row is on.</summary>
/// <param name="Text">The block body.</param>
/// <param name="Line">The 1-based page line of the opening fence.</param>
public sealed record WireframeBlock(string Text, int Line)
{
    /// <summary>Gets the parsed block.</summary>
    public Wireframe Parsed { get; } = WireframeParser.Parse(Text);
}
