using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Consumer;

/// <summary>
///     The mutable state of one run: the roots, the findings, and the one text cache
///     every read passes through so a file the checks revisit is read once and reused.
///     One instance is passed to every check of the run.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
/// <follows>litespec/core/authoring#reject-a-recorded-synonym-inside-its-scope</follows>
sealed class ConsumerRun
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerRun"/> class.
    /// </summary>
    /// <param name="root">The consumer repository root.</param>
    /// <param name="json">Whether the report is written as JSON.</param>
    public ConsumerRun(string root, bool json)
    {
        Root = root;
        Json = json;
    }

    /// <summary>Gets the consumer repository root.</summary>
    public string Root { get; }

    /// <summary>Gets a value indicating whether gets whether the report is written as JSON.</summary>
    public bool Json { get; }

    /// <summary>Gets the problems the checks reported.</summary>
    public List<string> Errors { get; } = [];

    /// <summary>Gets the advisories the checks reported.</summary>
    public List<string> Advisories { get; } = [];

    /// <summary>Gets the file texts read so far, by file.</summary>
    public Dictionary<string, string> TextCache { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets the schemas read so far, by file.</summary>
    public Dictionary<string, JsonNode?> SchemaCache { get; } = new(StringComparer.Ordinal);

    // The per-file checks run in parallel, so both caches guard with a lock.
    // The file read and parse stay outside the lock; a race re-reads one file.
    /// <summary>The lock guarding <see cref="TextCache"/>.</summary>
    readonly object TextLock = new();

    /// <summary>The lock guarding <see cref="SchemaCache"/>.</summary>
    readonly object SchemaLock = new();

    /// <summary>Gets or sets the LiteSpec release root.</summary>
    public string StandardsRoot { get; set; } = "";

    /// <summary>Gets or sets the project schema file.</summary>
    public string ProjectSchemaFile { get; set; } = "";

    /// <summary>Gets or sets the record schema file.</summary>
    public string RecordSchemaFile { get; set; } = "";

    /// <summary>Gets or sets the glossary schema file.</summary>
    public string GlossarySchemaFile { get; set; } = "";

    /// <summary>Gets or sets the parsed project record.</summary>
    public JsonNode? Project { get; set; }

    /// <summary>Gets or sets the configured documentation root, as recorded.</summary>
    public string DocsPath { get; set; } = "docs";

    /// <summary>Gets or sets the documentation root as a full path.</summary>
    public string DocsRoot { get; set; } = "";

    /// <summary>Gets the Markdown files the page scan collected.</summary>
    public List<string> Files { get; } = [];

    /// <summary>Gets or sets how many files carry a frontmatter page.</summary>
    public int Pages { get; set; }

    /// <summary>Gets the normalized extension selections.</summary>
    public List<Selection> Selections { get; } = [];

    /// <summary>Gets the frontend stack ids the pinned release ships.</summary>
    public List<string> KnownStacks { get; } = [];

    /// <summary>Gets the declared frontends on a controlled platform.</summary>
    public List<JsonNode?> ControlledFrontends { get; } = [];

    /// <summary>Gets or sets the declared shared UI package.</summary>
    public JsonNode? UiPackage { get; set; }

    /// <summary>Gets or sets the glossary line the human report prints.</summary>
    public string GlossarySummary { get; set; } = "";

    /// <summary>Gets today as an ISO date.</summary>
    public string Today { get; } = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>
    ///     Runs every check of the consumer validator in order.
    /// </summary>
    public void Execute()
    {
        StandardsRoot = LiteSpecHome.RequireRoot();
        ProjectSchemaFile = LiteSpecHome.PathTo("schemas/litespec-project.schema.json");
        RecordSchemaFile = LiteSpecHome.PathTo("schemas/record.schema.json");
        GlossarySchemaFile = LiteSpecHome.PathTo("schemas/glossary.schema.json");

        foreach (var schemaFile in new[] { ProjectSchemaFile, RecordSchemaFile, GlossarySchemaFile })
        {
            if (!File.Exists(schemaFile))
            {
                FailInput($"Schema '{Relative(StandardsRoot, schemaFile)}' is missing from the pinned LiteSpec release; the validator cannot run without it");
            }
        }

        // The consumer must contain its project record.
        var projectFile = Path.Join(Root, "litespec.project.json");

        if (!File.Exists(projectFile))
        {
            Console.Error.WriteLine($"No litespec.project.json at {Root}");
            throw new FailFast(2);
        }

        try
        {
            Project = ReadJson(projectFile);
        }
        catch (Exception cause) when (cause is JsonException or IOException or UnauthorizedAccessException)
        {
            FailInput($"litespec.project.json: not valid JSON ({cause.Message})");
        }

        var total = System.Diagnostics.Stopwatch.StartNew();
        var projectWatch = System.Diagnostics.Stopwatch.StartNew();
        ProjectFileChecks.CheckProjectRecord(this);
        ManifestSelectionChecks.CheckManifest(this);
        FrontendChecks.CheckFrontends(this);
        PageChecks.CollectFiles(this);

        // Every check below reads this list, so an empty list agrees with everything. A
        // consumer with no page does not exist; a root that resolves to nothing does.
        if (Files.Count == 0)
        {
            Err($"Scanned no Markdown files under '{DocsPath}'; check paths.docs, because a consumer with no page is a misconfigured scan");
        }

        projectWatch.Stop();
        Timing.Report($"[time] consumer-project {projectWatch.Elapsed.TotalSeconds:0.0}s");

        PageChecks.CheckPages(this);
        ManifestSelectionChecks.CheckStageDeferrals(this);
        GlossaryChecks.CheckGlossaryAndProse(this);

        var uiWatch = System.Diagnostics.Stopwatch.StartNew();
        ControlledUiChecks.CheckControlledUi(this);
        uiWatch.Stop();
        Timing.Report($"[time] consumer-ui {uiWatch.Elapsed.TotalSeconds:0.0}s");
        Timing.Report($"[time] consumer total {total.Elapsed.TotalSeconds:0.0}s");
    }

    /// <summary>
    ///     Writes the machine-readable or human-readable report and returns the exit code.
    /// </summary>
    /// <returns>The exit code: 0 when no problem was reported, 1 otherwise.</returns>
    public int Report()
    {
        // A machine reader gets the problems as an array and the counts as fields, so
        // nothing has to be recovered by parsing the human lines back apart.
        // Every finding carries its level: problems read pages only and always
        // block, so G; advisories report and never block, so A.
        if (Json)
        {
            var payload = new JsonObject
            {
                ["tool"] = "consumer",
                ["consumer"] = Root,
                ["ok"] = Errors.Count == 0,
                ["scanned"] = new JsonObject { ["documentationRoot"] = DocsPath, ["files"] = Files.Count, ["pages"] = Pages },
                ["problems"] = new JsonArray([.. Errors.Select(error => (JsonNode?)$"[G] {error}")]),
                ["advisories"] = new JsonArray([.. Advisories.Select(advisory => (JsonNode?)$"[A] {advisory}")]),
            };

            CliJson.Write(payload);

            return Errors.Count > 0 ? 1 : 0;
        }

        Console.WriteLine($"Consumer: {Root}");
        Console.WriteLine($"Files scanned: {Files.Count} under {DocsPath}, frontmatter pages: {Pages}");
        Console.WriteLine(GlossarySummary);

        if (Advisories.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"ADVISORY ({Advisories.Count} finding(s))");

            foreach (var advisory in Advisories)
            {
                Console.WriteLine($"  - [A] {advisory}");
            }
        }

        if (Errors.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"FAIL ({Errors.Count} problem(s)):");

            foreach (var problem in Errors)
            {
                Console.WriteLine($"  - [G] {problem}");
            }

            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("PASS: project configuration, page frontmatter and glossary are valid.");
        return 0;
    }

    /// <summary>
    ///     Records one problem.
    /// </summary>
    /// <param name="message">The problem text.</param>
    public void Err(string message) => Errors.Add(message);

    /// <summary>
    ///     Records one advisory.
    /// </summary>
    /// <param name="message">The advisory text.</param>
    public void Advise(string message) => Advisories.Add(message);

    /// <summary>
    ///     Reads a file once and reuses the text. Strips a leading byte-order mark,
    ///     which some editors still write: without the strip a page that carries
    ///     a frontmatter block reports none.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The file text.</returns>
    public string ReadText(string file)
    {
        lock (TextLock)
        {
            if (TextCache.TryGetValue(file, out var cached))
            {
                return cached;
            }
        }

        var text = ReadShared(file);

        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        lock (TextLock)
        {
            if (TextCache.TryGetValue(file, out var raced))
            {
                return raced;
            }

            TextCache[file] = text;
            return text;
        }
    }

    /// <summary>
    ///     Reads a file that an editor or an agent may be saving at the same moment:
    ///     a sharing violation is retried for about a second before it is thrown.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The file text.</returns>
    static string ReadShared(string file)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return File.ReadAllText(file);
            }
            catch (IOException) when (attempt < 10 && File.Exists(file))
            {
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    ///     Reads and parses a JSON file through the text cache.
    /// </summary>
    /// <param name="file">The file to read.</param>
    /// <returns>The parsed JSON value.</returns>
    public JsonNode? ReadJson(string file) => JsonNode.Parse(ReadText(file));

    /// <summary>
    ///     Reads a schema once and reuses the parsed shape: every page validates
    ///     against the same record schema, so parsing it per page re-reads one
    ///     file thousands of times. The evaluator never writes to a schema.
    /// </summary>
    /// <param name="file">The schema file to read.</param>
    /// <returns>The parsed schema.</returns>
    public JsonNode? ReadSchema(string file)
    {
        lock (SchemaLock)
        {
            if (SchemaCache.TryGetValue(file, out var cached))
            {
                return cached;
            }
        }

        var schema = JsonNode.Parse(File.ReadAllText(file));

        lock (SchemaLock)
        {
            if (SchemaCache.TryGetValue(file, out var raced))
            {
                return raced;
            }

            SchemaCache[file] = schema;
            return schema;
        }
    }

    /// <summary>
    ///     The path of a file relative to the consumer root.
    /// </summary>
    /// <param name="file">The file to make relative.</param>
    /// <returns>The forward-slash relative path.</returns>
    public string Relative(string file) => Relative(Root, file);

    /// <summary>
    ///     The path of a file relative to a root.
    /// </summary>
    /// <param name="root">The root the path is made relative to.</param>
    /// <param name="file">The file to make relative.</param>
    /// <returns>The forward-slash relative path.</returns>
    public static string Relative(string root, string file) =>
        Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>
    ///     Ends the run as a usage error with the path and the parse message. A stack
    ///     trace would break the <c>--format json</c> contract a machine reader relies on.
    /// </summary>
    /// <param name="message">The problem text.</param>
    public void FailInput(string message)
    {
        if (Json)
        {
            CliJson.Write(new
            {
                tool = "consumer",
                consumer = Root,
                ok = false,
                scanned = new { documentationRoot = "docs", files = 0, pages = 0 },
                problems = new[] { $"[G] {message}" },
                advisories = Array.Empty<string>(),
            });
        }
        else
        {
            Console.Error.WriteLine(message);
        }

        throw new FailFast(2);
    }
}
