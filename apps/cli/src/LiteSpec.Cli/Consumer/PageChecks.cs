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
    // An explanation file carries the sections its record type requires: research answers
    // its question, a decision states its context, and a topic argues its position. Every
    // other type keeps free prose. Each field the schema requires left the body for the
    // record file, so no other record type names a required section.
    /// <summary>The H2 sections each article type's explanation file requires.</summary>
    static readonly Dictionary<string, string[]> ExplanationSections = new(StringComparer.Ordinal)
    {
        ["research"] = ["Answer"],
        ["decision"] = ["Context"],
        ["topic"] = ["The Situation", "What We Do", "Why It Is Better", "Example", "Costs and Risks", "How Sure We Are"],
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
        ["review-report"] = ["Verdicts", "How the Review Ran"],
        ["command"] = ["Name", "Synopsis", "Description", "Arguments", "Options", "Exit Codes", "Examples", "Underneath"],
        ["configuration"] = ["Intent", "Settings", "Precedence"],
    };

    /// <summary>The page styles a document carries. Every other named type lives in a record file.</summary>
    static readonly HashSet<string> DocumentStyles = new(
        ["index", "tutorial", "how-to", "reference", "review-report", "command", "configuration", "glossary", "example-cast"],
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
        var glossaryFile = run.GlossaryFile;
        Walk(run.DocsRoot);

        void Walk(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var entry in Directory.GetFileSystemEntries(directory).OrderBy(name => name, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(entry) ?? "";

                if (name == "node_modules" || name.StartsWith('.'))
                {
                    continue;
                }

                if (Directory.Exists(entry))
                {
                    Walk(entry);
                }
                else if (IsScanned(name) && File.Exists(entry) && !entry.Equals(glossaryFile, StringComparison.OrdinalIgnoreCase))
                {
                    run.Files.Add(entry);
                }
            }
        }
    }

    /// <summary>Whether the page scan reads the file: a record file or a Markdown file.</summary>
    /// <param name="name">The file name.</param>
    /// <returns>True for a record file or a Markdown file.</returns>
    static bool IsScanned(string name) =>
        name.EndsWith(RecordFile.Extension, StringComparison.Ordinal)
        || name.EndsWith(RecordFile.ExplanationExtension, StringComparison.Ordinal);

    /// <summary>
    ///     Validates each record file and each Markdown file. Files are independent, so the
    ///     scan runs in parallel and merges in file order, keeping every finding exactly
    ///     where the sequential scan reported it.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckPages(ConsumerRun run)
    {
        var exampleLimit = ExampleWordLimit(run);
        var count = run.Files.Count;
        var errors = new List<string>[count];
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
                isPage[index] = CheckMarkdownFile(run, file, relative, raw, exampleLimit, add);
            }
        });

        watch.Stop();
        Timing.Report($"[time] consumer-pages {watch.Elapsed.TotalSeconds:0.0}s");

        for (var index = 0; index < count; index++)
        {
            foreach (var problem in errors[index])
            {
                run.Err(problem);
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
    /// <param name="file">The record file.</param>
    /// <param name="relative">The file path the findings name.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>True when the file parsed as a record.</returns>
    static bool CheckRecordFile(ConsumerRun run, string file, string relative, string raw, Action<string> add)
    {
        if (!RecordFile.TryParse(raw, out _, out var tree, out var error))
        {
            add($"{relative}: {error}");
            return false;
        }

        var meta = JsonNode.Parse(tree.ToJson());

        // The record is validated through the shared schema module against the
        // record schema, so the schema is the one statement of every record shape.
        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, run.ReadSchema))
        {
            add(problem);
        }

        var type = JsonRead.PropString(meta as JsonObject, "type");

        if (type is not null && DocumentStyles.Contains(type))
        {
            add($"{relative}: type '{type}' names a document; documents live in '.md' files");
        }

        if (type is not null && RecordFile.ArticleTypes.Contains(type))
        {
            var explanation = RecordFile.ExplanationPathOf(file);

            if (!File.Exists(explanation))
            {
                add($"{relative}: type '{type}' requires an explanation file '{Path.GetFileName(explanation)}' beside the record file");
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
    /// <param name="file">The Markdown file.</param>
    /// <param name="relative">The file path the findings name.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="exampleLimit">The Example word bound.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>True when the file is a document.</returns>
    static bool CheckMarkdownFile(ConsumerRun run, string file, string relative, string raw, int exampleLimit, Action<string> add)
    {
        var recordPath = RecordFile.RecordPathOf(file);

        // The glossary file is data, not a record, so a Markdown file beside it is no
        // explanation of it: the glossary document is a document like any other.
        var hasRecord = File.Exists(recordPath) && !recordPath.Equals(run.GlossaryFile, StringComparison.OrdinalIgnoreCase);
        var startsFence = StartsFence(raw);

        if (hasRecord && !startsFence)
        {
            CheckExplanation(run, file, relative, raw, exampleLimit, add);
            return false;
        }

        if (hasRecord)
        {
            add($"{relative}: explanation files carry no frontmatter block; the record lives in '{Path.GetFileName(recordPath)}'");
            CheckExplanation(run, file, relative, raw, exampleLimit, add);
            return false;
        }

        if (!startsFence)
        {
            if (Path.GetFileName(file) == "README.md")
            {
                CheckDocumentBody(relative, raw, null, exampleLimit, add);
                return true;
            }

            add($"{relative}: no frontmatter block and no '{Path.GetFileName(recordPath)}' beside it; every document opens with a '---' frontmatter block carrying its page style");
            return false;
        }

        if (!Frontmatter.TryParse(raw, out var parsed, out var error))
        {
            add($"{relative}: {error}");
            return false;
        }

        var meta = JsonNode.Parse(parsed.Root.GetRawText());

        foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, run.ReadSchema))
        {
            add(problem);
        }

        var type = JsonRead.PropString(meta as JsonObject, "type");

        if (type is not null && !DocumentStyles.Contains(type) && (RecordTypes.All.Contains(type) || RecordTypes.PageStyles.Contains(type)))
        {
            add($"{relative}: type '{type}' names a record; records live in '{Path.GetFileName(recordPath)}' record files");
        }

        CheckDocumentBody(relative, parsed.Body, type, exampleLimit, add);
        return true;
    }

    /// <summary>
    ///     Checks one explanation file: its headings, the sections its record type requires,
    ///     and the Example word bound.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <param name="file">The explanation file.</param>
    /// <param name="relative">The file path the findings name.</param>
    /// <param name="raw">The file text.</param>
    /// <param name="exampleLimit">The Example word bound.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckExplanation(ConsumerRun run, string file, string relative, string raw, int exampleLimit, Action<string> add)
    {
        // The paired record file names the explanation's required sections. The scan
        // already reported a record file it cannot read, so this read stays quiet and
        // checks the headings and the Example bound only.
        string? type = null;

        if (RecordFile.TryParse(run.ReadText(RecordFile.RecordPathOf(file)), out var root, out _, out _))
        {
            type = JsonRead.PropString(JsonNode.Parse(root.GetRawText()) as JsonObject, "type");
        }

        var lines = SplitLines(raw);
        HeadingChecks.CheckHeadings(relative, lines, add);

        string[] required = type is not null && ExplanationSections.TryGetValue(type, out var names) ? names : [];
        CheckSections(relative, lines, type, required, exampleLimit, add);
    }

    /// <summary>
    ///     Checks one document's body: its headings, the sections its page style requires,
    ///     and the Example word bound.
    /// </summary>
    /// <param name="relative">The file path the findings name.</param>
    /// <param name="body">The document text after its frontmatter.</param>
    /// <param name="type">The document page style, or null when it names none.</param>
    /// <param name="exampleLimit">The Example word bound.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckDocumentBody(string relative, string body, string? type, int exampleLimit, Action<string> add)
    {
        var lines = SplitLines(body);
        HeadingChecks.CheckHeadings(relative, lines, add);

        string[] required = type is not null && DocumentSections.TryGetValue(type, out var names) ? names : [];
        CheckSections(relative, lines, type, required, exampleLimit, add);
    }

    /// <summary>
    ///     Checks the required H2 sections and the Example word bound of one Markdown text.
    /// </summary>
    /// <param name="relative">The file path the findings name.</param>
    /// <param name="lines">The text lines.</param>
    /// <param name="type">The record type or page style, naming the required sections.</param>
    /// <param name="required">The required H2 section names.</param>
    /// <param name="exampleLimit">The Example word bound.</param>
    /// <param name="add">Collects one finding.</param>
    static void CheckSections(string relative, string[] lines, string? type, string[] required, int exampleLimit, Action<string> add)
    {
        var bodies = SectionBodies(lines, required.Append("Example"));

        foreach (var name in required)
        {
            if (bodies[name] is null)
            {
                add($"{relative}: type '{type}' requires an H2 '{name}'");
            }
        }

        // The Example section stays a narrative, not a second specification: it
        // illustrates the page within the word bound and carries no authority.
        if (bodies["Example"] is string example)
        {
            var words = Prose.Words(example).Count;

            if (words > exampleLimit)
            {
                add($"{relative}: Example section has {words} words, over the {exampleLimit} word bound");
            }
        }
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
    ///     Reads the Example word bound from the project record. Omitted, the recorded
    ///     default of 120 applies, and a bound below 40 is refused.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    /// <returns>The Example word bound.</returns>
    static int ExampleWordLimit(ConsumerRun run)
    {
        var value = (run.Project as JsonObject)?["exampleWordLimit"];

        if (value is JsonValue number && number.TryGetValue<long>(out var limit))
        {
            if (limit < 40)
            {
                run.Err($"litespec.project.json: exampleWordLimit '{limit}' is below 40; the Example bound cannot be that short");
                return 120;
            }

            return limit > int.MaxValue ? int.MaxValue : (int)limit;
        }

        return 120;
    }

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
