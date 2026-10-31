using System.Text.Json;

namespace LiteSpec.Model;

/// <summary>
///     What the model builder reads from the consumer's declaration.
/// </summary>
/// <follows>litespec/core/model#declare-size-stacks-and-optional-types</follows>
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

    /// <summary>Gets days before a tier-3 draft question auto-approves.</summary>
    public int VetoWindowDays { get; private set; } = 7;

    /// <summary>Gets the share of a tier-3 batch that batch approval samples, from the lowest semantic hashes.</summary>
    public double SampleShare { get; private set; } = 0.1;

    /// <summary>Gets the owner reviewing access, outside calls and personal-data flows.</summary>
    public string? SecurityReviewer { get; private set; }

    /// <summary>Gets the decision whose approval ends transcription mode.</summary>
    public string? TranscriptionEndsWith { get; private set; }

    /// <summary>Gets the refusal code the derived forbidden result carries.</summary>
    public string ForbiddenCode { get; private set; } = "authorization.forbidden";

    /// <summary>Gets the refusal code the derived invalid-input result carries.</summary>
    public string InvalidInputCode { get; private set; } = "request.validation-failed";

    /// <summary>Gets the HTTP status each refusal code answers with, by refusal code.</summary>
    public IReadOnlyDictionary<string, int> RefusalStatuses { get; private set; } =
        new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Gets the refusal-code prefixes the project shares across modules, such as a shared kernel.</summary>
    public IReadOnlyList<string> RefusalPrefixes { get; private set; } = [];

    /// <summary>
    ///     Gets the refusal codes the project composes at run time from a key, each a template with one <c>{key}</c>
    ///     placeholder, such as the conflict a commit answers for a lost uniqueness key.
    /// </summary>
    public IReadOnlyList<string> RefusalComposed { get; private set; } = [];

    /// <summary>Gets the client refusal vocabulary translated to declared codes, by client code.</summary>
    public IReadOnlyDictionary<string, string> RefusalAliases { get; private set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets the domain verbs the project adds to the baseline vocabulary.</summary>
    public IReadOnlyList<string> Verbs { get; private set; } = [];

    /// <summary>Gets multi-word verbs counted as one use-case opening.</summary>
    public IReadOnlyList<string> PhrasalVerbs { get; private set; } = [];

    /// <summary>Gets the project verbs that name a query, beside the baseline reading verbs.</summary>
    public IReadOnlyList<string> QueryVerbs { get; private set; } = [];

    /// <summary>Gets retired synonyms mapped to their replacement verb, by synonym, beside the baseline ones.</summary>
    public IReadOnlyDictionary<string, string> RetiredVerbs { get; private set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets lookalike pairs the term-clash check skips, each as "a|b".</summary>
    public IReadOnlyList<string> LookalikeIgnore { get; private set; } = [];

    /// <summary>Gets the past tense of project domain verbs, by verb, beside the baseline table.</summary>
    public IReadOnlyDictionary<string, string> PastTense { get; private set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets a value indicating whether whether the declaration names a use-case verb vocabulary.</summary>
    public bool HasVocabulary { get; private set; }

    /// <summary>Gets the backend layers.</summary>
    public string ApplicationRoot { get; private set; } = "";

    /// <summary>Gets the domain layer.</summary>
    public string DomainRoot { get; private set; } = "";

    /// <summary>Gets the HTTP layer.</summary>
    public string WebApiRoot { get; private set; } = "";

    /// <summary>Gets the adapters.</summary>
    public string InfrastructureRoot { get; private set; } = "";

    /// <summary>Gets the background worker.</summary>
    public string WorkerRoot { get; private set; } = "";

    /// <summary>Gets the declared shared-kernel folder, or empty when the project declares none.</summary>
    public string SharedKernel { get; private set; } = "";

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

    /// <summary>Gets the project size: small or large.</summary>
    public string Size { get; private set; } = "large";

    /// <summary>Gets the project shape: server or client-only.</summary>
    public string Shape { get; private set; } = "server";

    /// <summary>Gets the stacks the project names in <c>litespec.project.json</c>.</summary>
    public IReadOnlyList<string> Stacks { get; private set; } = [];

    /// <summary>Gets the extension ids the project selects in <c>selectedExtensions</c>.</summary>
    public IReadOnlyList<string> Extensions { get; private set; } = [];

    /// <summary>Gets the mark style the project declares.</summary>
    public MarkStyle Marks { get; private set; } = MarkStyle.Xml;

    /// <summary>Gets a value indicating whether whether the CI environment variable is set, so reader-missing blocks.</summary>
    public bool IsContinuousIntegration { get; private set; }

    /// <summary>Gets the closed owner list.</summary>
    public IReadOnlyList<string> Owners { get; private set; } = [];

    /// <summary>Gets the CODEOWNERS handles each owner name maps to, by owner name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> OwnerCodeowners { get; private set; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    /// <summary>Gets a value indicating whether whether the declaration names an owner list.</summary>
    public bool HasOwners { get; private set; }

    /// <summary>Gets the optional types the project records.</summary>
    public IReadOnlyList<string> OptionalTypes { get; private set; } = [];

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
        var file = Path.Combine(config.Root, "litespec.project.json");
        var text = File.Exists(file) ? File.ReadAllText(file) : "{}";

        using var document = JsonDocument.Parse(text);

        if (!ProjectSettings.TryParse(text, out var settings, out var error))
        {
            throw new InvalidOperationException(error!);
        }

        config.Marks = settings.Marks;

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

        string DeclaredRoot(string field, string fallback)
        {
            if (dotnet is not null
                && dotnet.Value.TryGetProperty(field, out var value)
                && value.ValueKind == JsonValueKind.String
                && value.GetString() is { Length: > 0 } pattern)
            {
                var resolved = Resolve(config.Root, pattern);

                if (!Directory.Exists(resolved))
                {
                    problems.Add(new Finding(
                        "reader-missing",
                        $"Stack root '{field}' declared as '{pattern}' resolves to '{resolved}', which does not exist; source under that root was not read.",
                        resolved,
                        1,
                        null)
                    {
                        Reader = Finding.RoslynReader,
                        ReaderIsQualified = true,
                    });
                }

                return resolved;
            }

            return Resolve(config.Root, fallback);
        }

        // Records live under the documentation roots that paths.docs names.
        config.RecordRoots = ReadRoots(config.Root, paths, "docs", ["docs"]);
        config.GlossaryPath = Combine(config.Root, ReadString(paths, "glossary", "docs/glossary.json"));
        config.BehaviorDocs = Combine(config.Root, ReadString(paths, "behaviorDocs", "docs/behavior"));
        config.ApplicationRoot = DeclaredRoot("application", "apps/api/src/*.Application");
        config.DomainRoot = DeclaredRoot("domain", "apps/api/src/*.Domain");
        config.WebApiRoot = DeclaredRoot("webapi", "apps/api/src/*.WebApi");
        config.InfrastructureRoot = DeclaredRoot("infrastructure", "apps/api/src/*.Infrastructure");
        config.WorkerRoot = DeclaredRoot("worker", "apps/api/src/*.Worker");

        if (dotnet is not null
            && dotnet.Value.TryGetProperty("sharedKernel", out var kernel)
            && kernel.ValueKind == JsonValueKind.String
            && kernel.GetString() is { Length: > 0 } kernelPath)
        {
            config.SharedKernel = DeclaredRoot("sharedKernel", kernelPath);
        }

        if (dotnet is not null
            && dotnet.Value.TryGetProperty("templateStore", out var store)
            && store.ValueKind == JsonValueKind.String
            && store.GetString() is { Length: > 0 } storePath)
        {
            config.TemplateStore = Combine(config.Root, storePath);
        }

        config.Alerts = ReadRoots(config.Root, paths, "alerts", []);
        config.Operations = ReadRoots(config.Root, paths, "operations", []);
        config.CheckResultsDir = Combine(config.Root, ReadString(paths, "checkResults", "docs/proof/checks"));
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

        if (document.RootElement.TryGetProperty("owners", out var owners)
            && owners.ValueKind == JsonValueKind.Array)
        {
            config.HasOwners = true;

            // An entry is a plain name or an object mapping a readable name to its
            // CODEOWNERS handles. The first entry wins when two entries share a name.
            var names = new List<string>();
            var mapped = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

            foreach (var entry in owners.EnumerateArray())
            {
                var name = entry.ValueKind switch
                {
                    JsonValueKind.String => entry.GetString(),
                    JsonValueKind.Object when entry.TryGetProperty("name", out var named)
                        && named.ValueKind == JsonValueKind.String => named.GetString(),
                    _ => null,
                };

                if (name is not { Length: > 0 } || !mapped.TryAdd(name, ReadHandles(entry)))
                {
                    continue;
                }

                names.Add(name);
            }

            config.Owners = names;
            config.OwnerCodeowners = mapped;
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
        }

        if (document.RootElement.TryGetProperty("optionalTypes", out var optional)
            && optional.ValueKind == JsonValueKind.Array)
        {
            config.OptionalTypes =
            [
                .. optional.EnumerateArray()
                             .Where(entry => entry.ValueKind == JsonValueKind.String)
                             .Select(entry => entry.GetString()!),
            ];
        }

        if (document.RootElement.TryGetProperty("proof", out var proof)
            && proof.ValueKind == JsonValueKind.Object)
        {
            config.JobTriggerSteps = ReadNames(proof, "jobTriggerSteps");
            config.OutcomeOnlySteps = ReadNames(proof, "outcomeOnlySteps");
        }

        if (document.RootElement.TryGetProperty("review", out var review)
            && review.ValueKind == JsonValueKind.Object)
        {
            if (review.TryGetProperty("vetoWindowDays", out var window)
                && window.ValueKind == JsonValueKind.Number
                && window.TryGetInt32(out var days)
                && days > 0)
            {
                config.VetoWindowDays = days;
            }

            if (review.TryGetProperty("sampleShare", out var share)
                && share.ValueKind == JsonValueKind.Number
                && share.TryGetDouble(out var sampled)
                && sampled is > 0 and <= 1)
            {
                config.SampleShare = sampled;
            }

            if (review.TryGetProperty("securityReviewer", out var reviewer)
                && reviewer.ValueKind == JsonValueKind.String
                && reviewer.GetString() is { Length: > 0 } name)
            {
                config.SecurityReviewer = name;
            }
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
            config.RefusalPrefixes = ReadNames(refusals, "prefixes");
            config.RefusalComposed = [.. ReadNames(refusals, "composed").Where(RefusalTemplate.IsWellFormed)];
        }

        if (document.RootElement.TryGetProperty("vocabulary", out var vocabulary)
            && vocabulary.ValueKind == JsonValueKind.Object)
        {
            config.HasVocabulary = true;
            config.Verbs = ReadNames(vocabulary, "verbs");
            config.PhrasalVerbs = ReadNames(vocabulary, "phrasal");
            config.QueryVerbs = ReadNames(vocabulary, "queryVerbs");
            config.RetiredVerbs = ReadRetired(vocabulary);
            config.LookalikeIgnore = ReadNames(vocabulary, "lookalikeIgnore");
            config.PastTense = ReadPastTense(vocabulary);
        }

        if (document.RootElement.TryGetProperty("adoption", out var adoption)
            && adoption.ValueKind == JsonValueKind.Object
            && adoption.TryGetProperty("transcriptionEndsWith", out var ends)
            && ends.ValueKind == JsonValueKind.String
            && ends.GetString() is { Length: > 0 } decision)
        {
            config.TranscriptionEndsWith = decision;
        }

        config.IsContinuousIntegration = Environment.GetEnvironmentVariable("CI") is { Length: > 0 };
        config.Problems = problems;

        return config;
    }

    /// <summary>
    ///     A path written with forward slashes, relative to the root.
    /// </summary>
    /// <param name="absolute">The absolute path to make relative.</param>
    /// <returns>The path relative to the root, with forward slashes.</returns>
    public string Relative(string absolute) =>
        Path.GetRelativePath(Root, absolute).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>
    ///     The owner itself plus the CODEOWNERS handles its project owners entry lists.
    /// </summary>
    /// <param name="owner">The owner name to expand.</param>
    /// <returns>The owner followed by its CODEOWNERS handles, or the owner alone when it maps to none.</returns>
    public IReadOnlyList<string> CandidatesFor(string owner) =>
        OwnerCodeowners.TryGetValue(owner, out var handles)
            ? [owner, .. handles]
            : [owner];

    /// <summary>Reads a string array field, keeping only its string entries.</summary>
    /// <param name="owner">The JSON object holding the field.</param>
    /// <param name="field">The field name to read.</param>
    /// <returns>The string entries of the field, or an empty list when the field is missing or not an array.</returns>
    private static IReadOnlyList<string> ReadNames(JsonElement owner, string field) =>
        owner.TryGetProperty(field, out var names) && names.ValueKind == JsonValueKind.Array
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

    /// <summary>The past-tense map: each project domain verb with its past tense.</summary>
    /// <param name="vocabulary">The vocabulary object holding the pastTense table.</param>
    /// <returns>The past tense for each project domain verb.</returns>
    private static Dictionary<string, string> ReadPastTense(JsonElement vocabulary)
    {
        var past = new Dictionary<string, string>(StringComparer.Ordinal);

        if (vocabulary.TryGetProperty("pastTense", out var table)
            && table.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in table.EnumerateObject())
            {
                if (entry.Name.Length > 0
                    && entry.Value.ValueKind == JsonValueKind.String
                    && entry.Value.GetString() is { Length: > 0 } form
                    && !past.ContainsKey(entry.Name))
                {
                    past.Add(entry.Name, form);
                }
            }
        }

        return past;
    }

    /// <summary>The retired verb map: each retired synonym with its replacement verb.</summary>
    /// <param name="vocabulary">The vocabulary object holding the retired table.</param>
    /// <returns>The replacement verb for each retired synonym.</returns>
    private static Dictionary<string, string> ReadRetired(JsonElement vocabulary)
    {
        var retired = new Dictionary<string, string>(StringComparer.Ordinal);

        if (vocabulary.TryGetProperty("retired", out var table)
            && table.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in table.EnumerateObject())
            {
                if (entry.Name.Length > 0
                    && entry.Value.ValueKind == JsonValueKind.String
                    && entry.Value.GetString() is { Length: > 0 } replacement
                    && !retired.ContainsKey(entry.Name))
                {
                    retired.Add(entry.Name, replacement);
                }
            }
        }

        return retired;
    }

    /// <summary>Reads the CODEOWNERS handles one owner entry lists.</summary>
    /// <param name="entry">The owner entry, a name string or an object with a codeowners array.</param>
    /// <returns>The distinct non-empty handles the entry lists, or an empty list for a plain name entry.</returns>
    private static IReadOnlyList<string> ReadHandles(JsonElement entry) =>
        entry.ValueKind == JsonValueKind.Object
        && entry.TryGetProperty("codeowners", out var handles)
        && handles.ValueKind == JsonValueKind.Array
            ? [.. handles.EnumerateArray()
                .Where(handle => handle.ValueKind == JsonValueKind.String)
                .Select(handle => handle.GetString()!)
                .Where(handle => handle.Length > 0)
                .Distinct(StringComparer.Ordinal)]
            : [];

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
}
