using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using LiteSpec.Model;
using LiteSpec.Schemas;

namespace LiteSpec.Consumer;

/// <summary>
///     The page scan of the consumer validator: collects the record files and Markdown files,
///     validates each record file and each document's frontmatter against the record schema,
///     and checks the explanation and document sections, headings, and the Example word bound.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#use-the-declared-page-contract</follows>
static class PageChecks
{
    /// <summary>The Example word bound.</summary>
    const int ExampleLimit = 120;

    // An explanation file carries the sections its record type requires: research answers
    // its question and a decision states its context. Every other type keeps free prose.
    // Each field the schema requires left the body for the record file, so no other
    // record type names a required section.
    /// <summary>The H2 sections each article type's explanation file requires.</summary>
    static readonly Dictionary<string, string[]> ExplanationSections = new(StringComparer.Ordinal)
    {
        ["research"] = ["Answer"],
        ["decision"] = ["Context"],
    };

    // A document keeps the sections its page style always required: a document never held
    // a record, so nothing about it changed when records moved to files.
    /// <summary>The H2 sections each document page style requires.</summary>
    static readonly Dictionary<string, string[]> DocumentSections = new(StringComparer.Ordinal)
    {
        ["example-cast"] = ["The Organization", "The People", "The Occasion", "The Amounts", "Recurring Situations"],
        ["tutorial"] = ["Purpose", "Prerequisites", "Lesson", "What You Built"],
        ["how-to"] = ["Purpose", "Procedure", "Check"],
        ["reference"] = ["Intent", "Reference"],
        ["command"] = ["Name", "Synopsis", "Description", "Arguments", "Options", "Exit Codes", "Examples", "Underneath"],
        ["configuration"] = ["Intent", "Settings", "Precedence"],
    };

    /// <summary>The page styles a document carries. Every other named type lives in a record file.</summary>
    static readonly HashSet<string> DocumentStyles = new(
        ["index", "tutorial", "how-to", "reference", "command", "configuration", "glossary", "example-cast"],
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
        run.Files.AddRange(GlobWalker.PageFiles(run.DocsRoot, run.GlossaryFile));
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

        var watch = System.Diagnostics.Stopwatch.StartNew();

        Parallel.For(0, count, index =>
        {
            var file = run.Files[index];
            var relative = run.Relative(file);
            var raw = run.ReadText(file);
            var add = errors[index].Add;

            if (RecordFile.IsRecordFile(file))
            {
                isPage[index] = CheckRecordFile(run, file, relative, raw, add);
            }
            else
            {
                isPage[index] = CheckMarkdownFile(run, file, relative, raw, add);
            }
        });

        watch.Stop();
        Timing.Report($"[time] consumer-pages {watch.Elapsed.TotalSeconds:0.0}s");

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
    ///     document, it satisfies the record schema, and an article type keeps its
    ///     explanation file beside it.
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

        var meta = JsonNode.Parse(tree.ToJson());
        var subject = JsonRead.PropString(meta as JsonObject, "id");

        // The record is validated through the shared schema module against the
        // record schema, so the schema is the one statement of every record shape.
        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, run.ReadSchema))
        {
            add(ConsumerRun.SchemaFinding("record-invalid", file, relative, problem, subject));
        }

        var type = JsonRead.PropString(meta as JsonObject, "type");

        if (type is not null && DocumentStyles.Contains(type))
        {
            add(new Finding("page-invalid", $"type '{type}' names a document; documents live in '.md' files", file, 1, subject));
        }

        if (type is not null && RecordFile.ArticleTypes.Contains(type))
        {
            var explanation = RecordFile.ExplanationPathOf(file);

            if (!File.Exists(explanation))
            {
                add(new Finding(
                    "page-invalid",
                    $"type '{type}' requires an explanation file '{Path.GetFileName(explanation)}' beside the record file",
                    file,
                    1,
                    subject));
            }
        }

        return true;
    }

    /// <summary>
    ///     Validates one Markdown file: an explanation file beside a record file carries no
    ///     frontmatter, a document carries its page style in one, and anything else is an
    ///     orphaned explanation. A README without either stays a document.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="file">The Markdown file as a full path.</param>
    /// <param name="relative">The file path the schema check names.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>True when the file is a document.</returns>
    static bool CheckMarkdownFile(ConsumerRun run, string file, string relative, string raw, Action<Finding> add)
    {
        var recordPath = RecordFile.RecordPathOf(file);

        // The glossary file is data, not a record, so a Markdown file beside it is no
        // explanation of it: the glossary document is a document like any other.
        var hasRecord = File.Exists(recordPath) && !recordPath.Equals(run.GlossaryFile, StringComparison.OrdinalIgnoreCase);
        var startsFence = StartsFence(raw);

        if (hasRecord && !startsFence)
        {
            CheckExplanation(run, file, raw, add);
            return false;
        }

        if (hasRecord)
        {
            add(new Finding(
                "page-invalid",
                $"explanation files carry no frontmatter block; the record lives in '{Path.GetFileName(recordPath)}'",
                file,
                1,
                RecordId(run, recordPath)));
            CheckExplanation(run, file, raw, add);
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
        var subject = JsonRead.PropString(meta as JsonObject, "id");

        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, run.ReadSchema))
        {
            add(ConsumerRun.SchemaFinding("record-invalid", file, relative, problem, subject));
        }

        var type = JsonRead.PropString(meta as JsonObject, "type");

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
    ///     The id of a record file, or null when the file cannot be read as a record.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="recordPath">The record file.</param>
    /// <returns>The record id, or null.</returns>
    static string? RecordId(ConsumerRun run, string recordPath)
    {
        if (RecordFile.TryParse(run.ReadText(recordPath), out var root, out _, out _))
        {
            return JsonRead.PropString(JsonNode.Parse(root.GetRawText()) as JsonObject, "id");
        }

        return null;
    }

    /// <summary>
    ///     Checks one explanation file: its headings, the sections its record type requires,
    ///     and the Example word bound.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="file">The explanation file as a full path.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckExplanation(ConsumerRun run, string file, string raw, Action<Finding> add)
    {
        // The paired record file names the explanation's required sections. The scan
        // already reported a record file it cannot read, so this read stays quiet and
        // checks the headings and the Example bound only.
        string? type = null;
        string? subject = null;

        if (RecordFile.TryParse(run.ReadText(RecordFile.RecordPathOf(file)), out var root, out _, out _))
        {
            var meta = JsonNode.Parse(root.GetRawText()) as JsonObject;
            type = JsonRead.PropString(meta, "type");
            subject = JsonRead.PropString(meta, "id");
        }

        var lines = SplitLines(raw);
        HeadingChecks.CheckHeadings(file, lines, subject, add);

        string[] required = type is not null && ExplanationSections.TryGetValue(type, out var names) ? names : [];
        CheckSections(file, lines, type, subject, required, add);
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
    /// <param name="type">The record type or page style, naming the required sections.</param>
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
                    "page-example-too-long",
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
