namespace LiteSpec.Cli.Rules;

/// <summary>
///     A glob over consumer-relative paths: <c>*</c> crosses no folder and
///     <c>**</c> crosses folders. Convention scopes and departure paths use it.
/// </summary>
/// <follows>litespec/core/agent#load-applicable-provisions-before-editing</follows>
internal static class Glob
{
    /// <summary>Whether the glob matches the path.</summary>
    /// <param name="glob">The glob, with forward slashes.</param>
    /// <param name="path">The path with forward slashes.</param>
    /// <returns>True when the glob matches the whole path.</returns>
    public static bool Matches(string glob, string path)
    {
        var pattern = ToRegex(glob.Trim().Replace('\\', '/'));
        return System.Text.RegularExpressions.Regex.IsMatch(
            path.Replace('\\', '/'), pattern, System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }

    /// <summary>Turns a glob into an anchored regular expression.</summary>
    /// <param name="glob">The glob with forward slashes.</param>
    /// <returns>The anchored expression.</returns>
    static string ToRegex(string glob)
    {
        var builder = new System.Text.StringBuilder("^");

        for (var index = 0; index < glob.Length; index++)
        {
            if (glob[index] == '*' && index + 1 < glob.Length && glob[index + 1] == '*')
            {
                builder.Append(".*");
                index++;

                if (index + 1 < glob.Length && glob[index + 1] == '/')
                {
                    index++;
                }
            }
            else if (glob[index] == '*')
            {
                builder.Append("[^/]*");
            }
            else
            {
                builder.Append(System.Text.RegularExpressions.Regex.Escape(glob[index].ToString()));
            }
        }

        builder.Append('$');
        return builder.ToString();
    }
}
