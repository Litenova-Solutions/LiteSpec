using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Cli.Model.Ui;
using LiteSpec.Cli.Rules;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Consumer;

/// <summary>
///     The page scan of the consumer validator: collects the record files and Markdown files,
///     validates each record file and each document's frontmatter against the record schema,
///     and checks the document sections, headings, and the Example word bound.
/// </summary>
/// <follows>litespec/core/model#hold-every-record-file-and-frontmatter-block-to-the-schema</follows>
/// <follows>litespec/core/authoring#use-the-declared-page-contract</follows>
static class PageChecks
{
    /// <summary>The Example word bound.</summary>
    const int ExampleLimit = 120;

    // A document keeps the sections its page style always required: a document never held
    // a record, so nothing about it changed when records moved to files.
    /// <summary>The H2 sections each document page style requires.</summary>
    static readonly Dictionary<string, string[]> DocumentSections = new(StringComparer.Ordinal)
    {
        ["tutorial"] = ["Purpose", "Prerequisites", "Lesson", "What You Built"],
        ["how-to"] = ["Purpose", "Procedure", "Check"],
        ["reference"] = ["Intent", "Reference"],
        ["command"] = ["Name", "Synopsis", "Description", "Arguments", "Options", "Exit Codes", "Examples", "Underneath"],
        ["configuration"] = ["Intent", "Settings", "Precedence"],
    };

    /// <summary>The page styles a document carries. Every other named type lives in a record file.</summary>
    static readonly HashSet<string> DocumentStyles = new(
        ["index", "tutorial", "how-to", "reference", "command", "configuration", "glossary"],
        StringComparer.Ordinal);

    /// <summary>An H2 heading line.</summary>
    static readonly Regex HeadingTwo = new("^##\\s", RegexOptions.Compiled);

    /// <summary>
    ///     Collects the record files and Markdown files under the documentation root in ordinal
    ///     name order, leaving the glossary file to the glossary check.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CollectFiles(ConsumerRun run)
    {
        var notPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            run.GlossaryFile,
            ConsumerRoot.ProjectFile(run.Root),
            Path.GetFullPath(Path.Join(run.Root, JsonValues.PropString(run.Project?["paths"] as JsonObject, "uiPatterns") ?? CatalogChecks.DefaultCatalog)),
        };

