using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     What the model builder reads from the consumer's declaration.
/// </summary>
/// <follows>litespec/core/model#declare-size-and-stacks</follows>
sealed class ConsumerConfig
{
    /// <summary>Initializes a new instance of the <see cref="ConsumerConfig" /> class for one consumer root.</summary>
    /// <param name="root">The absolute consumer root the declaration is read from.</param>
    private ConsumerConfig(string root) => Root = root;

    /// <summary>Gets the consumer root.</summary>
    public string Root { get; }

    /// <summary>Gets configuration problems found while loading, such as a declared stack root with no folder.</summary>
    public IReadOnlyList<Finding> Problems { get; private set; } = [];

    /// <summary>Gets where written records live.</summary>
    public IReadOnlyList<string> RecordRoots { get; private set; } = [];

    /// <summary>Gets the glossary.</summary>
    public string GlossaryPath { get; private set; } = "";

    /// <summary>Gets where behavior records live.</summary>
    public string BehaviorDocs { get; private set; } = "";

    /// <summary>Gets the absolute screenshot gallery folders, from <c>screenshots.galleries</c>.</summary>
    public IReadOnlyList<string> ScreenshotGalleries { get; private set; } = [];

    /// <summary>Gets the screenshot file-name pattern, from <c>screenshots.pattern</c>.</summary>
    public string ScreenshotPattern { get; private set; } = "{route}--{theme}--{width}.png";

    /// <summary>Gets the refusal code the derived forbidden result carries.</summary>
    public string ForbiddenCode { get; private set; } = "authorization.forbidden";

    /// <summary>Gets the refusal code the derived invalid-input result carries.</summary>
    public string InvalidInputCode { get; private set; } = "request.validation-failed";

