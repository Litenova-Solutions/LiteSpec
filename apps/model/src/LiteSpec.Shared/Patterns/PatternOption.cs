namespace LiteSpec.Patterns;

/// <summary>One option of a pattern.</summary>
/// <param name="Id">The option identifier.</param>
/// <param name="Name">The option name.</param>
/// <param name="Line">The 1-based page line of the option heading.</param>
public sealed record PatternOption(string Id, string Name, int Line)
{
    /// <summary>Gets or sets the option's wireframe block.</summary>
    public WireframeBlock? Wireframe { get; set; }
    /// <summary>Gets or sets the option's compact wireframe block.</summary>
    public WireframeBlock? Compact { get; set; }
    /// <summary>Gets or sets how many wireframe blocks the option carries.</summary>
    public int WireframeCount { get; set; }
    /// <summary>Gets or sets how many compact blocks the option carries.</summary>
    public int CompactCount { get; set; }
    /// <summary>Gets the option's fields by name.</summary>
    public Dictionary<string, string> Fields { get; } = new(StringComparer.Ordinal);
}
