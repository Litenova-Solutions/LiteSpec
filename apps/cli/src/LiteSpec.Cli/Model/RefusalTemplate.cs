namespace LiteSpec.Model;

/// <summary>
///     A refusal code the project composes at run time from a key, such as <c>uniqueness.{key}-taken</c> for the
///     conflict a commit answers when an aggregate loses a uniqueness key.
/// </summary>
/// <remarks>
///     No literal in the code spells the composed code, so the checks read it through its template: the text
///     before the first dot is a refusal prefix, and a code matching the template is raised where its key is a
///     literal in the code.
/// </remarks>
/// <follows>litespec/core/model#compute-proof-statuses-in-the-builder</follows>
internal static class RefusalTemplate
{
    /// <summary>The placeholder a template carries exactly once.</summary>
    public const string Placeholder = "{key}";

    /// <summary>Whether a template carries one placeholder after a prefix and a dot.</summary>
    /// <param name="template">The declared template.</param>
    /// <returns>True when the checks can read codes through it.</returns>
    public static bool IsWellFormed(string template)
    {
        var at = template.IndexOf(Placeholder, StringComparison.Ordinal);
        var dot = template.IndexOf('.');

        return at > 0
            && dot > 0
            && dot < at
            && template.IndexOf(Placeholder, at + Placeholder.Length, StringComparison.Ordinal) < 0;
    }

    /// <summary>The prefix a template's codes carry, the text before its first dot.</summary>
    /// <param name="template">A well-formed template.</param>
    /// <returns>The prefix.</returns>
    public static string PrefixOf(string template) => template[..template.IndexOf('.')];

    /// <summary>The key a code carries in place of the template's placeholder, when the code matches it.</summary>
    /// <param name="template">A well-formed template.</param>
    /// <param name="code">The refusal code.</param>
    /// <param name="key">The text in place of the placeholder.</param>
    /// <returns>True when the code matches the template with a non-empty key.</returns>
    public static bool TryMatch(string template, string code, out string key)
    {
        var at = template.IndexOf(Placeholder, StringComparison.Ordinal);
        var before = template[..at];
        var after = template[(at + Placeholder.Length)..];

        if (code.Length > before.Length + after.Length
            && code.StartsWith(before, StringComparison.Ordinal)
            && code.EndsWith(after, StringComparison.Ordinal))
        {
            key = code[before.Length..^after.Length];
            return true;
        }

        key = "";
        return false;
    }
}