    /// <summary>Gets the HTTP status each refusal code answers with, by refusal code.</summary>
    public IReadOnlyDictionary<string, int> RefusalStatuses { get; private set; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    ///     Gets the refusal codes the project composes at run time from a key, each a template with one <c>{key}</c>
    ///     placeholder, such as the conflict a commit answers for a lost uniqueness key.
    /// </summary>
    public IReadOnlyList<string> RefusalComposed { get; private set; } = [];

    /// <summary>Gets the client refusal codes translated to declared codes, by client code.</summary>
    public IReadOnlyDictionary<string, string> RefusalAliases { get; private set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets the modules project holding every module, or the Application project under layer-projects.</summary>
    public string ModulesRoot { get; private set; } = "";

    /// <summary>Gets the Domain project, under layer-projects only; empty in the module layout unless the declaration names it.</summary>
    public string DomainRoot { get; private set; } = "";

    /// <summary>Gets the WebApi host projects, in declaration order.</summary>
    public IReadOnlyList<string> WebApiRoots { get; private set; } = [];

    /// <summary>Gets the Infrastructure project, under layer-projects only; empty in the module layout unless the declaration names it.</summary>
    public string InfrastructureRoot { get; private set; } = "";

    /// <summary>Gets the background worker host projects, in declaration order.</summary>
    public IReadOnlyList<string> WorkerRoots { get; private set; } = [];

    /// <summary>Gets a value indicating whether the project selects the layer-projects extension, so the five project roots apply.</summary>
    public bool IsLayerProjects { get; private set; }

    /// <summary>Gets the shared kernel project holding what every module builds on.</summary>
    public string SharedKernelRoot { get; private set; } = "";

    /// <summary>Gets where tests live.</summary>
    public IReadOnlyList<string> TestRoots { get; private set; } = [];

    /// <summary>Gets the alert rule files or folders, from <c>paths.alerts</c>.</summary>
    public IReadOnlyList<string> Alerts { get; private set; } = [];

    /// <summary>Gets the deployment, proxy, host and CI workflow files or folders, from <c>paths.operations</c>.</summary>
    public IReadOnlyList<string> Operations { get; private set; } = [];

    /// <summary>Gets where drill and harness check results live, from <c>paths.checkResults</c>.</summary>
    public string CheckResultsDir { get; private set; } = "";

    /// <summary>Gets the bill of materials files or folders, from <c>paths.sbom</c>.</summary>
    public IReadOnlyList<string> Sboms { get; private set; } = [];

    /// <summary>Gets the template store file, from <c>stackConfig.dotnet.templateStore</c>, or empty.</summary>
    public string TemplateStore { get; private set; } = "";

    /// <summary>Gets the licenses the project accepts, as SPDX identifiers, from <c>dependencies.licenses</c>.</summary>
    public IReadOnlySet<string> AcceptedLicenses { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Gets the declared frontends.</summary>
    public IReadOnlyList<FrontendConfig> Frontends { get; private set; } = [];

    /// <summary>Gets the generated client's sources.</summary>
    public string ApiClientSrc { get; private set; } = "";

    /// <summary>Gets where test result files live.</summary>
    public string TestResultsDir { get; private set; } = "";

    /// <summary>Gets the committed OpenAPI document, or null when the project names none.</summary>
    public string? ContractPath { get; private set; }

    /// <summary>Gets the conformance level the project claims; full when the project file names none, which the schema check reports.</summary>
    public ConformanceLevel Conformance { get; private set; } = ConformanceLevel.Full;

    /// <summary>Gets the project size: small or large.</summary>
    public string Size { get; private set; } = "large";

    /// <summary>Gets the project shape: server or client-only.</summary>
    public string Shape { get; private set; } = "server";

    /// <summary>Gets the stacks the project names in <c>.litespec/project.json</c>.</summary>
    public IReadOnlyList<string> Stacks { get; private set; } = [];

    /// <summary>Gets the extension ids the project selects in <c>selectedExtensions</c>.</summary>
    public IReadOnlyList<string> Extensions { get; private set; } = [];

    /// <summary>Gets the mark style the project declares.</summary>
    public MarkStyle Marks { get; private set; } = MarkStyle.Xml;

    /// <summary>Gets a value indicating whether whether the CI environment variable is set, so reader-missing blocks.</summary>
    public bool IsContinuousIntegration { get; private set; }

    /// <summary>Gets the steps classes whose when steps run a job, such as one that moves a simulated clock.</summary>
    public IReadOnlyList<string> JobTriggerSteps { get; private set; } = [];

    /// <summary>Gets the steps classes whose then steps state only that a request succeeded or was refused.</summary>
    public IReadOnlyList<string> OutcomeOnlySteps { get; private set; } = [];

    /// <summary>
    ///     Reads the declaration, falling back to the documented defaults.
    /// </summary>
    /// <param name="root">The consumer root holding the declaration file.</param>
    /// <returns>The loaded configuration, with defaults for anything the declaration omits.</returns>
    public static ConsumerConfig Load(string root)
    {
        var config = new ConsumerConfig(Path.GetFullPath(root));
        var file = ConsumerRoot.ProjectFile(config.Root);
        var text = File.Exists(file) ? File.ReadAllText(file) : "{}";

        using var document = JsonDocument.Parse(text);

        if (!ProjectPaths.TryRead(file, text, out var projectPaths, out var error) || projectPaths is null)
        {
            throw new InvalidOperationException(error!);
        }

        config.Marks = projectPaths.Settings.Marks;

        var paths = document.RootElement.TryGetProperty("paths", out var found)
            && found.ValueKind == JsonValueKind.Object
                ? found
                : (JsonElement?)null;

        var dotnet = document.RootElement.TryGetProperty("stackConfig", out var stacksConfig)
            && stacksConfig.ValueKind == JsonValueKind.Object
            && stacksConfig.TryGetProperty("dotnet", out var dotnetConfig)
            && dotnetConfig.ValueKind == JsonValueKind.Object
                ? dotnetConfig
                : (JsonElement?)null;

        var problems = new List<Finding>();
        var layerProjects = SelectsExtension(document.RootElement, "layer-projects");
        var selectsDotnet = SelectsDotnet(document.RootElement);

        void ReportMissing(string field, string pattern, string resolved, bool declared)
        {
            problems.Add(new Finding(
                "reader-missing",
                declared
                    ? $"Stack root '{field}' declared as '{pattern}' resolves to '{resolved}', which does not exist; source under that root was not read."
                    : $"Stack root '{field}' defaulted to '{pattern}' resolves to '{resolved}', which does not exist; source under that root was not read.",
                resolved,
                1,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });
        }

        string DeclaredRootOrEmpty(string field)
        {
            if (dotnet is not null
                && dotnet.Value.TryGetProperty(field, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } pattern)
            {
                return DeclaredRoot(field, pattern, reportDefault: false);
            }

            return "";
        }

        string DeclaredRoot(string field, string fallback, bool reportDefault)
        {
            if (dotnet is not null
                && dotnet.Value.TryGetProperty(field, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } pattern)
            {
                var resolved = Resolve(config.Root, pattern);

                if (!Directory.Exists(resolved))
                {
                    ReportMissing(field, pattern, resolved, declared: true);
                }

                return resolved;
            }

            var fallbackResolved = Resolve(config.Root, fallback);

            if (reportDefault && selectsDotnet && !Directory.Exists(fallbackResolved))
            {
                ReportMissing(field, fallback, fallbackResolved, declared: false);
            }

            return fallbackResolved;
        }

        IReadOnlyList<string> DeclaredRoots(string field, string fallback)
        {
            if (dotnet is not null && dotnet.Value.TryGetProperty(field, out var value))
            {
                if (value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } single)
                {
                    var roots = new List<string>();

                    foreach (var resolved in Expand(config.Root, single))
                    {
                        if (!Directory.Exists(resolved))
                        {
                            ReportMissing(field, single, resolved, declared: true);
                        }

                        roots.Add(resolved);
                    }

                    return roots;
                }

                if (value.ValueKind == JsonValueKind.Array)
                {
                    var roots = new List<string>();

                    foreach (var entry in value.EnumerateArray())
                    {
                        if (entry.ValueKind != JsonValueKind.String
                            || entry.GetString() is not { Length: > 0 } pattern)
                        {
                            continue;
                        }

                        foreach (var resolved in Expand(config.Root, pattern))
                        {
                            if (!Directory.Exists(resolved))
                            {
                                ReportMissing(field, pattern, resolved, declared: true);
                            }

                            if (!roots.Contains(resolved, StringComparer.OrdinalIgnoreCase))
                            {
                                roots.Add(resolved);
                            }
                        }
                    }

                    return roots;
                }
            }

            return [Resolve(config.Root, fallback)];
        }

        // Records live under the record roots that paths.docs names, which default
        // to the one LiteSpec folder at the repository root. The paths come from
        // the shared reader the analyzers use too.
        config.RecordRoots = [.. projectPaths.DocsRoots.Select(Native)];
        config.GlossaryPath = Native(projectPaths.GlossaryPath);
        config.BehaviorDocs = Native(projectPaths.BehaviorDocs);
        config.ModulesRoot = DeclaredRoot("modules", layerProjects ? "apps/api/src/*.Application" : "apps/api/src/*.Modules", reportDefault: true);
        config.DomainRoot = layerProjects ? DeclaredRoot("domain", "apps/api/src/*.Domain", reportDefault: true) : DeclaredRootOrEmpty("domain");
        config.WebApiRoots = DeclaredRoots("webapi", "apps/api/src/*.WebApi");
        config.InfrastructureRoot = layerProjects ? DeclaredRoot("infrastructure", "apps/api/src/*.Infrastructure", reportDefault: true) : DeclaredRootOrEmpty("infrastructure");
        config.WorkerRoots = DeclaredRoots("worker", "apps/api/src/*.Worker");
        config.SharedKernelRoot = DeclaredRoot("sharedKernel", "apps/api/src/*.SharedKernel", reportDefault: false);

        if (dotnet is not null
            && dotnet.Value.TryGetProperty("templateStore", out var store)
            && store.ValueKind == JsonValueKind.String
            && store.GetString() is { Length: > 0 } storePath)
        {
            config.TemplateStore = Combine(config.Root, storePath);
        }

        config.Alerts = ReadRoots(config.Root, paths, "alerts", []);
        config.Operations = ReadRoots(config.Root, paths, "operations", []);
        config.CheckResultsDir = Combine(config.Root, ReadString(paths, "checkResults", ".litespec/proof/checks"));
        config.Sboms = ReadRoots(config.Root, paths, "sbom", []);

        if (document.RootElement.TryGetProperty("dependencies", out var dependencies)
            && dependencies.ValueKind == JsonValueKind.Object
            && dependencies.TryGetProperty("licenses", out var licenses)
            && licenses.ValueKind == JsonValueKind.Array)
        {
            config.AcceptedLicenses = licenses.EnumerateArray()
                .Where(entry => entry.ValueKind == JsonValueKind.String)
                .Select(entry => entry.GetString()!)
                .ToHashSet(StringComparer.Ordinal);
        }
        config.TestRoots = ReadRoots(config.Root, paths, "testRoots", []);
        config.Frontends = ReadFrontends(config.Root, paths);
        config.ApiClientSrc = Path.Combine(
            Combine(config.Root, ReadString(paths, "apiClient", "packages/api")), "src");
        config.TestResultsDir = Combine(config.Root, ReadString(paths, "testResults", "TestResults"));
        config.ContractPath = ReadString(paths, "contract", "") is { Length: > 0 } contract ? Combine(config.Root, contract) : null;

        if (document.RootElement.TryGetProperty("conformance", out var conformance)
            && conformance.ValueKind == JsonValueKind.String
            && ConformanceLevels.TryParse(conformance.GetString(), out var level))
        {
            config.Conformance = level;
        }

        if (document.RootElement.TryGetProperty("size", out var size)
            && size.ValueKind == JsonValueKind.String
            && size.GetString() is "small" or "large")
        {
            config.Size = size.GetString()!;
        }

        if (document.RootElement.TryGetProperty("shape", out var shape)
            && shape.ValueKind == JsonValueKind.String
            && shape.GetString() is { Length: > 0 } shapeName)
        {
            config.Shape = shapeName;
        }

        if (document.RootElement.TryGetProperty("stacks", out var stacks)
            && stacks.ValueKind == JsonValueKind.Array)
        {
            config.Stacks =
            [
                .. stacks.EnumerateArray()
                             .Where(entry => entry.ValueKind == JsonValueKind.String)
                             .Select(entry => entry.GetString()!),
            ];
        }

        if (document.RootElement.TryGetProperty("selectedExtensions", out var extensions)
            && extensions.ValueKind == JsonValueKind.Array)
        {
            var ids = new List<string>();

            foreach (var entry in extensions.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String && entry.GetString() is { Length: > 0 } id)
                {
                    ids.Add(id);
                }
                else if (entry.ValueKind == JsonValueKind.Object
                    && entry.TryGetProperty("id", out var named)
                    && named.ValueKind == JsonValueKind.String
                    && named.GetString() is { Length: > 0 } objectId)
                {
                    ids.Add(objectId);
                }
            }

            config.Extensions = ids;
            config.IsLayerProjects = ids.Contains("layer-projects", StringComparer.Ordinal);
        }

        if (document.RootElement.TryGetProperty("proof", out var proof)
            && proof.ValueKind == JsonValueKind.Object)
        {
            config.JobTriggerSteps = ReadNames(proof, "jobTriggerSteps");
            config.OutcomeOnlySteps = ReadNames(proof, "outcomeOnlySteps");
        }

        if (document.RootElement.TryGetProperty("refusals", out var refusals)
            && refusals.ValueKind == JsonValueKind.Object)
        {
            if (refusals.TryGetProperty("forbidden", out var forbidden)
                && forbidden.ValueKind == JsonValueKind.String
                && forbidden.GetString() is { Length: > 0 } forbiddenCode)
            {
                config.ForbiddenCode = forbiddenCode;
            }

            if (refusals.TryGetProperty("invalidInput", out var invalid)
                && invalid.ValueKind == JsonValueKind.String
                && invalid.GetString() is { Length: > 0 } invalidCode)
            {
                config.InvalidInputCode = invalidCode;
            }

            config.RefusalStatuses = ReadStatuses(refusals);
            config.RefusalAliases = ReadAliases(refusals);
            config.RefusalComposed = [.. ReadNames(refusals, "composed").Where(RefusalTemplate.IsWellFormed)];
        }

        if (document.RootElement.TryGetProperty("screenshots", out var screenshots)
            && screenshots.ValueKind == JsonValueKind.Object)
        {
            if (screenshots.TryGetProperty("galleries", out var galleries)
                && galleries.ValueKind == JsonValueKind.Array)
            {
                config.ScreenshotGalleries =
                [
                    .. galleries.EnumerateArray()
                                  .Where(entry => entry.ValueKind == JsonValueKind.String)
                                  .Select(entry => entry.GetString()!)
                                  .Where(gallery => gallery.Length > 0)
                                  .Select(gallery => Combine(config.Root, gallery)),
                ];
            }
            else if (screenshots.TryGetProperty("gallery", out var gallery)
                && gallery.ValueKind == JsonValueKind.String
                && gallery.GetString() is { Length: > 0 } single)
            {
                config.ScreenshotGalleries = [Combine(config.Root, single)];
            }

            if (screenshots.TryGetProperty("pattern", out var pattern)
                && pattern.ValueKind == JsonValueKind.String
                && pattern.GetString() is { Length: > 0 } named)
            {
                config.ScreenshotPattern = named;
            }
        }

        config.IsContinuousIntegration = Environment.GetEnvironmentVariable("CI") is { Length: > 0 };
        config.Problems = problems;

        return config;
    }

    /// <summary>Whether the declaration selects the extension id in selectedExtensions.</summary>
    /// <param name="root">The declaration root object.</param>
    /// <param name="id">The extension id to test for.</param>
    /// <returns>True when selectedExtensions names the id as a string or an object with that id.</returns>
    private static bool SelectsExtension(JsonElement root, string id)
    {
        if (!root.TryGetProperty("selectedExtensions", out var extensions)
            || extensions.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in extensions.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String
                && string.Equals(entry.GetString(), id, StringComparison.Ordinal))
            {
                return true;
            }

            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty("id", out var named)
                && named.ValueKind == JsonValueKind.String
                && string.Equals(named.GetString(), id, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the declaration selects a stack whose sources the .NET reader reads.</summary>
    /// <param name="root">The declaration root object.</param>
    /// <returns>True when the stacks array names dotnet, blazor or android.</returns>
    private static bool SelectsDotnet(JsonElement root)
    {
        if (!root.TryGetProperty("stacks", out var stacks)
            || stacks.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in stacks.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.String
                && entry.GetString() is "dotnet" or "blazor" or "android")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads a string array field, keeping only its string entries.</summary>
    /// <param name="element">The JSON object holding the field.</param>
    /// <param name="field">The field name to read.</param>
    /// <returns>The string entries of the field, or an empty list when the field is missing or not an array.</returns>
    private static IReadOnlyList<string> ReadNames(JsonElement element, string field) =>
        element.TryGetProperty(field, out var names) && names.ValueKind == JsonValueKind.Array
            ? [.. names.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.String).Select(entry => entry.GetString()!)]
            : [];

    /// <summary>The refusal statuses table: each declared code with the HTTP status it answers with.</summary>
    /// <param name="refusals">The refusals object holding the statuses table.</param>
    /// <returns>The HTTP status for each declared refusal code.</returns>
    private static Dictionary<string, int> ReadStatuses(JsonElement refusals)
    {
        var statuses = new Dictionary<string, int>(StringComparer.Ordinal);

        if (refusals.TryGetProperty("statuses", out var table)
            && table.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in table.EnumerateObject())
            {
                if (entry.Name.Length > 0
                    && entry.Value.ValueKind == JsonValueKind.Number
                    && entry.Value.TryGetInt32(out var status)
                    && status is >= 100 and <= 599
                    && !statuses.ContainsKey(entry.Name))
                {
                    statuses.Add(entry.Name, status);
                }
            }
        }

        return statuses;
    }

    /// <summary>The refusal code map: each client code with the declared code it translates to.</summary>
    /// <param name="refusals">The refusals object holding the aliases table.</param>
    /// <returns>The declared code for each client code.</returns>
    private static Dictionary<string, string> ReadAliases(JsonElement refusals)
    {
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        if (refusals.TryGetProperty("aliases", out var table)
            && table.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in table.EnumerateObject())
            {
                if (entry.Name.Length > 0
                    && entry.Value.ValueKind == JsonValueKind.String
                    && entry.Value.GetString() is { Length: > 0 } code
                    && !aliases.ContainsKey(entry.Name))
                {
                    aliases.Add(entry.Name, code);
                }
            }
        }

        return aliases;
    }

    /// <summary>Reads one string field of the paths object, with a fallback.</summary>
    /// <param name="paths">The paths object, or null when the declaration names none.</param>
    /// <param name="field">The field name to read.</param>
    /// <param name="fallback">The value used when the field is missing or empty.</param>
    /// <returns>The field string, or the fallback.</returns>
    private static string ReadString(JsonElement? paths, string field, string fallback) =>
        paths is not null
        && paths.Value.TryGetProperty(field, out var value)
        && value.ValueKind == JsonValueKind.String
        && value.GetString() is { Length: > 0 } text
            ? text
            : fallback;

    /// <summary>Reads a roots field: one path or an array of paths, resolved against the root.</summary>
    /// <param name="root">The consumer root the paths resolve against.</param>
    /// <param name="paths">The paths object, or null when the declaration names none.</param>
    /// <param name="field">The field name to read.</param>
    /// <param name="fallback">The relative suffixes used when the field is missing.</param>
    /// <returns>The resolved absolute roots, or the resolved fallback suffixes.</returns>
    private static IReadOnlyList<string> ReadRoots(
        string root, JsonElement? paths, string field, string[] fallback)
    {
        if (paths is not null && paths.Value.TryGetProperty(field, out var value))
        {
            if (value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } single)
            {
                return [Combine(root, single)];
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return
                [
                    .. value.EnumerateArray()
                              .Where(entry => entry.ValueKind == JsonValueKind.String)
                              .Select(entry => Combine(root, entry.GetString()!)),
                ];
            }
        }

        return [.. fallback.Select(suffix => Combine(root, suffix))];
    }

