using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.NamespacePlacement;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The backend placement check: every backend mark sits under the code folder its record's placement names, with a namespace that names the same path.</summary>
/// <follows>litespec/core/model#match-each-backend-mark-to-its-folder</follows>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class BackendPlacementChecks
{
    /// <summary>
    ///     Reports every backend mark whose namespace or aggregate folder misses the code folder its record's placement names:
    ///     a use case mark outside its aggregate folder, an event or rule mark outside its module or aggregate,
    ///     an external-system mark outside its module, and any of those with a namespace that names another path.
    ///     A mark naming no record, a record of another type, or a record its kind does not take is
    ///     <c>mark-unknown</c>, not drift. A host endpoint, a technical class, an aggregate method and a
    ///     frontend element carry no backend placement, so they are left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and each marked code element.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var placed = model.Code.Elements.Where(element => HasBackendPlacement(model.Config, element)).ToList();
        var namespaces = NamespacesOf(placed.Where(element => element.Marks.Count > 0).Select(element => element.File));

        foreach (var element in placed)
        {
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

                var path = PathOf(record);

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
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="ruleHomes">The aggregates a rule mark accepts, or null when the mark names no rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckBackendFolder(ProjectModel model, MarkedElement element, CodeMark mark, Record record, PlacementPath path, List<string>? ruleHomes, List<Finding> findings)
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
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckUseCaseElementFolder(ConsumerConfig config, MarkedElement element, CodeMark mark, PlacementPath path, List<Finding> findings)
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

            var segments = PathText.Relative(config.ModulesRoot, file).Split('/');

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
    /// <param name="module">The module the record's folder path states.</param>
    /// <returns>True when the file sits under the module Contracts folder in the module layout.</returns>
    private static bool IsInModuleContracts(ConsumerConfig config, string file, string module)
    {
        if (!SourceRoots.IsProductRoot(config.ModulesRoot) || !SourceRoots.IsUnder(config.ModulesRoot, file))
        {
            return false;
        }

        var segments = PathText.Relative(config.ModulesRoot, file).Split('/');

        return segments.Length >= 3
            && SameName(segments[0], module)
            && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The aggregates a rule exception may sit beside: the rule home, and every same-module aggregate the rule governs or binds.</summary>
    /// <param name="model">The loaded project model that resolves the governed and bound records.</param>
    /// <param name="record">The marked rule record.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <returns>The accepted aggregate folder names, starting with the rule home.</returns>
    private static List<string> AcceptedRuleAggregates(ProjectModel model, Record record, PlacementPath path)
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
    /// <param name="module">The module the rule's folder path states.</param>
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

        var path = PathOf(target);

        // Across modules a type is never shared: an aggregate of another module
        // names the exception through the owner Contracts folder instead.
        if (path.Module is null || !SameName(path.Module, module) || path.Aggregate is null)
        {
            return;
        }

        accepted.Add(path.Aggregate);
    }

    /// <summary>Reports the backend namespace that names another path than the marked record's folder path.</summary>
    /// <param name="config">The project configuration that holds the product name.</param>
    /// <param name="element">The marked code element whose namespace is checked.</param>
    /// <param name="mark">The mark naming the record the namespace should place.</param>
    /// <param name="record">The marked record.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="ruleHomes">The aggregates a rule mark accepts, or null when the mark names no rule.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBackendNamespace(ConsumerConfig config, MarkedElement element, CodeMark mark, Record record, PlacementPath path, List<string>? ruleHomes, Dictionary<string, string> namespaces, List<Finding> findings)
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

    /// <summary>Whether the endpoint sits in a host, where host endpoints carry their mark without a use case folder.</summary>
    /// <param name="config">The project configuration naming the host roots.</param>
    /// <param name="file">The full path of the endpoint file.</param>
    /// <returns>True when the file sits under a host root in the module layout.</returns>
    private static bool IsHostEndpoint(ConsumerConfig config, string file) =>
        !SourceRoots.IsProductRoot(config.ModulesRoot)
            ? false
            : SourceRoots.IsUnderHost(config, file);

    /// <summary>
    ///     Whether the element carries a backend placement: not a test, a frontend element, an
    ///     aggregate method, a technical class or a host endpoint.
    /// </summary>
    /// <param name="config">The project configuration naming the host roots.</param>
    /// <param name="element">The marked code element.</param>
    /// <returns>True when the element's folder and namespace follow its record.</returns>
    private static bool HasBackendPlacement(ConsumerConfig config, MarkedElement element) =>
        !element.InTests
        && element.Kind is not (CodeKind.Test or CodeKind.BrowserTest
            or CodeKind.FrontendModule or CodeKind.FrontendRoute
            or CodeKind.AggregateMethod or CodeKind.Technical)
        && !(element.Kind == CodeKind.Endpoint && IsHostEndpoint(config, element.File));
}
