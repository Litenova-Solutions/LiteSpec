using System.Text.RegularExpressions;

namespace LiteSpec.Model.Readers.Suppressions;

/// <summary>
///     Reads every lowering or suppression of a LiteSpec analyzer rule under the
///     project root: <c>dotnet_diagnostic.LSxxxx.severity</c> entries below the
///     error default in every <c>.editorconfig</c> and <c>.globalconfig</c>, every
///     LS rule named in a <c>NoWarn</c> element of <c>Directory.Build.props</c> or a
///     project file, and every LS rule named by <c>#pragma warning disable</c> or
///     <c>SuppressMessage</c> in a C# source file. Build output, installed packages
///     and tool folders are left out. A file that cannot be read states nothing.
/// </summary>
/// <follows>litespec/core/model#record-a-project-wide-lowering-as-an-exception</follows>
static class SuppressionReader
{
    /// <summary>Directory names the walk leaves out: build output, installed packages and tool folders.</summary>
    private static readonly HashSet<string> Skipped = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git",
        ".svelte-kit", "build", "dist", "coverage",
    };

    /// <summary>One analyzer rule identifier, such as LS1001.</summary>
    private static readonly Regex RuleId = new(@"LS\d{4}", RegexOptions.Compiled);

    /// <summary>One dotnet_diagnostic severity entry for an analyzer rule in an editorconfig or globalconfig file.</summary>
    private static readonly Regex SeverityEntry = new(
        @"dotnet_diagnostic\.(LS\d{4})\.severity\s*=\s*(?<severity>[A-Za-z]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>One NoWarn element in a build props or project file.</summary>
    private static readonly Regex NoWarnElement = new(
        @"<NoWarn\b[^>]*>(?<text>.*?)</NoWarn\s*>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>One pragma warning disable directive in a C# source file.</summary>
    private static readonly Regex PragmaDisable = new(
        @"#pragma\s+warning\s+disable\b(?<text>.*)",
        RegexOptions.Compiled);

    /// <summary>One SuppressMessage attribute in a C# source file, across its argument list.</summary>
    private static readonly Regex SuppressMessageAttribute = new(
        @"SuppressMessage\s*\((?<args>[^()]*)\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>The severities below the error default every LiteSpec rule reports at: a rule lowered to one of these is lowered.</summary>
    private static readonly HashSet<string> LoweredSeverities = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "silent", "suggestion", "warning",
    };

    /// <summary>Reads every lowering or suppression of a LiteSpec analyzer rule under the project root, in file, line and rule order.</summary>
    /// <param name="root">The absolute project root to read under.</param>
    /// <returns>Every lowering or suppression under the root, in file, line and rule order; empty when the root does not exist.</returns>
    public static IReadOnlyList<AnalyzerSuppression> Read(string root)
    {
        var suppressions = new List<AnalyzerSuppression>();

        foreach (var file in Files(root))
        {
            string text;

            try
            {
                text = File.ReadAllText(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var name = Path.GetFileName(file);

            if (name.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase))
            {
                ReadSeverityEntries(
                    file,
                    name.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase) ? ".globalconfig" : ".editorconfig",
                    text,
                    suppressions);
            }
            else if (name.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                ReadNoWarn(file, text, suppressions);
            }
            else if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                ReadSource(file, text, suppressions);
            }
        }

        suppressions.Sort((left, right) =>
        {
            var file = string.Compare(left.File, right.File, StringComparison.Ordinal);

            if (file != 0)
            {
                return file;
            }

            var line = left.Line.CompareTo(right.Line);
            return line != 0 ? line : string.Compare(left.Rule, right.Rule, StringComparison.Ordinal);
        });

        return suppressions;
    }

    /// <summary>Every candidate file under the root: config, build and C# source files outside the skipped folders, in ordinal path order.</summary>
    /// <param name="root">The absolute project root to read under.</param>
    /// <returns>Every candidate file under the root in ordinal path order, or an empty list when the root does not exist.</returns>
    private static List<string> Files(string root)
    {
        if (root.Length == 0 || !Directory.Exists(root))
        {
            return [];
        }

        // Walk folder by folder and never descend into a skipped folder, so a large tree with
        // node_modules or an unreadable entry still yields every config and source file.
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false };

        while (pending.Count > 0)
        {
            var folder = pending.Pop();

            try
            {
                files.AddRange(Directory.EnumerateFiles(folder, "*", options).Where(IsCandidate));

                foreach (var child in Directory.EnumerateDirectories(folder, "*", options))
                {
                    if (!Skipped.Contains(Path.GetFileName(child)))
                    {
                        pending.Push(child);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A folder that cannot be read holds no readable lowering; the rest of the tree still counts.
            }
        }

        files.Sort(StringComparer.Ordinal);

        return files;
    }

    /// <summary>Whether the file can state a lowering or suppression: an editorconfig, a globalconfig, Directory.Build.props, a project file or a C# source file.</summary>
    /// <param name="file">The file path to test.</param>
    /// <returns>True when the file can state a lowering or suppression; otherwise false.</returns>
    private static bool IsCandidate(string file)
    {
        var name = Path.GetFileName(file);

        return name.EndsWith(".editorconfig", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Adds every rule an editorconfig or globalconfig file lowers below error, one per entry and rule.</summary>
    /// <param name="file">The config file path recorded on its suppressions.</param>
    /// <param name="kind">The config file name recorded as the kind: .editorconfig or .globalconfig.</param>
    /// <param name="text">The full text of the config file.</param>
    /// <param name="suppressions">The suppressions collected so far, added to in place.</param>
    private static void ReadSeverityEntries(string file, string kind, string text, List<AnalyzerSuppression> suppressions)
    {
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index].Trim();

            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            var match = SeverityEntry.Match(line);

            if (match.Success && LoweredSeverities.Contains(match.Groups["severity"].Value))
            {
                suppressions.Add(new AnalyzerSuppression(
                    match.Groups[1].Value.ToUpperInvariant(),
                    kind.ToLowerInvariant(),
                    match.Groups["severity"].Value.ToLowerInvariant(),
                    file,
                    index + 1));
            }
        }
    }

    /// <summary>Adds every rule a NoWarn element names, one per element and rule.</summary>
    /// <param name="file">The build file path recorded on its suppressions.</param>
    /// <param name="text">The full text of the build file.</param>
    /// <param name="suppressions">The suppressions collected so far, added to in place.</param>
    private static void ReadNoWarn(string file, string text, List<AnalyzerSuppression> suppressions)
    {
        foreach (Match element in NoWarnElement.Matches(text))
        {
            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match rule in RuleId.Matches(element.Groups["text"].Value))
            {
                if (named.Add(rule.Value.ToUpperInvariant()))
                {
                    suppressions.Add(new AnalyzerSuppression(
                        rule.Value.ToUpperInvariant(),
                        "NoWarn",
                        "",
                        file,
                        LineOf(text, element.Index)));
                }
            }
        }
    }

    /// <summary>Adds every rule a pragma warning disable or a SuppressMessage names in a C# source file.</summary>
    /// <param name="file">The source file path recorded on its suppressions.</param>
    /// <param name="text">The full text of the source file.</param>
    /// <param name="suppressions">The suppressions collected so far, added to in place.</param>
    private static void ReadSource(string file, string text, List<AnalyzerSuppression> suppressions)
    {
        var lines = text.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            var match = PragmaDisable.Match(lines[index]);

            if (!match.Success)
            {
                continue;
            }

            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match rule in RuleId.Matches(match.Groups["text"].Value))
            {
                if (named.Add(rule.Value.ToUpperInvariant()))
                {
                    suppressions.Add(new AnalyzerSuppression(
                        rule.Value.ToUpperInvariant(),
                        "#pragma warning disable",
                        "",
                        file,
                        index + 1));
                }
            }
        }

        foreach (Match attribute in SuppressMessageAttribute.Matches(text))
        {
            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match rule in RuleId.Matches(attribute.Groups["args"].Value))
            {
                if (named.Add(rule.Value.ToUpperInvariant()))
                {
                    suppressions.Add(new AnalyzerSuppression(
                        rule.Value.ToUpperInvariant(),
                        "SuppressMessage",
                        "",
                        file,
                        LineOf(text, attribute.Index)));
                }
            }
        }
    }

    /// <summary>The 1-based line holding the offset in the text.</summary>
    /// <param name="text">The text whose lines are counted.</param>
    /// <param name="offset">The character offset whose line is wanted.</param>
    /// <returns>The one-based line number that holds the offset.</returns>
    private static int LineOf(string text, int offset)
    {
        var line = 1;

        for (var index = 0; index < offset && index < text.Length; index++)
        {
            if (text[index] == '\n')
            {
                line++;
            }
        }

        return line;
    }
}
