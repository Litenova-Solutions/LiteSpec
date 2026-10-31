namespace LiteSpec.Authoring;

/// <summary>
///     The <c>litespec-maintain validate</c> command: the authoring checks over the LiteSpec repository.
/// </summary>
/// <follows>litespec/core/authoring#run-repeatable-authoring-checks</follows>
internal static class StandardsValidator
{
    /// <summary>The command's usage text.</summary>
    public const string Usage = """
        Usage: litespec-maintain validate [--root <path>] [--warnings] [--format json]

        Validates the authoring contract over the LiteSpec repository at --root,
        which defaults to the LiteSpec checkout.

          --root <path>   The repository root. Defaults to the LiteSpec checkout.
          --warnings      List every warning occurrence instead of a count per code.
          --format json   Write one JSON object on stdout instead of human-readable lines.
          --help          Print this text and exit.

        Exit codes: 0 no error, 1 at least one error, 2 usage error.
        """;

    /// <summary>Every diagnostic code the validator can emit, in declaration order.</summary>
    public static readonly IReadOnlyList<string> StableDiagnosticCodes =
    [
        "ANCHOR_BROKEN",
        "CHECKED_BY_UNKNOWN",
        "CONVENTION_DEFAULT_SENTENCE",
        "CONVENTION_MISSING_DEFAULT",
        "CONVENTION_MISSING_REPLACEMENT",
        "CONVENTION_REPLACEMENT_DEVIATION",
        "CONVENTION_REPLACEMENT_SENTENCE",
        "EXTENSION_ACTIVATION",
        "EXTENSION_BASELINE",
        "EXTENSION_DEPENDENCIES",
        "EXTENSION_KIND",
        "EXTENSION_REPLACES_ENFORCED",
        "EXTENSION_REPLACES_UNKNOWN",
        "EXTENSION_LOCATION",
        "EXTENSION_SCOPE",
        "EXTENSION_UNLISTED",
        "FIELD_NAME_SHAPE",
        "LAYER_UNREACHABLE",
        "SHAPE_PAGE",
        "STACK_FOLDER",
        "STACK_REFERENCE",
        "GLOSSARY_DEFINITION",
        "GLOSSARY_ORDER",
        "HEADING_ACTION",
        "HEADING_EMPTY_BODY",
        "ID_DUPLICATE",
        "ID_IN_HEADING",
        "ID_LINE_MISSING",
        "ID_LINE_MISMATCH",
        "ID_PAGE_FILENAME",
        "ID_REMOVED_STILL_PRESENT",
        "ID_REMOVED_UNRECORDED",
        "ID_RENAME_TARGET_MISSING",
        "ID_UNKNOWN_REFERENCE",
        "INDEX_CONTAINS_PROCEDURE",
        "INDEX_INTENT",
        "LINK_BROKEN",
        "MANIFEST_ANALYZER_PIN",
        "MANIFEST_ANALYZER_RULE",
        "MANIFEST_ANCHOR",
        "MANIFEST_DUPLICATE_PATH",
        "MANIFEST_JSON",
        "MANIFEST_PATH",
        "MARK_MISSING",
        "MARK_UNRESOLVED",
        "PAGE_EMPTY_SECTION",
        "PAGE_EMPTY_SECTION_TEXT",
        "PAGE_HEADING_CASE",
        "PAGE_MISSING_SECTION",
        "PAGE_SECTION_ORDER",
        "PAGE_TITLE_CASE",
        "PAGE_TITLE_COUNT",
        "PAGE_UNKNOWN_SECTION",
        "PATTERN_INVALID",
        "PROSE_NON_ASCII",
        "PROSE_NORMATIVE_LOCATION",
        "PROVISIONS_STALE",
        "PROVISION_RESTATES_HEADING",
        "RECORD_DECISION_UNCITED",
        "RECORD_ID_MISMATCH",
        "RECORD_SCHEMA",
        "RECORD_SOURCE",
        "RECORD_UNKNOWN_REFERENCE",
        "REFERENCE_EXAMPLE_DECLARATION",
        "SCHEMA_ID",
        "SCHEMA_INVALID",
        "SCHEMA_JSON",
        "SCHEMA_UNSUPPORTED_KEYWORD",
        "STANDARD_DEVIATION_NORMATIVE",
        "STANDARD_DEVIATION_SENTENCE",
        "STANDARD_EXAMPLE_LABEL",
        "STANDARD_INFORMATIVE_NORMATIVE",
        "STANDARD_LABEL_DUPLICATE",
        "STANDARD_LABEL_INVALID",
        "STANDARD_LABEL_ORDER",
        "STANDARD_MISSING_DEVIATION",
        "STANDARD_MISSING_REQUIREMENT",
        "STANDARD_MODAL_COUNT",
        "STANDARD_MODAL_VOCABULARY",
        "STANDARD_REQUIREMENT_COUNT",
        "STANDARD_SENTENCE_COUNT",
        "STANDARD_UNEXPECTED_DEVIATION",
        "STANDARD_UNLABELED_CONTENT",
        "VERIFY_COMMAND",
        "VERIFY_DUPLICATE_EVIDENCE",
        "VERIFY_DUPLICATE_ID",
        "VERIFY_GENERIC_EVIDENCE",
        "VERIFY_HEADER",
        "VERIFY_MISSING_ID",
        "VERIFY_ORDER",
        "VERIFY_TEMPLATED_EVIDENCE",
        "VALUE_STYLE",

        "TEMPLATE_INDEX_PATH",
        "TEMPLATE_SCHEMA",

        "INDEX_MISSING_ROUTING",
        "LAYER_UNLISTED",
    ];

