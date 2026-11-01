namespace LiteSpec.Model.Readers.Operations;

/// <summary>One alert rule: its name, labels and own text.</summary>
/// <param name="Name">The rule name from its alert or title key.</param>
/// <param name="File">The absolute file path.</param>
/// <param name="Line">The 1-based line of the rule item.</param>
/// <param name="Labels">The rule labels by key.</param>
/// <param name="Text">The rule's own lines, for the threshold comparison.</param>
sealed record AlertRule(string Name, string File, int Line, IReadOnlyDictionary<string, string> Labels, string Text);
