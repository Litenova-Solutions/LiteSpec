namespace LiteSpec.Patterns;

/// <summary>
///     One part of a wireframe: its name, modifiers, visible copy and children.
///     The language is defined in <c>docs/frontend/patterns.md</c>.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
public sealed class WireframeNode
{
    /// <summary>Gets the part name, such as <c>header</c> or <c>action</c>.</summary>
    public required string Part { get; init; }

    /// <summary>Gets the bare words after the part name, such as <c>primary</c> or <c>positive</c>.</summary>
    public List<string> Modifiers { get; } = [];

    /// <summary>Gets the quoted strings, in order: the visible copy.</summary>
    public List<string> Strings { get; } = [];

    /// <summary>Gets or sets the value after <c>=</c>, for a field that shows one.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the bare integer, for a collection.</summary>
    public int? Count { get; set; }

    /// <summary>Gets or sets the width ratio, for a split.</summary>
    public List<int>? Ratio { get; set; }

    /// <summary>Gets the children, indented or inline.</summary>
    public List<WireframeNode> Children { get; } = [];

    /// <summary>Gets or sets a value indicating whether true when the children came inline after <c>:</c>, so they lay out side by side.</summary>
    public bool InlineChildren { get; set; }

    /// <summary>Gets the 1-based line inside the block.</summary>
    public int Line { get; init; }

    /// <summary>Gets the first quoted string, or empty.</summary>
    public string Text => Strings.Count > 0 ? Strings[0] : "";

    /// <summary>Whether the part carries the modifier.</summary>
    /// <param name="modifier">The modifier to test.</param>
    /// <returns>True when the part carries the modifier.</returns>
    public bool Has(string modifier) => Modifiers.Contains(modifier);

    /// <summary>Every node below this one, depth first.</summary>
    /// <returns>Every descendant node, depth first.</returns>
    public IEnumerable<WireframeNode> Descendants()
    {
        foreach (var child in Children)
        {
            yield return child;

            foreach (var below in child.Descendants())
            {
                yield return below;
            }
        }
    }
}
