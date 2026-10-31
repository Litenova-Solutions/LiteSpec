using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using LiteSpec.Model;
using LiteSpec.Schemas;

namespace LiteSpec.Consumer;

/// <summary>
///     The page scan of the consumer validator: collects the Markdown pages, validates each
///     page's frontmatter against the record schema, and checks the required sections
///     and the Example word bound.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#use-the-declared-page-contract</follows>
static class PageChecks
{
    // Each template in the 2.0 template table names the sections its reader expects
    // to find. An absent section is a question the page never answered, which reads
    // exactly like a question with no answer. Types with no entry carry their
    // content in frontmatter or hold optional sections only, and keep free body prose.
    // An aggregate carries its Transitions and Data in frontmatter with generated
    // regions for reading, so it names no required section. A use case carries Goal
    // and What Happens only when significant, which the section check below decides.
    // An exception carries Consequences or Exit by its kind, decided below.
    /// <summary>The H2 sections each page type requires.</summary>
    static readonly Dictionary<string, string[]> RequiredSections = new(StringComparer.Ordinal)
    {
        ["product"] = ["Summary"],
        ["goal"] = ["Why It Matters"],
        ["problem"] = ["Situation", "Evidence"],
        ["feature"] = ["Offering"],
        ["constraint"] = ["Summary", "What It Requires"],
        ["research"] = ["Answer"],
        ["decision"] = ["Context", "Decision", "Consequences"],
        ["topic"] = ["Summary", "The Situation", "What We Do", "Why It Is Better", "Example", "Costs and Risks", "How Sure We Are", "Position"],
        ["journey"] = ["Example", "Between the Steps"],
        ["rule-set"] = ["Summary"],
        ["actor"] = ["Summary"],
        ["role"] = ["Summary"],
        ["external-system"] = ["Summary"],
        ["runbook"] = ["Impact", "Prerequisites", "Procedure", "Verification", "Rollback or Recovery", "Escalation"],
        ["example-cast"] = ["The Organization", "The People", "The Occasion", "The Amounts", "Recurring Situations"],
        ["tutorial"] = ["Purpose", "Prerequisites", "Lesson", "What You Built"],
        ["how-to"] = ["Purpose", "Procedure", "Check"],
        ["reference"] = ["Intent", "Reference"],
        ["review-report"] = ["Verdicts", "How the Review Ran"],
        ["command"] = ["Name", "Synopsis", "Description", "Arguments", "Options", "Exit Codes", "Examples", "Underneath"],
        ["configuration"] = ["Intent", "Settings", "Precedence"],
    };

    /// <summary>A record held in a fenced block rather than a frontmatter block.</summary>
    static readonly Regex FencedBlock = new("```[a-z]*\\s*\\n\\s*\\{[\\s\\S]{0,400}?\"type\"\\s*:", RegexOptions.Compiled);

    /// <summary>An H2 heading line.</summary>
    static readonly Regex HeadingTwo = new("^##\\s", RegexOptions.Compiled);