    /// <summary>Reads the declared frontends from the paths object.</summary>
    /// <param name="root">The consumer root the frontend paths resolve against.</param>
    /// <param name="paths">The paths object, or null when the declaration names none.</param>
    /// <returns>The declared frontends in declaration order, or an empty list when none are declared.</returns>
    private static List<FrontendConfig> ReadFrontends(string root, JsonElement? paths)
    {
        if (paths is null
            || !paths.Value.TryGetProperty("frontends", out var frontends)
            || frontends.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var found = new List<FrontendConfig>();

        foreach (var frontend in frontends.EnumerateArray())
        {
            if (frontend.TryGetProperty("name", out var name)
                && frontend.TryGetProperty("path", out var path)
                && name.GetString() is { } named
                && path.GetString() is { } located)
            {
                var platform = frontend.TryGetProperty("platform", out var declared)
                    ? declared.GetString() ?? ""
                    : "";

                string? Optional(string field) =>
                    frontend.TryGetProperty(field, out var value)
                    && value.ValueKind == JsonValueKind.String
                    && value.GetString() is { Length: > 0 } text
                        ? Combine(root, text)
                        : null;

                found.Add(new FrontendConfig(
                    named, Combine(root, located), platform,
                    Optional("refusalDictionary"), Optional("routeSuite"), Optional("visualGoldens"),
                    frontend.TryGetProperty("entryPoint", out var entry) && entry.ValueKind == JsonValueKind.String
                        ? entry.GetString()
                        : null));
            }
        }

        return found;
    }

    /// <summary>Combines a root with a forward-slash relative path into an absolute path.</summary>
    /// <param name="root">The absolute root.</param>
    /// <param name="relative">The relative path, written with forward slashes.</param>
    /// <returns>The absolute combined path.</returns>
    private static string Combine(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>Converts a resolved path written with forward slashes to the operating system's form.</summary>
    /// <param name="path">The absolute path with forward slashes, as <see cref="ProjectPaths" /> resolves it.</param>
    /// <returns>The absolute path with the operating system's separators.</returns>
    private static string Native(string path) => Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    ///     Resolves one path, expanding a single segment wildcard to the first match.
    /// </summary>
    /// <param name="root">The absolute root the pattern resolves against.</param>
    /// <param name="pattern">The path pattern, with at most one wildcard per segment.</param>
    /// <returns>The resolved absolute path, with an unmatched wildcard segment emptied.</returns>
    private static string Resolve(string root, string pattern)
    {
        if (!pattern.Contains('*'))
        {
            return Combine(root, pattern);
        }

        var parts = pattern.Replace('/', Path.DirectorySeparatorChar)
                           .Split(Path.DirectorySeparatorChar);
        var current = root;

        foreach (var part in parts)
        {
            if (!part.Contains('*'))
            {
                current = Path.Combine(current, part);
                continue;
            }

            if (!Directory.Exists(current))
            {
                return Path.Combine(current, part.Replace("*", ""));
            }

            var match = Directory.EnumerateDirectories(current, part)
                                 .Order(StringComparer.Ordinal)
                                 .FirstOrDefault();

            if (match is null)
            {
                return Path.Combine(current, part.Replace("*", ""));
            }

            current = match;
        }

        return current;
    }

    /// <summary>
    ///     Resolves one host pattern to every matching folder, in ordinal order.
    /// </summary>
    /// <param name="root">The absolute root the pattern resolves against.</param>
    /// <param name="pattern">The path pattern, with at most one wildcard per segment.</param>
    /// <returns>Every matching absolute path, or one path with an unmatched wildcard segment emptied when no folder matches.</returns>
    private static List<string> Expand(string root, string pattern)
    {
        if (!pattern.Contains('*'))
        {
            return [Combine(root, pattern)];
        }

        var parts = pattern.Replace('/', Path.DirectorySeparatorChar)
                           .Split(Path.DirectorySeparatorChar);
        var current = new List<string> { root };

        foreach (var part in parts)
        {
            var next = new List<string>();

            foreach (var folder in current)
            {
                if (!part.Contains('*'))
                {
                    next.Add(Path.Combine(folder, part));
                    continue;
                }

                if (!Directory.Exists(folder))
                {
                    next.Add(Path.Combine(folder, part.Replace("*", "")));
                    continue;
                }

                var matches = Directory.EnumerateDirectories(folder, part)
                                       .Order(StringComparer.Ordinal)
                                       .ToList();

                if (matches.Count == 0)
                {
                    next.Add(Path.Combine(folder, part.Replace("*", "")));
                }
                else
                {
                    next.AddRange(matches);
                }
            }

            current = next;
        }

        return current;
    }
}
