using System.Text.RegularExpressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The link checks: every Markdown link in an explanation file or a document resolves to a file or a record.</summary>
/// <follows>litespec/core/model#resolve-every-page-link</follows>
internal static class LinkChecks
{
    /// <summary>Reports link-unknown.</summary>
    /// <param name="model">The loaded project model that holds every explanation and document text and every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var found = new System.Collections.Concurrent.ConcurrentBag<Finding>();
        var byId = model.ById;

        Parallel.ForEach(model.Bodies, body =>
        {
            var text = WithoutCode(body.Body);
            var starts = LineStarts(text);
            var folder = Path.GetDirectoryName(body.File) ?? ".";

            foreach (Match match in MarkdownLink.Matches(text))
            {
                var target = match.Groups["target"].Value.Trim('<', '>');

                if (target.Length == 0 || target.StartsWith('#') || UriScheme.IsMatch(target))
                {
                    continue;
                }

                if (IsRecordTarget(target))
                {
                    if (!byId.ContainsKey(Identifiers.BaseId(target)))
                    {
                        found.Add(new Finding("link-unknown", $"Link '{target}' resolves to no record.", body.File, body.BodyStartLine + LineOf(starts, match.Index) - 1, body.PageId));
                    }

                    continue;
                }

                var path = Uri.UnescapeDataString(target.Split('#')[0].Split('?')[0]);
                var resolved = Path.GetFullPath(Path.Combine(folder, path.Replace('/', Path.DirectorySeparatorChar)));

                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    found.Add(new Finding("link-unknown", $"Link '{target}' resolves to no file.", body.File, body.BodyStartLine + LineOf(starts, match.Index) - 1, body.PageId));
                }
            }
        });

        findings.AddRange(found);
    }

    /// <summary>Whether a link target names a record: the identifier grammar, optionally with a step fragment.</summary>
    /// <param name="target">The link target as written, without surrounding angle brackets.</param>
    /// <returns>True when the target names a record identifier rather than a file path.</returns>
    internal static bool IsRecordTarget(string target)
    {
        var hash = target.IndexOf('#');
        var baseTarget = hash < 0 ? target : target[..hash];

        if (baseTarget.Length == 0
            || baseTarget.StartsWith('.')
            || baseTarget.StartsWith('/')
            || baseTarget.Contains('?')
            || baseTarget.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            || baseTarget.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
            || baseTarget.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Identifiers.TrySplit(baseTarget, out var prefix, out _, out _)
            && RecordTypes.All.Contains(prefix);
    }

    /// <summary>Blanks fenced and inline code, keeping offsets and line breaks, so links in examples are not read.</summary>
    /// <param name="text">The Markdown text whose code spans are blanked.</param>
    /// <returns>The text with fenced and inline code replaced by spaces, keeping every offset and line break.</returns>
    internal static string WithoutCode(string text)
    {
        static string Blank(Match match) => Regex.Replace(match.Value, @"[^\r\n]", " ");

        var fenced = FencedCode.Replace(text, Blank);
        return InlineCode.Replace(fenced, Blank);
    }

    /// <summary>Finds each Markdown link and image target in explanation and document text.</summary>
    private static readonly Regex MarkdownLink = new(@"!?\[[^\]\r\n]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)", RegexOptions.Compiled);

    /// <summary>Matches a URI scheme prefix, so external targets are left unchecked.</summary>
    private static readonly Regex UriScheme = new(@"^[a-zA-Z][a-zA-Z0-9+.-]*:", RegexOptions.Compiled);

    /// <summary>Matches one fenced code block from its opening fence to its closing fence.</summary>
    private static readonly Regex FencedCode = new(@"(?ms)^[ \t]*(```|~~~).*?^[ \t]*\1[^\r\n]*$", RegexOptions.Compiled);

    /// <summary>Matches one inline code span delimited by single backticks.</summary>
    private static readonly Regex InlineCode = new(@"`[^`\r\n]*`", RegexOptions.Compiled);
}