    /// <summary>
    ///     Collects the Markdown pages under the documentation root in ordinal name order.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CollectFiles(ConsumerRun run)
    {
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
                else if (name.EndsWith(".md", StringComparison.Ordinal) && File.Exists(entry))
                {
                    run.Files.Add(entry);
                }
            }
        }
    }

    /// <summary>
    ///     Validates each page's frontmatter, headings, and required sections. Pages
    ///     are independent, so both phases run in parallel and merge in file order,
    ///     keeping every finding exactly where the sequential scan reported it.
    /// </summary>
    /// <param name="run">The state of the run.</param>
    public static void CheckPages(ConsumerRun run)
    {
        var exampleLimit = ExampleWordLimit(run);
        var count = run.Files.Count;
        var relatives = new string[count];
        var raws = new string[count];
        var metas = new JsonNode?[count];
        var firstErrors = new List<string>[count];
        var secondErrors = new List<string>[count];
        var isPage = new bool[count];

        for (var slot = 0; slot < count; slot++)
        {
            firstErrors[slot] = [];
            secondErrors[slot] = [];
            relatives[slot] = "";
            raws[slot] = "";
        }

        var schemaWatch = System.Diagnostics.Stopwatch.StartNew();
        var refusalHomes = new System.Collections.Concurrent.ConcurrentDictionary<string, byte>(StringComparer.Ordinal);

        Parallel.For(0, count, index =>
        {
            var file = run.Files[index];
            var relative = run.Relative(file);
            relatives[index] = relative;
            var raw = run.ReadText(file);
            raws[index] = raw;
            var errors = firstErrors[index];
            var meta = ParseBlock(raw, relative, errors.Add);

            if (!JsonValues.IsTruthy(meta))
            {
                return;
            }

            isPage[index] = true;
            metas[index] = meta;

            foreach (var home in RefusalHomes(meta))
            {
                refusalHomes.TryAdd(home, 0);
            }

            // Frontmatter is validated through the shared schema module against the
            // record schema, so the schema is the one statement of every record shape.
            foreach (var problem in JsonSchema.Problems(run.RecordSchemaFile, meta, relative, run.ReadSchema))
            {
                errors.Add(problem);
            }
        });

        schemaWatch.Stop();
        Timing.Report($"[time] consumer-pages-schema {schemaWatch.Elapsed.TotalSeconds:0.0}s");

        var sectionsWatch = System.Diagnostics.Stopwatch.StartNew();

        Parallel.For(0, count, index =>
        {
            if (!isPage[index])
            {
                return;
            }

            var relative = relatives[index];
            var raw = raws[index];
            var meta = metas[index];
            var errors = secondErrors[index];

            // The lines split once and the sections collect in one pass: each lookup
            // below used to re-split the whole page for one section name.
            var lines = SplitLines(raw);
            HeadingChecks.CheckHeadings(relative, lines, errors.Add);

            var type = JsonRead.PropString(meta as JsonObject, "type");
            var required = type is not null && RequiredSections.TryGetValue(type, out var names) ? names : Array.Empty<string>();
            var bodies = SectionBodies(lines, required.Append("Goal").Append("What Happens").Append("Example").Append("Consequences").Append("Exit"));

            foreach (var name in required)
            {
                if (bodies[name] is null)
                {
                    errors.Add($"{relative}: type '{type}' requires an H2 '{name}'");
                }
            }

            // An exception page carries the section of its kind: a departure states
            // what is done instead in Consequences, a gap states its closing work in
            // Exit. The template names both and keeps one.
            if (type == "exception" && JsonRead.PropString(meta as JsonObject, "kind") is { } kind)
            {
                var want = kind == "departure" ? "Consequences" : kind == "gap" ? "Exit" : null;

                if (want is not null && bodies[want] is null)
                {
                    errors.Add($"{relative}: type 'exception' requires an H2 '{want}'");
                }
            }

            // Goal and What Happens agree with the model check: only a significant
            // use case carries them, and a query states its goal and no behavior.
            if (type == "use-case" && SignificantUseCase(meta as JsonObject, refusalHomes))
            {
                if (bodies["Goal"] is null)
                {
                    errors.Add($"{relative}: type 'use-case' requires an H2 'Goal'");
                }

                if (JsonRead.PropString(meta as JsonObject, "kind") != "query" && bodies["What Happens"] is null)
                {
                    errors.Add($"{relative}: type 'use-case' requires an H2 'What Happens'");
                }
            }

            // The Example section stays a narrative, not a second specification: it
            // illustrates the page within the word bound and carries no authority.
            if (bodies["Example"] is string example)
            {
                var words = Prose.Words(example).Count;

                if (words > exampleLimit)
                {
                    errors.Add($"{relative}: Example section has {words} words, over the {exampleLimit} word bound");
                }
            }
        });

        sectionsWatch.Stop();
        Timing.Report($"[time] consumer-sections-headings {sectionsWatch.Elapsed.TotalSeconds:0.0}s");

        for (var index = 0; index < count; index++)
        {
            foreach (var problem in firstErrors[index])
            {
                run.Err(problem);
            }

            if (!isPage[index])
            {
                continue;
            }

            run.Pages += 1;

            foreach (var problem in secondErrors[index])
            {
                run.Err(problem);
            }
        }
    }

    /// <summary>
    ///     Whether the use-case page needs Goal and What Happens prose, mirroring the
    ///     model check: it changes more than one aggregate, refuses beyond the generic
    ///     refusals, carries risk, or runs inline effects. A retired use case needs none.
    /// </summary>
    /// <param name="meta">The page frontmatter.</param>
    /// <param name="refusalHomes">The refusal-result homes every page names.</param>
    /// <returns>True when the use case is significant.</returns>
    static bool SignificantUseCase(JsonObject? meta, System.Collections.Concurrent.ConcurrentDictionary<string, byte> refusalHomes)
    {
        if (meta is null || JsonRead.PropString(meta, "status") == "retired")
        {
            return false;
        }

        if (IdCount(meta, "changes") > 1 || HasRisk(meta))
        {
            return true;
        }

        if (meta.TryGetPropertyValue("effects", out var effects) && effects is JsonArray entries)
        {
            foreach (var entry in entries)
            {
                if (entry is JsonObject edge
                    && JsonRead.PropString(edge, "timing") is { } timing
                    && timing.Equals("inline", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return JsonRead.PropString(meta, "id") is { } id
            && Identifiers.TrySplit(id, out _, out _, out var name)
            && refusalHomes.ContainsKey(name);
    }

    /// <summary>One id or id array as a count: a single id counts one.</summary>
    /// <param name="meta">The page frontmatter.</param>
    /// <param name="key">The property name.</param>
    /// <returns>The count the property carries.</returns>
    static int IdCount(JsonObject meta, string key)
    {
        if (!meta.TryGetPropertyValue(key, out var value))
        {
            return 0;
        }

        if (value is JsonArray array)
        {
            return array.Count;
        }

        return JsonRead.AsString(value) is { Length: > 0 } ? 1 : 0;
    }

    /// <summary>Whether the page carries any risk, which makes its record tier 1.</summary>
    /// <param name="meta">The page frontmatter.</param>
    /// <returns>True when the page carries any risk.</returns>
    static bool HasRisk(JsonObject meta)
    {
        if (!meta.TryGetPropertyValue("risk", out var risk))
        {
            return false;
        }

        if (risk is JsonArray array)
        {
            return array.Count > 0;
        }

        return JsonRead.AsString(risk) is { Length: > 0 };
    }

    /// <summary>Every refusal-result home the page's records name, each a use-case own name.</summary>
    /// <param name="meta">The page frontmatter.</param>
    /// <returns>The refusal homes the page names.</returns>
    static List<string> RefusalHomes(JsonNode? meta)
    {
        if (meta is not JsonObject page
            || !page.TryGetPropertyValue("records", out var records)
            || records is not JsonArray entries)
        {
            return [];
        }

        var homes = new List<string>();

        foreach (var entry in entries)
        {
            if (entry is not JsonObject record
                || JsonRead.PropString(record, "type") != "result"
                || JsonRead.PropString(record, "outcome") != "refusal"
                || JsonRead.PropString(record, "id") is not { } id
                || !Identifiers.TrySplit(id, out _, out var home, out _)
                || home is null)
            {
                continue;
            }

            homes.Add(home);
        }

        return homes;
    }

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
    ///     Reads the <c>---</c> delimited JSON frontmatter block. A file that carries a record in
    ///     any other wrapper, or carries none at all, is reported rather than skipped,
    ///     because a silently skipped page is an unvalidated page: the run passes while
    ///     that page sits unchecked beside every page that was checked.
    /// </summary>
    /// <param name="raw">The page text.</param>
    /// <param name="relative">The page path the findings name.</param>
    /// <param name="add">Collects one finding.</param>
    /// <returns>The parsed frontmatter, or null when it cannot be read.</returns>
    public static JsonNode? ParseBlock(string raw, string relative, Action<string> add)
    {
        if (!raw.StartsWith("---", StringComparison.Ordinal))
        {
            var head = raw.Length > 2000 ? raw.Substring(0, 2000) : raw;

            if (FencedBlock.IsMatch(head))
            {
                add($"{relative}: frontmatter block is not delimited by '---'");
            }
            else
            {
                add($"{relative}: no frontmatter block; every page opens with a '---' JSON block carrying its record type or page style");
            }

            return null;
        }

        var end = raw.IndexOf("\n---", 3, StringComparison.Ordinal);

        if (end < 0)
        {
            add($"{relative}: unterminated frontmatter block");
            return null;
        }

        try
        {
            return JsonNode.Parse(raw.Substring(3, end - 3).Trim());
        }
        catch (JsonException cause)
        {
            add($"{relative}: JSON parse error: {cause.Message}");
            return null;
        }
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
