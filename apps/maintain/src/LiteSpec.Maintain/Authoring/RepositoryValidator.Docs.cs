using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Model;
using static LiteSpec.Authoring.ManifestValidator;
using static LiteSpec.Authoring.ValidatorSupport;

namespace LiteSpec.Authoring;

/// <summary>
///     The checks that bind the documentation to the tools and the example: a page cites only finding
///     codes, templates and example records that exist, and the index links every page.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal sealed partial class RepositoryValidator
{
    /// <summary>The template folder a page cites a path under.</summary>
    private const string TemplateFolder = "templates/";

    /// <summary>The example folder whose record IDs an example page cites.</summary>
    private const string ExamplePages = "docs/example/";

    /// <summary>The page that indexes the documentation.</summary>
    private const string DocsIndex = "docs/README.md";

    /// <summary>The generated pages the index does not list: each is written by a tool and reached through the page that owns it.</summary>
    private static readonly HashSet<string> IndexExempt = new(StringComparer.Ordinal)
    {
        Provisions.IndexPath,
        Provisions.JsonPath,
        Provisions.PagesPath,
        ReferenceTables.FindingCodesPath,
        ReferenceTables.FoldersPath,
    };

    /// <summary>The source folders whose usage texts declare the commands the command pages describe.</summary>
    private static readonly string[] CommandSourceFolders = ["apps/cli/src/LiteSpec.Cli", "apps/maintain/src/LiteSpec.Maintain"];

    /// <summary>The research records, which are dated evidence and may name finding codes that were weighed and never adopted.</summary>
    private const string ResearchPages = "docs/intent/research/";

    /// <summary>Checks that every finding code a docs page cites exists in the finding code table; research records are evidence and are not checked.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckFindingCitations(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        foreach (var page in parsedPages.Where(page => page.Relative.StartsWith("docs/", StringComparison.Ordinal) && !page.Relative.StartsWith(ResearchPages, StringComparison.Ordinal)))
        {
            foreach (var (number, line) in ProseLines(page.Reader))
            {
                // A Checked by line names its finding code in the provision check, which owns that message.
                if (PageReader.TryParseVerificationLine(line, out var label, out _) && label == "Checked by")
                {
                    continue;
                }

                foreach (var span in InlineCodes(line).Distinct(StringComparer.Ordinal))
                {
                    if (ProvisionValidator.FindingCodePattern().IsMatch(span) && !FindingCodes.IsKnown(span))
                    {
                        add(page.Relative, number, Diagnostics.Codes.FindingCodeUnknown, $"'{span}' is not a finding code in the finding code table");
                    }
                }
            }
        }
    }

    /// <summary>Checks that every template path a docs page cites exists under <c>templates/</c>.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckTemplateCitations(List<ParsedPage> parsedPages)
    {
        var root = Context.Root;
        var add = Context.Add;
        foreach (var page in parsedPages.Where(page => page.Relative.StartsWith("docs/", StringComparison.Ordinal)))
        {
            foreach (var (number, line) in ProseLines(page.Reader))
            {
                foreach (var span in InlineCodes(line).Distinct(StringComparer.Ordinal))
                {
                    var path = span.TrimEnd('/');
                    if (!path.StartsWith(TemplateFolder, StringComparison.Ordinal) || !TemplatePathPattern().IsMatch(path))
                    {
                        continue;
                    }

                    var target = JoinRoot(root, path);
                    if (!File.Exists(target) && !Directory.Exists(target))
                    {
                        add(page.Relative, number, Diagnostics.Codes.TemplateCitedUnknown, $"'{span}' names no file or folder under templates/");
                    }
                }
            }
        }
    }

    /// <summary>Checks that every example record ID an example page cites exists in the example's records.</summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckExampleCitations(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        var pages = parsedPages.Where(page => page.Relative.StartsWith(ExamplePages, StringComparison.Ordinal)).ToList();
        if (pages.Count == 0)
        {
            return;
        }

        var known = RecordsValidator.ExampleAllRecordIds(Context.Root);
        foreach (var page in pages)
        {
            foreach (var (number, line) in ProseLines(page.Reader))
            {
                foreach (var span in InlineCodes(line).Distinct(StringComparer.Ordinal))
                {
                    // A placeholder such as use-case/<module>.<name> names a shape, not a record.
                    var id = span.Split('#')[0];
                    if (!RecordLinkTarget.IsMatch(id) || known.Contains(id))
                    {
                        continue;
                    }

                    add(page.Relative, number, Diagnostics.Codes.ExampleRecordUnknown, $"'{id}' names no record in examples/claimwick/.litespec");
                }
            }
        }
    }

    /// <summary>
    ///     Checks that every option a command page shows in its Synopsis or Options section is an option its command parses.
    ///     A parsed option is an option token of the command's usage text, the raw string literal named <c>Usage</c> in a source
    ///     file of the CLI or the maintainer. A page names its command by the LiteSpec command its Synopsis starts with, so a page
    ///     whose synopsis names another program is not checked, and a LiteSpec command no usage text declares is reported.
    /// </summary>
    /// <param name="parsedPages">The parsed pages.</param>
    internal void CheckCommandOptions(List<ParsedPage> parsedPages)
    {
        var add = Context.Add;
        var usages = CommandUsages(Context.Root);
        foreach (var page in parsedPages.Where(page => page.Parsed.Kind == "command"))
        {
            (int Number, string Command)? head = null;
            foreach (var (number, line) in ShownLines(page.Reader, "Synopsis"))
            {
                if (CommandHeadPattern().Match(line) is { Success: true } match)
                {
                    head = (number, $"{match.Groups[1].Value} {match.Groups[2].Value}");
                    break;
                }
            }

            if (head is not { } named)
            {
                continue;
            }

            var declared = usages.Where(usage => usage.Commands.Contains(named.Command)).ToList();
            if (declared.Count == 0)
            {
                add(page.Relative, named.Number, Diagnostics.Codes.CommandOptionUnknown, $"'{named.Command}' names no command with a usage text in source");
                continue;
            }

            var parsed = declared.SelectMany(usage => usage.Options).ToHashSet(StringComparer.Ordinal);
            foreach (var (number, line) in ShownLines(page.Reader, "Synopsis", "Options"))
            {
                foreach (var option in OptionPattern().Matches(line).Select(match => match.Value).Distinct(StringComparer.Ordinal))
                {
                    if (!parsed.Contains(option))
                    {
                        add(page.Relative, number, Diagnostics.Codes.CommandOptionUnknown, $"'{option}' is not an option '{named.Command}' parses");
                    }
                }
            }
        }
    }

    /// <summary>Reads the usage text of each command the CLI and the maintainer declare.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>Each usage text with the commands it names and the option tokens it contains.</returns>
    private static List<(HashSet<string> Commands, HashSet<string> Options)> CommandUsages(string root)
    {
        var usages = new List<(HashSet<string> Commands, HashSet<string> Options)>();
        foreach (var folder in CommandSourceFolders)
        {
            foreach (var file in Walk(JoinRoot(root, folder), candidate => candidate.EndsWith(".cs", StringComparison.Ordinal)))
            {
                foreach (Match usage in UsageConstantPattern().Matches(FileText.ReadText(file)))
                {
                    var text = usage.Groups[1].Value;
                    var commands = CommandHeadPattern().Matches(text).Select(match => $"{match.Groups[1].Value} {match.Groups[2].Value}").ToHashSet(StringComparer.Ordinal);
                    var options = OptionPattern().Matches(text).Select(match => match.Value).ToHashSet(StringComparer.Ordinal);
                    usages.Add((commands, options));
                }
            }
        }

        return usages;
    }

    /// <summary>The lines under the named H2 sections of a page, each with its 1-based number.</summary>
    /// <param name="reader">The page reader.</param>
    /// <param name="sections">The H2 section names to include.</param>
    /// <returns>Each included line's 1-based number and text.</returns>
    private static IEnumerable<(int Number, string Line)> ShownLines(PageReader reader, params string[] sections)
    {
        var inside = false;
        for (var index = 0; index < reader.Lines.Length; index++)
        {
            var line = reader.Lines[index];
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inside = sections.Contains(line[3..].Trim(), StringComparer.Ordinal);
            }
            else if (inside)
            {
                yield return (index + 1, line);
            }
        }
    }

    /// <summary>Checks that the documentation index links every page under <c>docs/</c>, directly or through a section index.</summary>
    /// <param name="files">The standards files, as absolute paths.</param>
    internal void CheckIndexCoverage(List<string> files)
    {
        var root = Context.Root;
        var add = Context.Add;
        if (!File.Exists(Path.Combine(root, "docs", "README.md")))
        {
            return;
        }

        // A reader starts at the index and follows links, so a page is indexed when some page
        // reached from the index links it. Three kinds are exempt: a generated page, which the
        // page that owns its generator reaches; a record's explanation file, which the record
        // reaches through its file name beside it; and the index itself.
        var reached = new HashSet<string>(StringComparer.Ordinal) { DocsIndex };
        var queue = new Queue<string>();
        queue.Enqueue(DocsIndex);
        while (queue.Count > 0)
        {
            foreach (var target in LinkedPages(root, queue.Dequeue()).Where(target => target.StartsWith("docs/", StringComparison.Ordinal) && target.EndsWith(".md", StringComparison.Ordinal)))
            {
                if (reached.Add(target))
                {
                    queue.Enqueue(target);
                }
            }
        }

        var pages = files
            .Select(file => PathText.Relative(root, file))
            .Where(relative => relative.StartsWith("docs/", StringComparison.Ordinal) && relative.EndsWith(".md", StringComparison.Ordinal))
            .Where(relative => !IndexExempt.Contains(relative) && !File.Exists(Path.ChangeExtension(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)), ".yaml")))
            .OrderBy(relative => relative, StringComparer.Ordinal);
        foreach (var page in pages.Where(page => !reached.Contains(page)))
        {
            add(page, 1, Diagnostics.Codes.IndexPageMissing, $"'{page}' is reached by no link from the documentation index");
        }
    }

    /// <summary>The prose lines of a page: each line with its number, skipping fenced code and generated regions.</summary>
    /// <param name="reader">The page reader.</param>
    /// <returns>Each prose line's 1-based number and text.</returns>
    private static IEnumerable<(int Number, string Line)> ProseLines(PageReader reader)
    {
        for (var index = 0; index < reader.Lines.Length; index++)
        {
            var number = index + 1;
            if (!reader.FencedLines.Contains(number) && !reader.GeneratedLines.Contains(number))
            {
                yield return (number, reader.Lines[index]);
            }
        }
    }

    /// <summary>The repository-relative paths of the local pages a page links to, anchors removed.</summary>
    /// <param name="root">The repository root.</param>
    /// <param name="relative">The repository-relative path of the linking page.</param>
    /// <returns>Each linked page's repository-relative path.</returns>
    private static HashSet<string> LinkedPages(string root, string relative)
    {
        var linked = new HashSet<string>(StringComparer.Ordinal);
        var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(file))
        {
            return linked;
        }

        var reader = PageReader.Parse(FileText.ReadText(file));
        foreach (var link in reader.Links)
        {
            var href = link.Target.Trim();
            var part = href.Split('#')[0];
            if (part.Length == 0 || AbsoluteUriPattern().IsMatch(href) || !TryDecodeURIComponent(part, out var decoded))
            {
                continue;
            }

            var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, decoded));
            linked.Add(PathText.Relative(root, target));
        }

        return linked;
    }

    /// <summary>Gets the pattern that matches a template path a page cites.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"^templates/[A-Za-z0-9_./-]+$")]
    private static partial Regex TemplatePathPattern();

    /// <summary>Gets the pattern that matches the usage text of a command source file, which the raw string literal named <c>Usage</c> holds.</summary>
    /// <returns>The compiled pattern; its first group is the text.</returns>
    [GeneratedRegex(@"const string Usage = \x22\x22\x22\s*\r?\n(.*?)^\s*\x22\x22\x22", RegexOptions.Singleline | RegexOptions.Multiline)]
    private static partial Regex UsageConstantPattern();

    /// <summary>Gets the pattern that matches the LiteSpec program and the command word a usage line or a synopsis starts with.</summary>
    /// <returns>The compiled pattern; its groups are the program and the command word.</returns>
    [GeneratedRegex(@"^\s*(?:Usage:\s*)?(litespec(?:-maintain)?)\s+([a-z][a-z-]*)", RegexOptions.Multiline)]
    private static partial Regex CommandHeadPattern();

    /// <summary>Gets the pattern that matches one long option token.</summary>
    /// <returns>The compiled pattern.</returns>
    [GeneratedRegex(@"(?<![A-Za-z0-9-])--[a-z][a-z0-9-]*")]
    private static partial Regex OptionPattern();
}