        run.Files.AddRange(GlobWalker.PageFiles(run.DocsRoot, notPages));
    }

    /// <summary>
    ///     Validates each record file and each Markdown file. Files are independent, so the
    ///     scan runs in parallel and merges in file order, keeping every finding exactly
    ///     where the sequential scan reported it.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckPages(ConsumerRun run)
    {
        var count = run.Files.Count;
        var errors = new List<Finding>[count];
        var isPage = new bool[count];

        for (var slot = 0; slot < count; slot++)
        {
            errors[slot] = [];
        }


        Parallel.For(0, count, index =>
        {
            var file = run.Files[index];
            var relative = PathText.Relative(run.Root, file);
            var raw = run.ReadText(file);
            var add = errors[index].Add;

            if (RecordFiles.IsRecordFile(file))
            {
                isPage[index] = CheckRecordFile(run, file, relative, raw, add);
            }
            else
            {
                isPage[index] = CheckMarkdownFile(run, file, relative, raw, add);
            }
        });


        for (var index = 0; index < count; index++)
        {
            foreach (var finding in errors[index])
            {
                run.Err(finding);
            }

            if (isPage[index])
            {
                run.Pages += 1;
            }
        }
    }

    /// <summary>
    ///     Validates one record file: it parses, its type names a record rather than a
    ///     document, and it satisfies the record schema. A Markdown file beside the
    ///     record file is human-only and is never read.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="file">The record file as a full path.</param>
    /// <param name="relative">The file path the schema check names.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>True when the file parsed as a record.</returns>
    static bool CheckRecordFile(ConsumerRun run, string file, string relative, string raw, Action<Finding> add)
    {
        if (!RecordFile.TryParse(raw, out _, out var tree, out var error))
        {
            add(new Finding("record-invalid", error, file, 1, null));
            return false;
        }

        var meta = RecordFile.NodeOf(tree);
        var subject = JsonValues.PropString(meta as JsonObject, "id");

        // The record is validated through the shared schema module against the
        // record schema, so the schema is the one statement of every record shape.
        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, ReleaseText.Read, run.Verbose))
        {
            add(ConsumerRun.SchemaFinding("record-invalid", file, relative, problem, subject));
        }

        var type = JsonValues.PropString(meta as JsonObject, "type");

        if (type is not null && DocumentStyles.Contains(type))
        {
            add(new Finding("page-invalid", $"type '{type}' names a document; documents live in '.md' files", file, 1, subject));
        }

        return true;
    }

    /// <summary>
    ///     Validates one Markdown file: a document carries its page style in a frontmatter
    ///     block, and anything else without a record file beside it is an orphaned page.
    ///     A Markdown file beside a record file is human-only and is never read. A README
    ///     without a record file beside it stays a document.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="file">The Markdown file as a full path.</param>
    /// <param name="relative">The file path the schema check names.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>True when the file is a document.</returns>
    static bool CheckMarkdownFile(ConsumerRun run, string file, string relative, string raw, Action<Finding> add)
    {
        // A project convention is provision-shaped Markdown that rules reads, not a document.
        if (ConventionFiles.InConventionsFolder(relative))
        {
            return false;
        }

        var recordPath = RecordFile.RecordPathOf(file);

        // The glossary file is data, not a record, so a Markdown file beside it is no
        // explanation of it: the glossary document is a document like any other.
        var hasRecord = File.Exists(recordPath) && !recordPath.Equals(run.GlossaryFile, StringComparison.OrdinalIgnoreCase);
        var startsFence = StartsFence(raw);

        // A Markdown file beside a record file is human-only: no check reads it.
        if (hasRecord)
        {
            return false;
        }

        if (!startsFence)
        {
            if (Path.GetFileName(file) == "README.md")
            {
                CheckDocumentBody(file, raw, null, null, add);
                return true;
            }

            add(new Finding(
                "page-invalid",
                $"no frontmatter block and no '{Path.GetFileName(recordPath)}' beside it; every document opens with a '---' frontmatter block carrying its page style",
                file,
                1,
                null));
            return false;
        }

        if (!Frontmatter.TryParse(raw, out var parsed, out var error))
        {
            add(new Finding("record-invalid", error, file, 1, null));
            return false;
        }

        var meta = JsonNode.Parse(parsed.Root.GetRawText());
        var subject = JsonValues.PropString(meta as JsonObject, "id");

        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, ReleaseText.Read, run.Verbose))
        {
            add(ConsumerRun.SchemaFinding("record-invalid", file, relative, problem, subject));
        }

        var type = JsonValues.PropString(meta as JsonObject, "type");

        if (type is not null && !DocumentStyles.Contains(type) && (RecordTypes.All.Contains(type) || RecordTypes.PageStyles.Contains(type)))
        {
            add(new Finding(
                "page-invalid",
                $"type '{type}' names a record; records live in '{Path.GetFileName(recordPath)}' record files",
                file,
                1,
                subject));
        }

        CheckDocumentBody(file, parsed.Body, type, subject, add);
        return true;
    }

    /// <summary>
    ///     Checks one document's body: its headings, the sections its page style requires,
    ///     and the Example word bound.
    /// </summary>
    /// <param name="file">The document file as a full path.</param>
    /// <param name="body">The document text after its frontmatter.</param>
    /// <param name="type">The document page style, or null when it names none.</param>
    /// <param name="subject">The document id, or null when it names none.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckDocumentBody(string file, string body, string? type, string? subject, Action<Finding> add)
    {
        var lines = SplitLines(body);
        HeadingChecks.CheckHeadings(file, lines, subject, add);

        string[] required = type is not null && DocumentSections.TryGetValue(type, out var names) ? names : [];
        CheckSections(file, lines, type, subject, required, add);
    }

    /// <summary>
    ///     Checks the required H2 sections and the Example word bound of one Markdown text.
    /// </summary>
    /// <param name="file">The page file as a full path.</param>
    /// <param name="lines">The text lines.</param>
    /// <param name="type">The document page style, naming the required sections.</param>
    /// <param name="subject">The record id the findings are about, when there is one.</param>
    /// <param name="required">The required H2 section names.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckSections(string file, string[] lines, string? type, string? subject, string[] required, Action<Finding> add)
    {
        var bodies = SectionBodies(lines, required.Append("Example"));

        foreach (var name in required)
        {
            if (bodies[name] is null)
            {
                add(new Finding("page-section-missing", $"type '{type}' requires an H2 '{name}'", file, 1, subject));
            }
        }

        // The Example section stays a narrative, not a second specification: it
        // illustrates the page within the word bound and carries no authority.
        if (bodies["Example"] is string example)
        {
            var words = Prose.Words(example).Count;

            if (words > ExampleLimit)
            {
                add(new Finding(
                    "page-example-invalid",
                    $"Example section has {words} words, over the {ExampleLimit} word bound",
                    file,
                    ExampleLine(lines),
                    subject));
            }
        }
    }

    /// <summary>
    ///     The 1-based line of the Example H2, or 1 when the page carries none.
    /// </summary>
    /// <param name="lines">The page lines.</param>
    /// <returns>The Example heading line.</returns>
    static int ExampleLine(string[] lines)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "## Example")
            {
                return index + 1;
            }
        }

        return 1;
    }

    /// <summary>
    ///     Whether the text opens with a frontmatter fence line: three dashes and nothing else.
    /// </summary>
    /// <param name="raw">The file text.</param>
    /// <returns>True when the first line is the opening fence.</returns>
    static bool StartsFence(string raw) =>
        raw.StartsWith("---\n", StringComparison.Ordinal)
        || raw.StartsWith("---\r\n", StringComparison.Ordinal)
        || raw == "---";

    /// <summary>
    ///     Splits a page into lines the way the section and heading checks always
    ///     split it: each newline ends a line and swallows one carriage return.
    /// </summary>
    /// <param name="raw">The page text.</param>
    /// <returns>The page lines.</returns>
    static string[] SplitLines(string raw)
    {
        var lines = raw.Split('\n');

        for (var index = 0; index < lines.Length; index++)
        {
            if (lines[index].Length > 0 && lines[index][^1] == '\r')
            {
                lines[index] = lines[index][..^1];
            }
        }

        return lines;
    }

    /// <summary>
    ///     The text between each named H2 and the next one, or null for each name
    ///     the page does not carry: the first line whose trimmed text is the name
    ///     opens the section, and the next unindented H2 closes it.
    /// </summary>
    /// <param name="lines">The page lines.</param>
    /// <param name="names">The section names to read.</param>
    /// <returns>The section bodies, by name.</returns>
    static Dictionary<string, string?> SectionBodies(string[] lines, IEnumerable<string> names)
    {
        var wanted = names.Select(name => "## " + name).ToHashSet(StringComparer.Ordinal);
        var starts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < lines.Length && starts.Count < wanted.Count; index++)
        {
            var trimmed = lines[index].Trim();

            if (wanted.Contains(trimmed))
            {
                starts.TryAdd(trimmed, index);
            }
        }

        var bodies = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            var key = "## " + name;

            if (!starts.TryGetValue(key, out var start))
            {
                bodies[name] = null;
                continue;
            }

            var end = start + 1;

            while (end < lines.Length && !HeadingTwo.IsMatch(lines[end]))
            {
                end++;
            }

            bodies[name] = string.Join("\n", lines, start + 1, end - start - 1);
        }

        return bodies;
    }
}