    /// <summary>
    ///     Codes reported as warnings, which never fail the build. Each new rule lands
    ///     here while existing content still carries the old form, and burns its count
    ///     down as the pages are rewritten.
    /// </summary>
    public static readonly IReadOnlyList<string> WarningDiagnosticCodes =
    [];

    /// <summary>
    ///     Validates the repository at <paramref name="root"/> and returns its sorted diagnostics.
    ///     The released provision identifiers come from the highest release tag.
    /// </summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The validation result.</returns>
    public static StandardsResult Validate(string root) => Validate(root, null);

    /// <summary>
    ///     Validates the repository at <paramref name="root"/> against the given released
    ///     provision identifiers, or the highest release tag when <paramref name="releasedIds"/>
    ///     is null. Tests pass explicit identifiers so the removal check needs no git history.
    /// </summary>
    /// <param name="root">The repository root.</param>
    /// <param name="releasedIds">The released provision identifiers, or null.</param>
    /// <returns>The validation result.</returns>
    internal static StandardsResult Validate(string root, HashSet<string>? releasedIds)
    {
        var resolved = Path.GetFullPath(root.Length == 0 ? "." : root);
        var diagnostics = new List<StandardsDiagnostic>();
        var stable = new HashSet<string>(StableDiagnosticCodes, StringComparer.Ordinal);
        void Add(string relative, int line, string code, string message)
        {
            if (!stable.Contains(code))
            {
                throw new InvalidOperationException($"Validator emitted undeclared diagnostic code '{code}'");
            }

            diagnostics.Add(new StandardsDiagnostic(ValidatorSupport.Slash(relative), line, code, message));
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
            markdown = ValidatorSupport.Walk(resolved, file => file.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            return new StandardsResult(diagnostics, $"Repository input is unreadable: {exception.Message}");
        }

        var context = new ValidationContext(resolved, Add);
        var current = markdown.Where(file => ValidatorSupport.IsCurrentStandardsMaterial(ValidatorSupport.Slash(Path.GetRelativePath(resolved, file)))).ToList();
        var parsedPages = new List<ParsedPage>();
        foreach (var file in current)
        {
            var relative = ValidatorSupport.Slash(Path.GetRelativePath(resolved, file));
            if (relative.StartsWith("templates/standard/", StringComparison.Ordinal))
            {
                continue;
            }

            if (context.Repository.ReservedTemplateStems.Contains(relative))
            {
                Add(relative, 1, "ID_PAGE_FILENAME", $"'{relative}' is the virtual path an authoring template parses as; rename the page so its scope is its own");
                continue;
            }

            var raw = ValidatorSupport.ReadText(file);
            var reader = PageReader.Parse(raw);
            var prose = string.Join('\n', reader.Lines.Select((line, index) =>
                reader.FencedLines.Contains(index + 1) || reader.GeneratedLines.Contains(index + 1) ? "" : line));
            var nonAscii = ValidatorSupport.NonAsciiOffset(prose);
            if (nonAscii >= 0)
            {
                Add(relative, ValidatorSupport.LineNumber(prose, nonAscii), "PROSE_NON_ASCII", ValidatorSupport.NonAsciiMessage(prose[nonAscii], "non-ASCII character "));
            }

            parsedPages.Add(new ParsedPage(relative, reader, context.Pages.ParsePage(relative, reader)));
        }

        context.Repository.CheckPageIdentity(parsedPages);

        var activeIds = new HashSet<string>(context.GlobalIds.Keys, StringComparer.Ordinal);
        context.Repository.CheckCitations(parsedPages, activeIds);
        context.Marks.CheckMarks(activeIds);
        context.Repository.CheckRemovedProvisions(activeIds, releasedIds);

        context.Repository.CheckAuthoringTemplates();
        context.Repository.CheckLinks(current);
        var provisionsByPage = parsedPages.ToDictionary(page => page.Relative, page => page.Parsed.Provisions, StringComparer.Ordinal);
        context.Manifest.CheckManifest(activeIds, provisionsByPage);
        context.Manifest.CheckAnalyzerRelease(parsedPages);
        context.Records.CheckRecords(parsedPages);

        // The provision index is derived. A stale page would send a reader to the wrong
        // heading, so the gate compares it with the active standards.
        // (litespec/core/authoring#regenerate-the-provision-index)
        var indexFile = ValidatorSupport.JoinRoot(resolved, Provisions.IndexPath);
        if (File.Exists(indexFile))
        {
            var expected = Provisions.BuildIndex(resolved) + "\n";
            var actual = ValidatorSupport.ReadText(indexFile).Replace("\r\n", "\n", StringComparison.Ordinal);
            if (actual != expected)
            {
                Add(Provisions.IndexPath, 1, "PROVISIONS_STALE", "generated provision index differs from the active standards; run litespec-maintain provisions");
            }
        }
        else if (Directory.Exists(Path.Combine(resolved, "docs", "reference")))
        {
            Add(Provisions.IndexPath, 1, "PROVISIONS_STALE", "generated provision index is missing; run litespec-maintain provisions");
        }

        context.Manifest.ValidateSchemaConsumer("schemas/litespec-manifest.schema.json", "litespec.manifest.json");
        context.Manifest.CheckSchemaIds();
        context.Fields.CheckFieldNaming();
        // Every shipped consumer template meets the schema it teaches. A template
        // the schema never reads drifts from it in silence.
        context.Manifest.ValidateSchemaConsumer("schemas/litespec-project.schema.json", "templates/consumer/litespec.project.json");

        // The template index is the only map from a template file to the consumer path
        // it targets. A renamed template leaves a row pointing at nothing, and that row
        // reads exactly like a correct one. (litespec/core/principles#keep-one-authored-source)
        context.Repository.CheckTemplateIndex();
        context.Repository.CheckConsumerTemplates();
        context.ValueStyle.CheckValueStyle();

        // A reader who cannot tell a tutorial from a reference opens both and trusts
        // neither, so the documentation root names each class before it links to one.
        // (litespec/core/authoring#route-the-reader-before-listing-pages)
        context.Repository.CheckRootIndexRouting();

        // A stable sort: diagnostics that share a file, line, and code keep the order
        // the checks emitted them in, as the reference implementation's sort does.
        var sorted = diagnostics
            .OrderBy(diagnostic => diagnostic.Relative, ValidatorSupport.SortOrder)
            .ThenBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Code, ValidatorSupport.SortOrder)
            .ToList();
        return new StandardsResult(sorted, null);
    }

