namespace LiteSpec.Patterns;

/// <summary>One pattern: a <c>###</c> heading in the page's Patterns section.</summary>
/// <param name="Name">The pattern name.</param>
/// <param name="Slug">The heading slug.</param>
/// <param name="Line">The 1-based page line of the pattern heading.</param>
public sealed record Pattern(string Name, string Slug, int Line)
{
    /// <summary>Gets the pattern's fields by name.</summary>
    public Dictionary<string, string> Fields { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets the pattern's options in page order.</summary>
    public List<PatternOption> Options { get; } = [];

    /// <summary>Gets the option id named by <c>**Default:**</c>, without backticks.</summary>
    public string? Default => Fields.TryGetValue("Default", out var value) ? value.Trim().Trim('`') : null;
}
