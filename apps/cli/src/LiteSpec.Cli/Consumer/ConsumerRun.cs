using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Model;

namespace LiteSpec.Consumer;

/// <summary>
///     The mutable state of the consumer pass inside <c>model check</c>: the roots,
///     the findings, and the one text cache every read passes through so a file the
///     checks revisit is read once and reused. One instance is passed to every check
///     of the pass, and the caller reports the collected findings in the one finding
///     shape.
/// </summary>
/// <follows>litespec/core/authoring#declare-structured-specification-metadata</follows>
/// <follows>litespec/core/authoring#record-the-reviewed-litespec-release</follows>
sealed class ConsumerRun
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerRun"/> class.
    /// </summary>
    /// <param name="root">The consumer repository root.</param>
    public ConsumerRun(string root)
    {
        Root = root;
    }

    /// <summary>Gets the consumer repository root.</summary>
    public string Root { get; }

    /// <summary>Gets the findings the checks reported.</summary>
    public List<Finding> Errors { get; } = [];

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
    public string DocsPath { get; set; } = ".litespec";

    /// <summary>Gets or sets the documentation root as a full path.</summary>
    public string DocsRoot { get; set; } = "";

    /// <summary>
    ///     Gets the glossary file as a full path: the configured <c>paths.glossary</c>,
    ///     or the glossary beside the documentation root.
    /// </summary>
    public string GlossaryFile
    {
        get
        {
            var paths = (Project as JsonObject)?["paths"] as JsonObject;
            var glossaryPath = JsonRead.PropString(paths, "glossary");
            return Path.GetFullPath(glossaryPath is not null ? Path.Join(Root, glossaryPath) : Path.Join(DocsRoot, "glossary.yaml"));
        }
    }

    /// <summary>Gets the record files and Markdown files the page scan collected.</summary>
    public List<string> Files { get; } = [];

    /// <summary>Gets or sets the count of record files and documents the scan validated.</summary>
    public int Pages { get; set; }

    /// <summary>Gets the normalized extension selections.</summary>
    public List<Selection> Selections { get; } = [];

    /// <summary>Gets the frontend stack ids the pinned release ships.</summary>
    public List<string> KnownStacks { get; } = [];

    /// <summary>Gets the declared frontends on a controlled platform.</summary>
    public List<JsonNode?> ControlledFrontends { get; } = [];

    /// <summary>Gets or sets the declared shared UI package.</summary>
    public JsonNode? UiPackage { get; set; }

    /// <summary>Gets or sets the glossary summary the glossary check writes.</summary>
    public string GlossarySummary { get; set; } = "";

    /// <summary>
    ///     Runs every check of the consumer pass in order, collecting the findings.
    ///     A malformed input ends the pass through <see cref="FailFast"/>; the
    ///     caller prints the carried message and returns the carried exit code.
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
                FailInput($"Schema '{Relative(StandardsRoot, schemaFile)}' is missing from the pinned LiteSpec release; the check cannot run without it");
            }
        }

        // The consumer must contain its project record.
        var projectFile = ConsumerRoot.ProjectFile(Root);

        if (!File.Exists(projectFile))
        {
            throw new FailFast(2, $"No .litespec/project.json at {Root}");
        }

        try
        {
            Project = ReadJson(projectFile);
        }
        catch (Exception cause) when (cause is JsonException or IOException or UnauthorizedAccessException)
        {
            FailInput($".litespec/project.json: not valid JSON ({cause.Message})");
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
            Err(new Finding(
                "project-path-invalid",
                $"Scanned no record or Markdown files under '{DocsPath}'; check paths.docs, because a consumer with no page is a misconfigured scan",
                ConsumerRoot.ProjectFile(Root),
                1,
                null));
        }

        projectWatch.Stop();
        Timing.Report($"[time] consumer-project {projectWatch.Elapsed.TotalSeconds:0.0}s");

        PageChecks.CheckPages(this);
        ManifestSelectionChecks.CheckStageDeferrals(this);
        GlossaryChecks.CheckGlossary(this);

        var uiWatch = System.Diagnostics.Stopwatch.StartNew();
        ControlledUiChecks.CheckControlledUi(this);
        uiWatch.Stop();
        Timing.Report($"[time] consumer-ui {uiWatch.Elapsed.TotalSeconds:0.0}s");
        Timing.Report($"[time] consumer total {total.Elapsed.TotalSeconds:0.0}s");
    }

    /// <summary>
    ///     Records one finding. Every finding carries its code, level, path, line
    ///     and subject, the one finding shape the model check reports. Consumer
    ///     findings read pages only and always block, so G.
    /// </summary>
    /// <param name="finding">The finding to record.</param>
    public void Err(Finding finding) => Errors.Add(finding);

    /// <summary>
    ///     Records one schema problem list as findings of one code. The schema
    ///     evaluator prefixes each problem with the location it was given, which
    ///     the finding carries as its path instead.
    /// </summary>
    /// <param name="code">The finding code.</param>
    /// <param name="file">The validated file as a full path.</param>
    /// <param name="location">The location prefix the problems carry.</param>
    /// <param name="problems">The schema problems.</param>
    /// <param name="subject">The record id the findings are about, when there is one.</param>
    public void Schema(string code, string file, string location, List<string> problems, string? subject = null)
    {
        foreach (var problem in problems)
        {
            Err(SchemaFinding(code, file, location, problem, subject));
        }
    }

    /// <summary>
    ///     One schema problem as a finding of one code.
    /// </summary>
    /// <param name="code">The finding code.</param>
    /// <param name="file">The validated file as a full path.</param>
    /// <param name="location">The location prefix the problem carries.</param>
    /// <param name="problem">The schema problem.</param>
    /// <param name="subject">The record id the finding is about, when there is one.</param>
    /// <returns>The finding.</returns>
    public static Finding SchemaFinding(string code, string file, string location, string problem, string? subject)
    {
        var prefix = location + ": ";
        var message = problem.StartsWith(prefix, StringComparison.Ordinal) ? problem[prefix.Length..] : problem;
        return new Finding(code, message, file, 1, subject);
    }

    /// <summary>
    ///     Reads a file once and reuses the text. Strips a leading byte-order mark,
    ///     which some editors still write: without the strip a document that carries
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
    ///     Ends the pass as a usage error, carrying the path and the parse message
    ///     out to the caller. The pass writes nothing itself, so the caller's
    ///     output contract stays intact whatever format it reports in.
    /// </summary>
    /// <param name="message">The problem text.</param>
    public static void FailInput(string message) => throw new FailFast(2, message);
}
