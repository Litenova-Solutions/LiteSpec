using LiteSpec.Model.Readers.Dotnet;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The placement checks: every backend mark sits under its record's qualified path with its namespace,
///     every test mirrors the code it proves, every frontend module names a declared module, and every
///     module folder names a declared record under a closed folder set.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/model#match-each-backend-mark-to-its-folder</follows>
internal static class PlacementChecks
{
    /// <summary>Fixed folders at the modules root, whatever its modules declare.</summary>
    static readonly HashSet<string> ModulesRootFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Composition",
    };

    /// <summary>Fixed folders at the shared kernel root: one layer folder per layer.</summary>
    static readonly HashSet<string> SharedKernelFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi",
    };

    /// <summary>Fixed folders at a module, whatever its aggregates declare.</summary>
    static readonly HashSet<string> ModuleFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contracts", "Domain", "Application", "Infrastructure", "WebApi",
        "UseCases", "Jobs",
    };

    /// <summary>The kind folders a module Contracts folder holds: the sorted public surface.</summary>
    static readonly HashSet<string> ContractsKindFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identifiers", "Exceptions", "IntegrationEvents", "Commands", "Ports",
    };

    /// <summary>Fixed folders at an aggregate, whatever its use cases declare.</summary>
    static readonly HashSet<string> AggregateFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi", "UseCases",
    };

    /// <summary>Namespace segments that state no placement: layers, plumbing, and record mirrors.</summary>
    static readonly HashSet<string> PlacelessSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Abstractions", "Infrastructure", "WebApi", "Worker",
        "Persistence", "Endpoints", "Contracts", "Events", "Exceptions",
        "Identifiers", "IntegrationEvents", "Commands", "Ports",
        "UseCases", "Composition", "Jobs", "Tests", "Test", "Acceptance", "Integration", "Unit",
    };

    /// <summary>Directory names the folder walk leaves out: build output, installed packages and tool folders.</summary>
    static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "TestResults", "node_modules", ".git",
        ".svelte-kit", "build", "dist", "coverage",
    };

    /// <summary>Runs the placement checks.</summary>
    /// <param name="model">The loaded project model that holds every record and each marked code element.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckBackendPlacement(model, findings);
        CheckTestPlacement(model, findings);
        CheckFrontendPlacement(model, findings);
        CheckClosedFolders(model, findings);
    }

    /// <summary>
    ///     Reports every backend mark whose namespace or aggregate folder misses its record's qualified path:
    ///     a use case mark outside its aggregate folder, an event or rule mark outside its module or aggregate,
    ///     an external-system mark outside its module, and any of those with a namespace that names another path.
    ///     A mark naming no record, a record of another type, or a record its kind does not take is
    ///     <c>mark-unknown</c>, not drift. A host endpoint, a technical class, an aggregate method and a
    ///     frontend element carry no backend placement, so they are left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each marked code element.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBackendPlacement(ProjectModel model, List<Finding> findings)
    {
        var namespaces = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var element in model.Code.Elements)
        {
            if (element.InTests
                || element.Kind is CodeKind.Test or CodeKind.BrowserTest
                    or CodeKind.FrontendModule or CodeKind.FrontendRoute
                    or CodeKind.AggregateMethod or CodeKind.Technical)
            {
                continue;
            }

            if (element.Kind == CodeKind.Endpoint && IsHostEndpoint(model.Config, element.File))
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    continue;
                }

                if (!model.ById.TryGetValue(mark.RecordId, out var record)
                    || !MarkRules.Accepts(element.Kind, mark.RecordId))
                {
                    continue;
                }

                if (!Identifiers.TrySplit(mark.RecordId, out var prefix, out _, out _)
                    || record.Type != prefix)
                {
                    continue;
                }

                // An endpoint in a host that marks no use case is a host endpoint, which
                // carries its mark without a use case folder.
                if (element.Kind == CodeKind.Endpoint
                    && record.Type != RecordTypes.UseCase
                    && SourceRoots.IsUnderHost(model.Config, element.File))
                {
                    continue;
                }

                // A rule the product holds is enforced by a shared value, so its code lives in
                // the shared kernel, wherever the rule's record file sits.
                if (record.Type == RecordTypes.Rule
                    && Identifiers.TrySplit(record.Id, out _, out var ruleHome, out _)
                    && ruleHome is not null
                    && model.ById.ContainsKey($"product/{ruleHome}"))
                {
                    if (!SourceRoots.IsUnder(model.Config.SharedKernelRoot, element.File))
                    {
                        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' implements the product rule '{record.Id}', so it belongs in the shared kernel.", mark.File, mark.Line, mark.RecordId));
                    }

                    continue;
                }

                var path = PathOf(record, model.Config);

                if (path.Module is null)
                {
                    continue;
                }

                var ruleHomes = record.Type == RecordTypes.Rule
                    ? AcceptedRuleAggregates(model, record, path)
                    : null;

                if (!CheckBackendFolder(model, element, mark, record, path, ruleHomes, findings))
                {
                    CheckBackendNamespace(model.Config, element, mark, record, path, ruleHomes, namespaces, findings);
                }
            }
        }
    }

    /// <summary>
    ///     Reports the backend folder the mark misses: the module folder for an event, rule or external-system
    ///     mark, the use case and aggregate folders for a handler or endpoint mark, and the aggregate folder
    ///     for an event, rule or aggregate mark. A rule exception or domain event in its module Contracts
    ///     folder, and a rule exception beside an aggregate its rule governs or binds, sit where the module
    ///     tree keeps them. The module folder of an event handler, job host or aggregate root, and the
    ///     module and use case folders of a handler or endpoint naming another module or use case, stay with
    ///     <c>mark-folder-drift</c>, so a mark it reports is not reported here.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the aggregates a rule governs or binds.</param>
    /// <param name="element">The marked code element whose folder is checked.</param>
    /// <param name="mark">The mark naming the record the folder should hold.</param>
    /// <param name="record">The marked record.</param>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <param name="ruleHomes">The aggregates a rule mark accepts, or null when the mark names no rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckBackendFolder(ProjectModel model, MarkedElement element, CodeMark mark, Record record, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path, List<string>? ruleHomes, List<Finding> findings)
    {
        if (SourceRoots.ModulePath(model.Config, element.File) is not { } segments)
        {
            return false;
        }

        if (!SameName(segments[0], path.Module!))
        {
            if (record.Type is RecordTypes.Event or RecordTypes.Rule or RecordTypes.ExternalSystem)
            {
                findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                return true;
            }

            return false;
        }

        if (element.Kind is CodeKind.Handler or CodeKind.Endpoint
            && record.Type == RecordTypes.UseCase
            && path.Name is not null)
        {
            return CheckUseCaseElementFolder(model.Config, element, mark, path, findings);
        }

        if (path.Aggregate is null
            || record.Type is not (RecordTypes.Event or RecordTypes.Rule or RecordTypes.Aggregate))
        {
            return false;
        }

        // A rule exception or domain event another module names lives in its owner
        // Contracts folder, outside the aggregate folders the comparison below reads.
        if (record.Type is RecordTypes.Event or RecordTypes.Rule
            && IsInModuleContracts(model.Config, element.File, path.Module!))
        {
            return false;
        }

        if (segments.Length < 3 || SameName(segments[1], path.Aggregate))
        {
            return false;
        }

        // A module with one aggregate of its own name stays flat, so the aggregate
        // folder is missing only when the aggregate names its own folder.
        if (SameName(path.Aggregate, path.Module!) && segments.Length == 3)
        {
            return false;
        }

        // A rule lives in its rule-set folder, but its exception lives with the
        // aggregate that enforces it, which the rule names in governs or binds.
        if (record.Type == RecordTypes.Rule
            && ruleHomes is not null
            && ruleHomes.Any(home => SameName(segments[1], home)))
        {
            return false;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     Reports the folder a handler or endpoint misses for its use case: an aggregate
    ///     use case lives in its aggregate UseCases folder, and a module-level use case in
    ///     the module UseCases folder. A use case folder naming another use case, and a
    ///     module folder naming another module, stay with <c>mark-folder-drift</c>, so they
    ///     are left out here, and a use case folder flat in its aggregate folder stays with
    ///     the closed folder check, which names the missing UseCases level.
    /// </summary>
    /// <param name="config">The project configuration that holds each layer root.</param>
    /// <param name="element">The marked handler or endpoint whose folder is checked.</param>
    /// <param name="mark">The mark naming the use case the folder should hold.</param>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckUseCaseElementFolder(ConsumerConfig config, MarkedElement element, CodeMark mark, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path, List<Finding> findings)
    {
        // Level folders state no module segment in the stripped path, so the raw
        // path derives the element's own folders: UseCases stays visible, and the
        // folder above the use case folder keeps its written name.
        if (RawModulePath(config, element.File) is not { } raw || raw.Length < 2)
        {
            return false;
        }

        if (!SameName(raw[0], path.Module!))
        {
            return false;
        }

        var folders = raw[1..^1];

        if (folders.Length == 0)
        {
            return false;
        }

        var own = folders[^1];

        if (!SameName(own, path.Name!))
        {
            return false;
        }

        var above = folders.Length >= 2 ? folders[^2] : null;

        if (path.Aggregate is null)
        {
            // A use case no single aggregate owns lives in the module UseCases
            // folder; directly under the module it is the closed folder check
            // that names the folder.
            if (above is null)
            {
                return false;
            }

            if (above.Equals("UseCases", StringComparison.OrdinalIgnoreCase))
            {
                // Nested under an aggregate UseCases folder, the use case sits in
                // an aggregate that owns no use case of its own name.
                if (folders.Length >= 3)
                {
                    findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{folders[^3]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                    return true;
                }

                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{above}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (above is null)
        {
            // Under layer-projects a module with one aggregate of its own name
            // stays flat, so the use case folder sits directly under the module
            // only for that aggregate. In the module layout the UseCases level
            // is required instead.
            if (!SourceRoots.IsProductRoot(config.ModulesRoot) && SameName(path.Aggregate, path.Module!))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{own}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (above.Equals("UseCases", StringComparison.OrdinalIgnoreCase))
        {
            var home = folders.Length >= 3 ? folders[^3] : null;

            if (home is null)
            {
                // A module with one aggregate of its own name stays flat, so the
                // use case folder sits in the module UseCases folder only for
                // that aggregate.
                if (SameName(path.Aggregate, path.Module!))
                {
                    return false;
                }

                findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder 'UseCases', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
                return true;
            }

            if (SameName(home, path.Aggregate))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{home}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (SameName(above, path.Aggregate))
        {
            return false;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{above}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     The path segments of the file under its module layout without leaving any
    ///     folder out, with the module first and the file name last, or null when the
    ///     file sits outside every module. Under layer projects the module path already
    ///     keeps every folder, so it is returned as is.
    /// </summary>
    /// <param name="config">The project configuration that holds each layer root and the shared kernel root.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <returns>The raw path segments with the module first, or null when the file sits outside the module layout.</returns>
    private static string[]? RawModulePath(ConsumerConfig config, string file)
    {
        if (SourceRoots.IsProductRoot(config.ModulesRoot) && SourceRoots.IsUnder(config.ModulesRoot, file))
        {
            if (config.SharedKernelRoot.Length > 0 && SourceRoots.IsUnder(config.SharedKernelRoot, file))
            {
                return null;
            }

            var abstractions = config.ModulesRoot + ".Abstractions";

            if (abstractions.Length > ".Abstractions".Length && SourceRoots.IsUnder(abstractions, file))
            {
                return null;
            }

            var segments = Path.GetRelativePath(config.ModulesRoot, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Length >= 2 && segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return segments.Length >= 2 ? segments : null;
        }

        return SourceRoots.ModulePath(config, file);
    }

    /// <summary>Whether the file sits in the Contracts folder of the module the mark names, at any depth below it.</summary>
    /// <param name="config">The project configuration that holds the modules root.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <param name="module">The module the qualified identifier states.</param>
    /// <returns>True when the file sits under the module Contracts folder in the module layout.</returns>
    private static bool IsInModuleContracts(ConsumerConfig config, string file, string module)
    {
        if (!SourceRoots.IsProductRoot(config.ModulesRoot) || !SourceRoots.IsUnder(config.ModulesRoot, file))
        {
            return false;
        }

        var segments = Path.GetRelativePath(config.ModulesRoot, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length >= 3
            && SameName(segments[0], module)
            && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The aggregates a rule exception may sit beside: the rule home, and every same-module aggregate the rule governs or binds.</summary>
    /// <param name="model">The loaded project model that resolves the governed and bound records.</param>
    /// <param name="record">The marked rule record.</param>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <returns>The accepted aggregate folder names, starting with the rule home.</returns>
    private static List<string> AcceptedRuleAggregates(ProjectModel model, Record record, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path)
    {
        var accepted = new List<string>();

        if (path.Aggregate is not null)
        {
            accepted.Add(path.Aggregate);
        }

        foreach (var governed in record.Ids(RecordFields.Governs))
        {
            AddGovernedAggregate(model, governed, path.Module!, accepted);
        }

        if (record.Field(RecordFields.Kind) == "atomicity")
        {
            foreach (var bound in record.Ids(RecordFields.Binds))
            {
                AddGovernedAggregate(model, bound, path.Module!, accepted);
            }
        }

        return accepted;
    }

    /// <summary>Adds the aggregate a governed or bound record enforces in, when it sits in the rule module.</summary>
    /// <param name="model">The loaded project model that resolves the governed or bound record.</param>
    /// <param name="id">The governed or bound record identifier.</param>
    /// <param name="module">The module the rule qualified identifier states.</param>
    /// <param name="accepted">The accepted aggregate folder names, receiving the enforcing aggregate.</param>
    private static void AddGovernedAggregate(ProjectModel model, string id, string module, List<string> accepted)
    {
        if (!model.ById.TryGetValue(id, out var target))
        {
            return;
        }

        if (target.Type is not (RecordTypes.Aggregate or RecordTypes.UseCase))
        {
            return;
        }

        var path = PathOf(target, model.Config);

        // Across modules a type is never shared: an aggregate of another module
        // names the exception through the owner Contracts folder instead.
        if (path.Module is null || !SameName(path.Module, module) || path.Aggregate is null)
        {
            return;
        }

        accepted.Add(path.Aggregate);
    }

    /// <summary>Reports the backend namespace that names another path than the mark's qualified identifier.</summary>
    /// <param name="config">The project configuration that holds the product name.</param>
    /// <param name="element">The marked code element whose namespace is checked.</param>
    /// <param name="mark">The mark naming the record the namespace should place.</param>
    /// <param name="record">The marked record.</param>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <param name="ruleHomes">The aggregates a rule mark accepts, or null when the mark names no rule.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBackendNamespace(ConsumerConfig config, MarkedElement element, CodeMark mark, Record record, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path, List<string>? ruleHomes, Dictionary<string, string> namespaces, List<Finding> findings)
    {
        var expected = ExpectedSegments(path);

        if (expected.Count == 0)
        {
            return;
        }

        var product = SourceRoots.ProductName(config);

        if (product.Length == 0)
        {
            return;
        }

        if (NamespaceOf(element.File, namespaces) is not { Length: > 0 } actual)
        {
            return;
        }

        if (PlacedSegments(product, actual) is not { } placed)
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
            return;
        }

        // A rule exception or domain event in its module Contracts folder names no
        // aggregate in its namespace, and a rule exception beside an enforcing
        // aggregate names that aggregate instead of the rule home.
        var significant = placed.Where((segment, index) => index == 0 || !PlacelessSegments.Contains(segment)).ToList();

        if (record.Type is RecordTypes.Event or RecordTypes.Rule
            && significant.Count >= 1
            && SameName(significant[0], path.Module!)
            && (IsInModuleContracts(config, element.File, path.Module!)
                || (record.Type == RecordTypes.Rule
                    && ruleHomes is not null
                    && significant.Count >= 2
                    && ruleHomes.Any(home => SameName(significant[1], home)))))
        {
            return;
        }

        if (!NamespaceMatches(placed, expected) && !NamespaceMatches(placed, WithModuleNamedAggregate(path, expected)))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
        }
    }

    /// <summary>
    ///     Reports every test whose folder or namespace misses the qualified path of the use case its scenario
    ///     proves. A test proving another record carries no folder rule, and an adapter test under its adapter's
    ///     Infrastructure path is exempt by kind. A mark naming no scenario is <c>mark-unknown</c>, not drift.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each marked test.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestPlacement(ProjectModel model, List<Finding> findings)
    {
        var namespaces = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.Test)
            {
                continue;
            }

            if (SourceRoots.LayerOf(model.Config, element.File) == "Infrastructure"
                || SourceRoots.IsUnder(model.Config.InfrastructureRoot, element.File)
                || LayerResolver.FolderLayer(element.File) == Layer.Infrastructure)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Proves)
                {
                    continue;
                }

                if (!model.ById.TryGetValue(mark.RecordId, out var record)
                    || record.Type != RecordTypes.Scenario)
                {
                    continue;
                }

                var path = UseCasePathOf(record, model.Config);

                if (path.Module is null || path.Name is null)
                {
                    continue;
                }

                if (!CheckTestFolder(model.Config, element, mark, path, findings))
                {
                    CheckTestNamespace(model.Config, element, mark, path, namespaces, findings);
                }
            }
        }
    }

    /// <summary>Reports the test folder that misses the qualified path of the scenario's use case.</summary>
    /// <param name="config">The project configuration that holds each test root.</param>
    /// <param name="element">The marked test whose folder is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's qualified identifier states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckTestFolder(ConsumerConfig config, MarkedElement element, CodeMark mark, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path, List<Finding> findings)
    {
        if (TestSegments(config, element.File) is not { } segments)
        {
            return false;
        }

        // Tests mirror the module tree, so a UseCases level states no module,
        // aggregate or use case segment of its own. A module-level UseCases folder
        // holding an aggregate-owned use case keeps its name, so the aggregate
        // check below names it.
        var kept = new List<string> { segments[0] };

        for (var index = 1; index < segments.Length - 1; index++)
        {
            if (!segments[index].Equals("UseCases", StringComparison.OrdinalIgnoreCase)
                || (path.Aggregate is not null && index == 1))
            {
                kept.Add(segments[index]);
            }
        }

        kept.Add(segments[^1]);
        segments = [.. kept];

        if (!SameName(segments[0], path.Module!))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segments[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (path.Aggregate is not null && segments.Length >= 3 && !SameName(segments[1], path.Aggregate))
        {
            if (SameName(path.Aggregate, path.Module!))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        var useCaseAt = path.Aggregate is not null && segments.Length >= 3 && SameName(segments[1], path.Aggregate) ? 2 : 1;

        if (segments.Length > useCaseAt + 1 && !SameName(segments[useCaseAt], path.Name!))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[useCaseAt]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        return false;
    }

    /// <summary>Reports the test namespace that names another path than the scenario's use case.</summary>
    /// <param name="config">The project configuration that holds the product name.</param>
    /// <param name="element">The marked test whose namespace is checked.</param>
    /// <param name="mark">The proves mark naming the scenario.</param>
    /// <param name="path">The module, aggregate and use case the scenario's qualified identifier states.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTestNamespace(ConsumerConfig config, MarkedElement element, CodeMark mark, (string? Module, string? Aggregate, string? Name, bool IsUseCase) path, Dictionary<string, string> namespaces, List<Finding> findings)
    {
        var product = SourceRoots.ProductName(config);

        if (product.Length == 0)
        {
            return;
        }

        if (NamespaceOf(element.File, namespaces) is not { Length: > 0 } actual)
        {
            return;
        }

        var expected = new List<string> { Pascal(path.Module!) };

        if (path.Aggregate is not null && !SameName(path.Aggregate, path.Module!))
        {
            expected.Add(Pascal(path.Aggregate));
        }

        expected.Add(Pascal(path.Name!));

        if (PlacedSegments(product, actual) is not { } placed
            || (!NamespaceMatches(placed, expected) && !NamespaceMatches(placed, WithModuleNamedAggregate(path, expected))))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
        }
    }

    /// <summary>
    ///     Reports every frontend module folder that names no declared module. The use case the folder
    ///     mirrors stays with the mirror check, so only the module segment against the declared modules
    ///     is reported here. A project with no declared modules states no mirror, so it is left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each frontend module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFrontendPlacement(ProjectModel model, List<Finding> findings)
    {
        var modules = DeclaredModules(model);

        if (modules.Count == 0)
        {
            return;
        }

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.FrontendModule)
            {
                continue;
            }

            var folder = Directory.Exists(element.File) ? element.File : Path.GetDirectoryName(element.File) ?? element.File;

            foreach (var frontend in model.Config.Frontends)
            {
                if (ModuleSegment(frontend, folder) is not { } segment)
                {
                    continue;
                }

                if (!modules.Any(module => SameName(segment, module)))
                {
                    var mark = element.Marks.FirstOrDefault(mark => mark.Verb != MarkVerb.Follows);
                    var subject = mark?.RecordId;
                    var file = mark?.File ?? element.File;
                    var line = mark?.Line ?? element.Line;

                    findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{segment}', but no declared module names it.", file, line, subject));
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Reports every backend and frontend folder outside the closed folder sets, and every module and
    ///     aggregate folder that names no declared record. Inside a layer folder subfolders are free, a host
    ///     holds its endpoints and plumbing without a folder rule, and a project with no declared modules,
    ///     aggregates or use cases states no names, so those folders are left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every declared module, aggregate and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckClosedFolders(ProjectModel model, List<Finding> findings)
    {
        var modules = DeclaredModules(model);
        var aggregates = DeclaredAggregates(model);
        var useCases = DeclaredUseCases(model);
        var reactions = DeclaredReactionFolders(model);

        if (model.Config.ModulesRoot.Length > 0 && Directory.Exists(model.Config.ModulesRoot))
        {
            if (SourceRoots.IsProductRoot(model.Config.ModulesRoot))
            {
                CheckModulesRoot(model.Config, modules, aggregates, useCases, reactions, findings);
            }
            else
            {
                CheckLayerProject(model.Config, model.Config.ModulesRoot, modules, findings);
            }
        }

        if (model.Config.SharedKernelRoot.Length > 0 && Directory.Exists(model.Config.SharedKernelRoot))
        {
            CheckSharedKernelRoot(model.Config, findings);
        }

        foreach (var root in new[] { model.Config.DomainRoot, model.Config.InfrastructureRoot })
        {
            if (root.Length > 0 && Directory.Exists(root))
            {
                CheckLayerProject(model.Config, root, modules, findings);
            }
        }

        // Hosts hold their endpoints and plumbing without a folder rule in the module
        // layout; under layer-projects their module folders name declared modules.
        if (!SourceRoots.IsProductRoot(model.Config.ModulesRoot))
        {
            foreach (var root in model.Config.WebApiRoots.Concat(model.Config.WorkerRoots))
            {
                if (root.Length > 0 && Directory.Exists(root))
                {
                    CheckLayerProject(model.Config, root, modules, findings);
                }
            }
        }

        foreach (var frontend in model.Config.Frontends)
        {
            CheckFrontendRoot(model.Config, frontend, modules, findings);
        }
    }

    /// <summary>Reports every folder under the shared kernel root that breaks its closed set of layer folders.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSharedKernelRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.SharedKernelRoot))
        {
            var name = Path.GetFileName(child);

            if (SharedKernelFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(child)}' names no shared kernel layer folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the modules root that breaks its level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModulesRoot(ConsumerConfig config, List<string> modules, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        foreach (var module in ChildFolders(config.ModulesRoot))
        {
            var name = Path.GetFileName(module);

            if (ModulesRootFolders.Contains(name))
            {
                continue;
            }

            if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(module)}' names no Composition folder or declared module.", module, 1, null));
                continue;
            }

            CheckModuleLevel(config, module, name, aggregates, useCases, reactions, findings);
        }
    }

    /// <summary>Reports every folder under one module that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="module">The absolute module folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ConsumerConfig config, string module, string moduleName, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        foreach (var child in ChildFolders(module))
        {
            var name = Path.GetFileName(child);

            if (ModuleFolders.Contains(name))
            {
                if (string.Equals(name, "Contracts", StringComparison.OrdinalIgnoreCase))
                {
                    CheckContractsLevel(config, child, findings);
                }
                else if (string.Equals(name, "UseCases", StringComparison.OrdinalIgnoreCase))
                {
                    CheckUseCasesLevel(config, child, moduleName, null, useCases, reactions, findings);
                }

                continue;
            }

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameName(name, aggregate.Aggregate)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(child)}' names no layer folder or declared aggregate.", child, 1, null));
                continue;
            }

            CheckAggregateLevel(config, child, moduleName, name, useCases, findings);
        }
    }

    /// <summary>Reports every folder under a module Contracts folder that names no kind folder, and every file beside them.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts folder of one module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractsLevel(ConsumerConfig config, string contracts, List<Finding> findings)
    {
        foreach (var child in ChildFolders(contracts))
        {
            var name = Path.GetFileName(child);

            if (!ContractsKindFolders.Contains(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(child)}' names no Contracts kind folder.", child, 1, null));
            }
        }

        foreach (var file in ChildFiles(contracts))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{config.Relative(file)}' sits directly in Contracts/, which holds only its kind folders.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under one aggregate that breaks the aggregate level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregateName">The aggregate folder name as written on disk.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateLevel(ConsumerConfig config, string aggregate, string moduleName, string aggregateName, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        foreach (var child in ChildFolders(aggregate))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "UseCases", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCasesLevel(config, child, moduleName, aggregateName, useCases, [], findings);
                continue;
            }

            if (AggregateFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(child)}' names no layer folder or UseCases folder.", child, 1, null));
        }
    }

    /// <summary>Reports every subfolder under a use case or reaction folder, which holds files only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The absolute use case or reaction folder.</param>
    /// <param name="kind">The folder kind as printed: use case or reaction.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseFilesOnly(ConsumerConfig config, string useCase, string kind, List<Finding> findings)
    {
        foreach (var child in ChildFolders(useCase))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(child)}' sits in {kind} '{Path.GetFileName(useCase)}', which holds files only.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under a UseCases folder that names no declared use case of its home, or no declared reaction at module level.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCases">The absolute UseCases folder of one module or aggregate.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregateName">The aggregate folder name as written on disk, or null at module level.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCasesLevel(ConsumerConfig config, string useCases, string moduleName, string? aggregateName, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        // A module with one aggregate of its own name stays flat, so the module
        // UseCases folder also holds the use cases of that aggregate.
        var owned = aggregateName is null
            ? declared
                .Where(useCase => SameName(moduleName, useCase.Module)
                    && (useCase.Aggregate is null || SameName(useCase.Aggregate, moduleName)))
                .ToList()
            : declared
                .Where(useCase => SameName(moduleName, useCase.Module)
                    && useCase.Aggregate is not null
                    && SameName(aggregateName, useCase.Aggregate))
                .ToList();
        var facts = aggregateName is null
            ? reactions.Where(reaction => SameName(moduleName, reaction.Module)).ToList()
            : [];

        foreach (var child in ChildFolders(useCases))
        {
            var name = Path.GetFileName(child);

            if (owned.Any(useCase => SameName(name, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            if (facts.Any(fact => SameName(name, fact.Folder)))
            {
                CheckUseCaseFilesOnly(config, child, "reaction", findings);
                continue;
            }

            if (owned.Count == 0 && facts.Count == 0)
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            findings.Add(new Finding(
                "code-placement-drift",
                aggregateName is null
                    ? $"Folder '{config.Relative(child)}' names no declared use case or reaction folder."
                    : $"Folder '{config.Relative(child)}' names no declared use case.",
                child,
                1,
                null));
        }
    }

    /// <summary>Reports every module folder under one layer project that names no declared module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="root">The absolute layer project root.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckLayerProject(ConsumerConfig config, string root, List<string> modules, List<Finding> findings)
    {
        if (modules.Count == 0)
        {
            return;
        }

        foreach (var module in ChildFolders(root))
        {
            var name = Path.GetFileName(module);

            if (name.Equals("Endpoints", StringComparison.OrdinalIgnoreCase) || name.Equals("Persistence", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(module)}' names no declared module.", module, 1, null));
            }
        }
    }

    /// <summary>Reports every module folder under one frontend modules root that names no declared module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose modules root is checked.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFrontendRoot(ConsumerConfig config, FrontendConfig frontend, List<string> modules, List<Finding> findings)
    {
        if (modules.Count == 0 || !Directory.Exists(frontend.ModulesRoot))
        {
            return;
        }

        foreach (var module in ChildFolders(frontend.ModulesRoot))
        {
            var name = Path.GetFileName(module);

            if (!modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{config.Relative(module)}' names no declared module.", module, 1, null));
            }
        }
    }

    /// <summary>The module, aggregate and name the record's qualified identifier states for its folder and namespace.</summary>
    /// <param name="record">The marked record.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The module, aggregate and name, with nulls where the qualified identifier states no placement.</returns>
    private static (string? Module, string? Aggregate, string? Name, bool IsUseCase) PathOf(Record record, ConsumerConfig config)
    {
        var qualified = record.QualifiedId.Length > 0
            ? record.QualifiedId
            : QualifiedIdentifier.Of(record.Type, record.Id, record.File, config);
        var segments = qualified.Split('/');

        if (segments.Length < 2 || segments[0] == record.Type)
        {
            return (null, null, null, record.Type == RecordTypes.UseCase);
        }

        var module = segments[0];
        var at = Array.IndexOf(segments, record.Type, 1);

        if (at < 0)
        {
            return record.Type == RecordTypes.Aggregate && segments.Length == 2
                ? (module, segments[1], segments[1], false)
                : (module, null, segments[^1], false);
        }

        // The aggregate sits directly under the module, even when the record nests
        // deeper, such as an event its use case raises.
        var aggregate = at > 1 ? segments[1] : null;
        var name = at + 1 < segments.Length ? segments[at + 1] : null;

        return (module, aggregate, name, record.Type == RecordTypes.UseCase);
    }

    /// <summary>The module, aggregate and use case the scenario's qualified identifier states for its test.</summary>
    /// <param name="record">The marked scenario record.</param>
    /// <param name="config">The consumer configuration holding the record roots.</param>
    /// <returns>The module, aggregate and use case home, with nulls where the qualified identifier states no placement.</returns>
    private static (string? Module, string? Aggregate, string? Name, bool IsUseCase) UseCasePathOf(Record record, ConsumerConfig config)
    {
        var qualified = record.QualifiedId.Length > 0
            ? record.QualifiedId
            : QualifiedIdentifier.Of(record.Type, record.Id, record.File, config);
        var segments = qualified.Split('/');
        var at = Array.IndexOf(segments, RecordTypes.UseCase);

        if (at <= 0 || at + 1 >= segments.Length)
        {
            return (null, null, null, false);
        }

        var aggregate = at > 1 ? segments[at - 1] : null;

        return (segments[0], aggregate, segments[at + 1], true);
    }

    /// <summary>The expected namespace segments after the product for the qualified path of the marked record type.</summary>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <returns>The expected segments in placement order, or an empty list when the path states no placement.</returns>
    private static List<string> ExpectedSegments((string? Module, string? Aggregate, string? Name, bool IsUseCase) path)
    {
        if (path.Module is null)
        {
            return [];
        }

        var expected = new List<string> { Pascal(path.Module) };

        if (path.Aggregate is not null && !SameName(path.Aggregate, path.Module))
        {
            expected.Add(Pascal(path.Aggregate));
        }

        if (path.Name is not null && path.IsUseCase && !SameName(path.Name, path.Aggregate ?? ""))
        {
            expected.Add(Pascal(path.Name));
        }

        return expected;
    }

    /// <summary>
    ///     Whether the declared namespace segments carry each expected segment in order, with only layer and
    ///     test folder names before or between them. A module or aggregate named like such a folder, such as
    ///     an Events module, still counts as its own segment.
    /// </summary>
    /// <param name="placed">The declared namespace segments after the product.</param>
    /// <param name="expected">The expected segments in placement order, without the product.</param>
    /// <returns>True when the placed segments carry each expected segment in order.</returns>
    private static bool NamespaceMatches(List<string> placed, List<string> expected)
    {
        var at = 0;

        foreach (var segment in expected)
        {
            while (at < placed.Count && !SameName(placed[at], segment) && PlacelessSegments.Contains(placed[at]))
            {
                at++;
            }

            if (at == placed.Count || !SameName(placed[at], segment))
            {
                return false;
            }

            at++;
        }

        return true;
    }

    /// <summary>
    ///     The expected segments with the aggregate kept after its module when both share a name, because the
    ///     module layout gives such an aggregate its own folder, as in Directory.Directory.
    /// </summary>
    /// <param name="path">The module, aggregate and name the qualified identifier states.</param>
    /// <param name="expected">The expected segments that drop the aggregate named like its module.</param>
    /// <returns>The expected segments with that aggregate, or the expected segments unchanged.</returns>
    private static List<string> WithModuleNamedAggregate((string? Module, string? Aggregate, string? Name, bool IsUseCase) path, List<string> expected)
    {
        if (path.Module is null || path.Aggregate is null || !SameName(path.Aggregate, path.Module) || expected.Count == 0)
        {
            return expected;
        }

        return [expected[0], Pascal(path.Aggregate), .. expected.Skip(1)];
    }

    /// <summary>The declared namespace segments after the product.</summary>
    /// <param name="product">The product name starting the namespace.</param>
    /// <param name="actual">The declared namespace as written in the file.</param>
    /// <returns>The segments after the product, or null when the namespace is empty or starts outside the product.</returns>
    private static List<string>? PlacedSegments(string product, string actual)
    {
        var segments = actual.Split('.').Where(segment => segment.Length > 0).ToList();

        if (segments.Count == 0 || !SameName(segments[0], product))
        {
            return null;
        }

        return segments.Skip(1).ToList();
    }

    /// <summary>The declared namespace of the file, or an empty string when the file cannot be read or declares none.</summary>
    /// <param name="file">The full path of the file whose namespace is wanted.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <returns>The declared namespace, or an empty string when none could be read.</returns>
    private static string NamespaceOf(string file, Dictionary<string, string> namespaces)
    {
        if (namespaces.TryGetValue(file, out var known))
        {
            return known;
        }

        string found = "";

        try
        {
            if (File.Exists(file))
            {
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                var spaces = tree.GetRoot().DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().ToList();

                if (spaces.FirstOrDefault(space => space is FileScopedNamespaceDeclarationSyntax) is { } scoped)
                {
                    found = scoped.Name.ToString();
                }
                else if (spaces.Count > 0)
                {
                    found = string.Join(".", spaces.Select(space => space.Name.ToString()));
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            found = "";
        }

        namespaces[file] = found;

        return found;
    }

    /// <summary>Whether the endpoint sits in a host, where host endpoints carry their mark without a use case folder.</summary>
    /// <param name="config">The project configuration naming the host roots.</param>
    /// <param name="file">The full path of the endpoint file.</param>
    /// <returns>True when the file sits under a host root in the module layout.</returns>
    private static bool IsHostEndpoint(ConsumerConfig config, string file) =>
        !SourceRoots.IsProductRoot(config.ModulesRoot)
            ? false
            : SourceRoots.IsUnderHost(config, file);

    /// <summary>The test path segments under the test project, or null when the file sits outside every test root or in a project root.</summary>
    /// <param name="config">The project configuration that holds each test root.</param>
    /// <param name="file">The full path of the test file.</param>
    /// <returns>The path segments with the module first and the file name last, or null outside the test mirror.</returns>
    private static string[]? TestSegments(ConsumerConfig config, string file)
    {
        foreach (var root in config.TestRoots)
        {
            if (!SourceRoots.IsUnder(root, file))
            {
                continue;
            }

            var segments = Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (segments.Length >= 3)
            {
                return segments.Skip(1).ToArray();
            }

            return null;
        }

        return null;
    }

    /// <summary>The module segment of a frontend module folder under the frontend modules root, or null outside it.</summary>
    /// <param name="frontend">The frontend whose modules root the folder sits under.</param>
    /// <param name="folder">The absolute folder path.</param>
    /// <returns>The first folder segment under the modules root, or null outside the root.</returns>
    private static string? ModuleSegment(FrontendConfig frontend, string folder)
    {
        if (!Directory.Exists(frontend.ModulesRoot))
        {
            return null;
        }

        var relative = Path.GetRelativePath(Path.GetFullPath(frontend.ModulesRoot), Path.GetFullPath(folder));

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            return null;
        }

        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Length > 0 && segments[0] is not "." && segments[0].Length > 0 ? segments[0] : null;
    }

    /// <summary>The declared module names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module names in model order.</returns>
    private static List<string> DeclaredModules(ProjectModel model)
    {
        var modules = new List<string>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Module)
            {
                continue;
            }

            var qualified = record.QualifiedId.Length > 0
                ? record.QualifiedId
                : QualifiedIdentifier.Of(record.Type, record.Id, record.File, model.Config);

            if (qualified.Length > 0 && !qualified.Contains('/', StringComparison.Ordinal))
            {
                modules.Add(qualified);
            }
            else if (Identifiers.TrySplit(record.Id, out _, out _, out var name))
            {
                modules.Add(name);
            }
        }

        return modules;
    }

    /// <summary>The declared aggregates by module and aggregate name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module and aggregate names of each aggregate record with a placed qualified identifier.</returns>
    private static List<(string Module, string Aggregate)> DeclaredAggregates(ProjectModel model)
    {
        var aggregates = new List<(string Module, string Aggregate)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            var path = PathOf(record, model.Config);

            if (path.Module is not null && path.Aggregate is not null)
            {
                aggregates.Add((path.Module, path.Aggregate));
            }
        }

        return aggregates;
    }

    /// <summary>The declared use cases by module, aggregate and name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate and name of each use case record with a placed qualified identifier.</returns>
    private static List<(string Module, string? Aggregate, string Name)> DeclaredUseCases(ProjectModel model)
    {
        var useCases = new List<(string Module, string? Aggregate, string Name)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var path = PathOf(record, model.Config);

            if (path.Module is not null && path.Name is not null)
            {
                useCases.Add((path.Module, path.Aggregate, path.Name));
            }
        }

        return useCases;
    }

    /// <summary>
    ///     The declared reaction folders by module and folder name, one On{Fact} folder per trigger of each reaction.
    ///     The fact is the name of the event class that carries the trigger's mark, without its Event suffix. A
    ///     trigger with no marked class allows its name alone and its home and name, since either can name the class.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module and folder name of each reaction trigger with a placed qualified identifier.</returns>
    private static List<(string Module, string Folder)> DeclaredReactionFolders(ProjectModel model)
    {
        var folders = new List<(string Module, string Folder)>();
        var eventClasses = model.Code.Elements
            .Where(element => element.Kind == CodeKind.DomainEvent)
            .SelectMany(element => element.Marks.Select(mark => (mark.RecordId, element.Class)))
            .ToLookup(pair => pair.RecordId, pair => pair.Class, StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Reaction)
            {
                continue;
            }

            var path = PathOf(record, model.Config);

            if (path.Module is null)
            {
                continue;
            }

            foreach (var trigger in record.Ids(RecordFields.TriggeredBy))
            {
                var classes = eventClasses[trigger].ToList();

                if (classes.Count > 0)
                {
                    folders.AddRange(classes.Select(type => (path.Module, "On" + (type.EndsWith("Event", StringComparison.Ordinal) ? type[..^"Event".Length] : type))));
                }
                else if (Identifiers.TrySplit(trigger, out _, out var home, out var name) && name.Length > 0)
                {
                    folders.Add((path.Module, "On" + Pascal(name)));

                    if (home is not null && !name.StartsWith(home + "-", StringComparison.Ordinal))
                    {
                        folders.Add((path.Module, "On" + Pascal(home + "-" + name)));
                    }
                }
            }
        }

        return folders;
    }

    /// <summary>The child folders of one folder in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="folder">The absolute folder to read.</param>
    /// <returns>The child folders in ordinal order, without build output and tool folders.</returns>
    private static List<string> ChildFolders(string folder)
    {
        try
        {
            return Directory.EnumerateDirectories(folder)
                .Where(child => !SkippedFolders.Contains(Path.GetFileName(child)))
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The files directly in one folder in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="folder">The absolute folder to read.</param>
    /// <returns>The files directly in the folder in ordinal order.</returns>
    private static List<string> ChildFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder)
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>Whether a folder name and a record name spell one name: case, hyphens and underscores ignored.</summary>
    /// <param name="folder">The folder name as written on disk.</param>
    /// <param name="name">The record name segment taken from the identifier.</param>
    /// <returns>True when both names agree with case, hyphens and underscores ignored; otherwise false.</returns>
    private static bool SameName(string folder, string name)
    {
        var left = folder.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        var right = name.Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The PascalCase spelling of a kebab-case record name segment.</summary>
    /// <param name="name">The record name segment.</param>
    /// <returns>The segment with each word capitalized and its separators removed.</returns>
    private static string Pascal(string name)
    {
        var built = new System.Text.StringBuilder();

        foreach (var word in name.Split('-', '_'))
        {
            if (word.Length == 0)
            {
                continue;
            }

            built.Append(char.ToUpperInvariant(word[0]));

            if (word.Length > 1)
            {
                built.Append(word[1..]);
            }
        }

        return built.ToString();
    }
}
