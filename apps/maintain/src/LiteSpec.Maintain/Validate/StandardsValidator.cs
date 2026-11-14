using System.Text.Json.Nodes;
using LiteSpec.CommandLine;
using LiteSpec.Maintain.Validate.Pages;
using LiteSpec.Standards;

namespace LiteSpec.Maintain.Validate;

/// <summary>
///     The <c>litespec-maintain validate</c> command: the authoring checks over the LiteSpec repository.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class StandardsValidator
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain validate [--root <path>] [--format json]

        Validates the authoring contract over the LiteSpec repository at --root,
        which defaults to the LiteSpec checkout.

          --root <path>   The repository root. Defaults to the LiteSpec checkout.
          --format json   Write one JSON object on stdout instead of human-readable lines.
          --help          Print this text and exit.

        Exit codes: 0 no error, 1 at least one error, 2 usage error.
        """;

    /// <summary>Every diagnostic code the validator can emit, in declaration order.</summary>
    public static readonly IReadOnlyList<string> StableDiagnosticCodes = MaintainerDiagnostics.All;

    /// <summary>Validates the repository at <paramref name="root"/> and returns its sorted diagnostics.</summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The validation result.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Validate reports an unreadable repository root or input walk as a result error instead of crashing the validation run on malformed repository input.")]
    public static StandardsResult Validate(string root)
    {
        var resolved = Path.GetFullPath(root.Length == 0 ? "." : root);
        var diagnostics = new List<StandardsDiagnostic>();
        void Add(string relative, int line, string code, string message)
        {
            diagnostics.Add(new StandardsDiagnostic(FileText.Slash(relative), line, code, message));
        }

        if (!Directory.Exists(resolved))
        {
            return new StandardsResult(diagnostics, $"Repository root is not a directory: {resolved}");
        }

        try
        {
            _ = Directory.GetFileSystemEntries(resolved);
        }
        catch (Exception exception)
        {
            return new StandardsResult(diagnostics, $"Repository root is unreadable: {resolved}: {exception.Message}");
        }

        List<string> markdown;
        try
        {
            markdown = RepositoryWalk.Walk(resolved, file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            return new StandardsResult(diagnostics, $"Repository input is unreadable: {exception.Message}");
        }

        var context = new ValidationContext(resolved, Add);
        var current = markdown.Where(file => RepositoryWalk.IsCurrentStandardsMaterial(PathText.Relative(resolved, file))).ToList();
        var parsedPages = new List<ParsedPage>();
        foreach (var file in current)
        {
            var relative = PathText.Relative(resolved, file);
            if (relative.StartsWith("templates/standard/", StringComparison.Ordinal))
            {
                continue;
            }

            if (context.Templates.ReservedTemplateStems.Contains(relative))
            {
                Add(relative, 1, MaintainerDiagnostics.Codes.IdPageFilename, $"'{relative}' is the virtual path an authoring template parses as; rename the page so its scope is its own");
                continue;
            }

            var raw = FileText.ReadText(file);
            var reader = PageReader.Parse(raw);
            var prose = string.Join('\n', reader.Lines.Select((line, index) =>
                reader.FencedLines.Contains(index + 1) || reader.GeneratedLines.Contains(index + 1) ? "" : line));
            var nonAscii = PageText.NonAsciiOffset(prose);
            if (nonAscii >= 0)
            {
                Add(relative, PageText.LineNumber(prose, nonAscii), MaintainerDiagnostics.Codes.ProseNonAscii, PageText.NonAsciiMessage(prose[nonAscii], "non-ASCII character "));
            }

            parsedPages.Add(new ParsedPage(relative, reader, context.Pages.ParsePage(relative, reader)));
        }

        context.Repository.CheckPageIdentity(parsedPages);
        context.Repository.CheckPackagedPathLength();

        var activeIds = new HashSet<string>(context.GlobalIds.Keys, StringComparer.Ordinal);
        context.Repository.CheckCitations(parsedPages, activeIds);
        context.Marks.CheckMarks(activeIds);

        context.Templates.CheckAuthoringTemplates();
        context.Templates.CheckProvisionCitations(activeIds);
        context.Links.CheckLinks(current);
        // The docs cite the tools, the templates and the example, so each citation is a lookup
        // against data the tools already hold. (litespec/core/authoring#run-repeatable-authoring-checks)
        context.Repository.CheckFindingCitations(parsedPages);
        context.Repository.CheckTemplateCitations(parsedPages);
        context.Repository.CheckExampleCitations(parsedPages);
        context.Repository.CheckCommandOptions(parsedPages);
        context.Repository.CheckIndexCoverage(current);
        var provisionsByPage = parsedPages.ToDictionary(page => page.Relative, page => page.Parsed.Provisions, StringComparer.Ordinal);
        context.Manifest.CheckManifest(activeIds, provisionsByPage);
        context.Manifest.CheckAnalyzerRelease(parsedPages);
        context.Records.CheckRecords(parsedPages, activeIds);

        // The provision index and its data file are derived. A stale page would send a
        // reader to the wrong heading, so the gate compares both with the active standards.
        // (litespec/core/authoring#regenerate-the-provision-index)
        var indexFile = RepositoryWalk.JoinRoot(resolved, Provisions.IndexPath);
        if (File.Exists(indexFile))
        {
            var expected = Provisions.BuildIndex(resolved) + "\n";
            var actual = FileText.ReadText(indexFile).Replace("\r\n", "\n", StringComparison.Ordinal);
            if (actual != expected)
            {
                Add(Provisions.IndexPath, 1, MaintainerDiagnostics.Codes.ProvisionsStale, "generated provision index differs from the active standards; run litespec-maintain provisions");
            }
        }
        else if (Directory.Exists(Path.Combine(resolved, "docs", "reference")))
        {
            Add(Provisions.IndexPath, 1, MaintainerDiagnostics.Codes.ProvisionsStale, "generated provision index is missing; run litespec-maintain provisions");
        }

        var jsonFile = RepositoryWalk.JoinRoot(resolved, Provisions.JsonPath);
        if (File.Exists(jsonFile))
        {
            var expectedJson = Provisions.BuildJson(resolved) + "\n";
            var actualJson = FileText.ReadText(jsonFile).Replace("\r\n", "\n", StringComparison.Ordinal);
            if (actualJson != expectedJson)
            {
                Add(Provisions.JsonPath, 1, MaintainerDiagnostics.Codes.ProvisionsStale, "generated provision data differs from the active standards; run litespec-maintain provisions");
            }
        }
        else if (Directory.Exists(Path.Combine(resolved, "docs", "reference")))
        {
            Add(Provisions.JsonPath, 1, MaintainerDiagnostics.Codes.ProvisionsStale, "generated provision data is missing; run litespec-maintain provisions");
        }

        // The finding code, folder and pin tables are written from the code and the manifest, so a
        // table that differs from them is stale like the index. (litespec/core/authoring#regenerate-the-provision-index)
        if (Directory.Exists(Path.Combine(resolved, "docs", "reference")))
        {
            try
            {
                foreach (var (path, text) in ReferenceTables.Outputs(resolved))
                {
                    var file = RepositoryWalk.JoinRoot(resolved, path);
                    if (!File.Exists(file) || FileText.ReadText(file).Replace("\r\n", "\n", StringComparison.Ordinal) != text)
                    {
                        Add(path, 1, MaintainerDiagnostics.Codes.ProvisionsStale, "generated table differs from its source; run litespec-maintain provisions");
                    }
                }
            }
            catch (InvalidOperationException exception)
            {
                Add(ReferenceTables.ManifestPath, 1, MaintainerDiagnostics.Codes.ProvisionsStale, exception.Message);
            }
        }

        context.Manifest.ValidateSchemaConsumer("schemas/litespec-manifest.schema.yaml", "litespec.manifest.yaml");
        context.Manifest.CheckSchemaIds();
        context.Fields.CheckFieldNaming();
        // Every shipped consumer template meets the schema it teaches. A template
        // the schema never reads drifts from it in silence.
        context.Manifest.ValidateSchemaConsumer("schemas/litespec-project.schema.yaml", "templates/consumer/project.yaml");

        // The template index is the only map from a template file to the consumer path
        // it targets. A renamed template leaves a row pointing at nothing, and that row
        // reads exactly like a correct one. (litespec/core/principles#keep-one-authored-source)
        context.Templates.CheckTemplateIndex();
        context.Templates.CheckConsumerTemplates();
        context.Templates.CheckTemplateSources();
        context.Repository.CheckGeneratedCounts(parsedPages);
        context.ValueStyle.CheckValueStyle();

        // A reader who cannot tell a tutorial from a reference opens both and trusts
        // neither, so the documentation root names each class before it links to one.
        // (litespec/core/authoring#route-the-reader-before-listing-pages)
        context.Repository.CheckRootIndexRouting();

        // A stable sort: diagnostics that share a file, line, and code keep the order
        // the checks emitted them in, as the reference implementation's sort does.
        var sorted = diagnostics
            .OrderBy(diagnostic => diagnostic.Relative, RepositoryWalk.SortOrder)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Code, RepositoryWalk.SortOrder)
            .ToList();
        return new StandardsResult(sorted, null);
    }

    /// <summary>
    ///     Runs the <c>validate</c> command over the remaining arguments. Returns the exit code.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Run is the validate command handler; it prints any failure to resolve the repository root and returns exit code 2 instead of crashing.")]
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, [], ["--root", "--format"]);
        if (parsed.Help)
        {
            Console.WriteLine(Usage);
            return 0;
        }

        var error = parsed.Error ?? parsed.FormatError;
        if (error is not null)
        {
            Console.Error.WriteLine(error);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string root;
        try
        {
            root = parsed.Value("--root") is string given ? Path.GetFullPath(given) : LiteSpecHome.RequireRoot();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine(Usage);
            return 2;
        }

        var result = Validate(root);
        if (result.UsageError is not null)
        {
            Console.Error.WriteLine(result.UsageError);
            return 2;
        }

        var errors = result.Diagnostics;

        // A machine reader needs the file, the line, and the stable code as fields. A
        // reader that has to parse the human lines back apart breaks the first time a
        // message is reworded, so the tool emits the structure it already holds.
        if (parsed.Json)
        {
            CliJson.Write(new JsonObject
            {
                ["tool"] = "validate",
                ["root"] = root,
                ["ok"] = errors.Count == 0,
                ["errors"] = new JsonArray([.. errors.Select(diagnostic => (JsonNode?)new JsonObject
                {
                    ["file"] = diagnostic.Relative,
                    ["line"] = diagnostic.Line,
                    ["code"] = diagnostic.Code,
                    ["message"] = diagnostic.Message,
                })]),
            });
            return errors.Count == 0 ? 0 : 1;
        }

        foreach (var diagnostic in errors)
        {
            Console.Error.WriteLine($"{diagnostic.Relative}:{diagnostic.Line} [{diagnostic.Code}] {diagnostic.Message}");
        }

        if (errors.Count > 0)
        {
            Console.Error.WriteLine($"LiteSpec authoring checks failed with {errors.Count} error(s).");
            return 1;
        }

        Console.WriteLine("LiteSpec authoring checks passed.");
        return 0;
    }
}