    /// <summary>
    ///     Runs the <c>validate</c> command over the remaining arguments. Returns the exit code.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code.</returns>
    public static int Run(string[] args)
    {
        var parsed = CliArgs.Parse(args, ["--warnings"], ["--root", "--format"]);
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

        var warningCodes = new HashSet<string>(WarningDiagnosticCodes, StringComparer.Ordinal);
        var errors = result.Diagnostics.Where(diagnostic => !warningCodes.Contains(diagnostic.Code)).ToList();
        var warnings = result.Diagnostics.Where(diagnostic => warningCodes.Contains(diagnostic.Code)).ToList();

        // A machine reader needs the file, the line, and the stable code as fields. A
        // reader that has to parse the human lines back apart breaks the first time a
        // message is reworded, so the tool emits the structure it already holds.
        if (parsed.Json)
        {
            CliJson.Write(new
            {
                tool = "validate",
                root,
                ok = errors.Count == 0,
                errors = errors.Select(diagnostic => new { file = diagnostic.Relative, line = diagnostic.Line, code = diagnostic.Code, message = diagnostic.Message }).ToList(),
                warnings = warnings.Select(diagnostic => new { file = diagnostic.Relative, line = diagnostic.Line, code = diagnostic.Code, message = diagnostic.Message }).ToList(),
            });
            return errors.Count == 0 ? 0 : 1;
        }

        foreach (var diagnostic in errors)
        {
            Console.Error.WriteLine($"{diagnostic.Relative}:{diagnostic.Line} [{diagnostic.Code}] {diagnostic.Message}");
        }

        if (warnings.Count > 0)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var diagnostic in warnings)
            {
                counts[diagnostic.Code] = counts.GetValueOrDefault(diagnostic.Code) + 1;
            }

            Console.Error.WriteLine();
            Console.Error.WriteLine($"LiteSpec authoring warnings ({warnings.Count}). These do not fail the build:");
            foreach (var code in counts.Keys.OrderBy(code => code, StringComparer.Ordinal))
            {
                Console.Error.WriteLine($"  {counts[code],4}  {code}");
            }

            Console.Error.WriteLine("  Run with --warnings to list every occurrence.");
            if (parsed.Has("--warnings"))
            {
                foreach (var diagnostic in warnings)
                {
                    Console.Error.WriteLine($"  {diagnostic.Relative}:{diagnostic.Line} [{diagnostic.Code}] {diagnostic.Message}");
                }
            }
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
