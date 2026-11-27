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
/// <follows>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
/// <follows>litespec/core/backend-application#use-this-use-case-layout</follows>
internal static class BackendPlacementChecks
{
    /// <summary>The layer folders a use case holds its folder under: Application always, and WebApi for an endpoint.</summary>
    static readonly HashSet<string> UseCaseLayerFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application", "WebApi",
    };

    /// <summary>
    ///     Reports every backend mark whose namespace or folder misses the code folder its record's placement names:
    ///     a use case mark outside its use case folder in its aggregate folder under Application or WebApi, a port or adapter mark outside its module Ports or Infrastructure folder,
    ///     an event or rule mark outside its module or aggregate, an external-system mark outside its module, and any of those with a namespace that names another path.
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
            // Knowledge projects follow the knowledge layout, not the module tree.
            if (SourceRoots.IsUnderKnowledge(model.Config, element.File))
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb == MarkVerb.Follows)
                {
                    continue;
                }

                if (!model.ById.TryGetValue(mark.RecordId, out var record))
                {
                    continue;
                }

                // A read model projection or reader carries its read model mark as a technical class mark. The
                // read model's folder is its module ReadModels folder, so only the read model placement applies to it.
                if (record.Type == "read-model")
                {
                    if (!CheckReadModelPlacement(model, element, mark, record, findings))
                    {
                        CheckBackendNamespace(model, element, mark, record, PathOf(record), null, namespaces, findings);
                    }

                    continue;
                }

                if (element.Kind == CodeKind.Technical || !MarkRules.Accepts(element.Kind, mark.RecordId))
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

                // A port interface lives in its module Ports folder, and its adapter in the
                // same-named Infrastructure folder, whatever the external system record
                // places, since the system record sits at product level with no module
                // of its own. The port folder takes the interface name; an adapter takes
                // the folder of the port it implements.
                if (element.Kind is CodeKind.Port or CodeKind.Adapter
                    && record.Type == RecordTypes.ExternalSystem)
                {
                    CheckPortPlacement(model, element, mark, findings);
                    continue;
                }

                // A rule the product holds is enforced by a shared value, so its code lives in
                // the Kernel, wherever the rule's record file sits.
                if (record.Type == RecordTypes.Rule
                    && Identifiers.TrySplit(record.Id, out _, out var ruleHome, out _)
                    && ruleHome is not null
                    && model.ById.ContainsKey($"product/{ruleHome}"))
                {
                    if (!SourceRoots.IsUnder(model.Config.KernelRoot, element.File))
                    {
                        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' implements the product rule '{record.Id}', so it belongs in the Kernel.", mark.File, mark.Line, mark.RecordId));
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
                    CheckBackendNamespace(model, element, mark, record, path, ruleHomes, namespaces, findings);
                }
            }
        }
    }

    /// <summary>
    ///     Reports the backend folder the mark misses: the module folder for an event, rule or external-system
    ///     mark, the use case folder for a handler or endpoint mark, and the aggregate folder
    ///     for an event, rule or aggregate mark. A rule exception or domain event in its module contracts,
    ///     and a rule exception beside an aggregate its rule governs or binds, sit where the module
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
            if (record.Type is RecordTypes.DomainEvent or RecordTypes.Rule or RecordTypes.ExternalSystem)
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
            return CheckUseCaseElementFolder(model, element, mark, path, findings);
        }

        if (element.Kind is CodeKind.EventHandler or CodeKind.Job
            && record.Type is RecordTypes.Reaction or RecordTypes.Job)
        {
            return CheckTriggerFolder(model, element, mark, record, path, findings);
        }

        if (path.Aggregate is null
            || record.Type is not (RecordTypes.DomainEvent or RecordTypes.Rule or RecordTypes.Aggregate))
        {
            return false;
        }

        // A rule exception or domain event another module names lives in its owner
        // contracts, outside the aggregate folders the comparison below reads.
        if (record.Type is RecordTypes.DomainEvent or RecordTypes.Rule
            && IsInModuleContracts(model.Config, element.File, path.Module!))
        {
            return false;
        }

        // A Domain type directly in its layer belongs in its aggregate folder: every
        // module keeps its plural aggregate folder whatever its number of aggregates.
        if (segments.Length < 3)
        {
            var direct = RawModulePath(model.Config, element.File) is { } raw && raw.Length >= 3 ? raw[1] : segments[0];
            var expected = ExpectedAggregateFolder(model, path.Module!, path.Aggregate!);

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits directly in '{direct}/', but marks '{mark.RecordId}', so it belongs in '{expected}/'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        if (SameAggregate(segments[1], path.Aggregate))
        {
            return false;
        }

        // A rule lives in its own folder, but its exception lives with the
        // aggregate that enforces it, which the rule names in governs or binds.
        if (record.Type == RecordTypes.Rule
            && ruleHomes is not null
            && ruleHomes.Any(home => SameAggregate(segments[1], home)))
        {
            return false;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{segments[1]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     Reports the folder a handler or endpoint misses for its use case: a use case whose record sits under an
    ///     aggregate lives in its plural aggregate folder under Application or WebApi, and a module-level
    ///     use case lives in its own folder directly under one of those layers. The finding names the expected folder.
    ///     A use case folder naming another use case, and a module folder naming another module, stay with
    ///     <c>mark-folder-drift</c>, so they are left out here, and a use case folder flat in its module stays with the
    ///     closed folder check, which names the missing layer level.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="element">The marked handler or endpoint whose folder is checked.</param>
    /// <param name="mark">The mark naming the use case the folder should hold.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckUseCaseElementFolder(ProjectModel model, MarkedElement element, CodeMark mark, PlacementPath path, List<Finding> findings)
    {
        // The raw path derives the element's own folders: the layer folder above
        // the use case folder keeps its written name.
        if (RawModulePath(model.Config, element.File) is not { } raw || raw.Length < 2)
        {
            return false;
        }

        if (!SameName(raw[0], path.Module!))
        {
            return false;
        }

        // A use case's handler, message and steps sit in its Application folder, and its endpoint
        // in its WebApi folder. Under an aggregate the use case folder sits in the plural
        // aggregate folder under the layer folder.
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

        // Directly under the module the closed folder check names the missing layer level.
        if (above is null)
        {
            return false;
        }

        if (path.Aggregate is null)
        {
            return CheckModuleLevelUseCaseFolder(element, mark, folders, findings);
        }

        return CheckAggregateUseCaseFolder(model, element, mark, path, raw[0], folders, findings);
    }

    /// <summary>
    ///     Reports the folder a handler or endpoint of a module-level use case misses: its own folder directly under
    ///     Application or WebApi. A use case nested under an aggregate folder reports that folder.
    /// </summary>
    /// <param name="element">The marked handler or endpoint whose folder is checked.</param>
    /// <param name="mark">The mark naming the use case the folder should hold.</param>
    /// <param name="folders">The folders under the module with the file name dropped, the use case folder last.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckModuleLevelUseCaseFolder(MarkedElement element, CodeMark mark, string[] folders, List<Finding> findings)
    {
        var above = folders[^2];

        if (UseCaseLayerFolders.Contains(above))
        {
            var home = folders.Length >= 3 ? folders[^3] : null;

            if (home is null)
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{home}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{above}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     Reports the folder a handler or endpoint of an aggregate-owned use case misses: its use case folder in the
    ///     plural aggregate folder under Application or WebApi. The finding names the expected folder.
    ///     Flat in its own aggregate folder the closed folder check names the missing layer level.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="element">The marked handler or endpoint whose folder is checked.</param>
    /// <param name="mark">The mark naming the use case the folder should hold.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="module">The module folder as written on disk.</param>
    /// <param name="folders">The folders under the module with the file name dropped, the use case folder last.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    private static bool CheckAggregateUseCaseFolder(ProjectModel model, MarkedElement element, CodeMark mark, PlacementPath path, string module, string[] folders, List<Finding> findings)
    {
        var above = folders[^2];
        var expected = ExpectedAggregateFolder(model, path.Module!, path.Aggregate!);
        var useCase = Naming.Pascal(path.Name!);

        if (UseCaseLayerFolders.Contains(above))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{folders[^1]}', but marks '{mark.RecordId}', so it belongs in '{module}/{above}/{expected}/{useCase}/'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        var layer = folders.Length >= 3 ? folders[^3] : null;

        if (SameAggregate(above, path.Aggregate!))
        {
            // Flat in its own aggregate folder the closed folder check names the missing layer level.
            if (layer is null || !UseCaseLayerFolders.Contains(layer))
            {
                return false;
            }

            if (SameName(above, expected))
            {
                return false;
            }
        }

        var home = layer is not null && UseCaseLayerFolders.Contains(layer)
            ? layer
            : element.Kind == CodeKind.Endpoint ? "WebApi" : "Application";

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{above}', but marks '{mark.RecordId}', so it belongs in '{module}/{home}/{expected}/{useCase}/'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     The path segments of the file under its module layout without leaving any
    ///     folder out, with the module first and the file name last, or null when the
    ///     file sits outside every module. Under one module project the module name
    ///     comes from the project folder.
    /// </summary>
    /// <param name="config">The project configuration that holds each module root, the Kernel root and the Platform root.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <returns>The raw path segments with the module first, or null when the file sits outside the module layout.</returns>
    private static string[]? RawModulePath(ConsumerConfig config, string file)
    {
        if (SourceRoots.IsUnderKernelOrPlatform(config, file))
        {
            return null;
        }

        foreach (var modules in config.ModulesRoots)
        {
            if (!SourceRoots.IsUnder(modules, file))
            {
                continue;
            }

            var segments = PathText.Relative(modules, file).Split('/');

            if (segments.Length >= 1 && segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (SourceRoots.IsSingleModulesProject(modules))
            {
                return segments.Length >= 2 ? segments : null;
            }

            if (SourceRoots.ModuleNameOf(modules) is not { } module)
            {
                return null;
            }

            if (segments.Length < 1)
            {
                return null;
            }

            return segments.Length == 1 ? [module, segments[0]] : [module, .. segments];
        }

        return null;
    }

    /// <summary>
    ///     Reports the folder a reaction or job trigger misses, read from its record's effects: the use case
    ///     folder under Application for the use case it runs, inside its aggregate folder when the use case sits
    ///     under one, else the Notifications folder under Application for the notification it sends, else the
    ///     ExternalSystems folder under Application for the external system it calls. A trigger in another module
    ///     folder stays with <c>mark-folder-drift</c>.
    /// </summary>
    /// <param name="model">The loaded project model that resolves the use case the trigger runs.</param>
    /// <param name="element">The marked event handler or job host whose folder is checked.</param>
    /// <param name="mark">The mark naming the reaction or job the folder should hold.</param>
    /// <param name="record">The marked reaction or job record.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    /// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
    private static bool CheckTriggerFolder(ProjectModel model, MarkedElement element, CodeMark mark, Record record, PlacementPath path, List<Finding> findings)
    {
        if (TriggerHomeOf(model, record) is not { } home
            || RawModulePath(model.Config, element.File) is not { } raw
            || raw.Length < 2
            || !SameName(raw[0], path.Module!))
        {
            return false;
        }

        var folders = raw[..^1];
        var placed = home.Container == "Application" && home.Aggregate is not null
            ? folders.Length >= 4
                && string.Equals(folders[^3], "Application", StringComparison.OrdinalIgnoreCase)
                && SameName(folders[^2], home.Aggregate)
                && SameName(folders[^1], home.Name)
            : home.Container == "Application"
                ? folders.Length >= 3
                    && string.Equals(folders[^2], "Application", StringComparison.OrdinalIgnoreCase)
                    && SameName(folders[^1], home.Name)
                : folders.Length >= 4
                    && string.Equals(folders[^3], "Application", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(folders[^2], home.Container, StringComparison.OrdinalIgnoreCase)
                    && SameName(folders[^1], home.Name);

        if (placed)
        {
            return false;
        }

        var expected = home.Container == "Application" && home.Aggregate is not null
            ? $"{raw[0]}/Application/{home.Aggregate}/{home.Name}"
            : home.Container == "Application"
                ? $"{raw[0]}/Application/{home.Name}"
                : $"{raw[0]}/Application/{home.Container}/{home.Name}";

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{folders[^1]}', but its trigger '{mark.RecordId}' belongs in '{expected}'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>
    ///     Reports the folder a port interface or adapter misses: the module Ports folder named for the port for
    ///     an interface, and the same-named Infrastructure folder for an adapter, whether one use case or many
    ///     call the port. The port folder takes the interface name; an adapter takes the folder of the port
    ///     it implements. An interface in module contracts is reported by the closed-folder check, so it is
    ///     left out here. The finding names the expected folder.
    /// </summary>
    /// <param name="model">The loaded project model whose configuration names the module roots.</param>
    /// <param name="element">The marked port interface or adapter whose folder is checked.</param>
    /// <param name="mark">The mark naming the external system the port talks to.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortPlacement(ProjectModel model, MarkedElement element, CodeMark mark, List<Finding> findings)
    {
        if (RawModulePath(model.Config, element.File) is not { } raw || raw.Length < 2)
        {
            return;
        }

        if (IsInModuleContracts(model.Config, element.File, raw[0]))
        {
            return;
        }

        var port = element.Kind == CodeKind.Port ? PortOf(element.Class) : AdapterPortOf(model, element, mark, raw[0]);
        var layer = element.Kind == CodeKind.Port ? "Ports" : "Infrastructure";
        var folders = raw[1..^1];

        if (folders.Length == 2
            && string.Equals(folders[0], layer, StringComparison.OrdinalIgnoreCase)
            && SameName(folders[1], port))
        {
            return;
        }

        var where = folders.Length == 0 ? raw[0] : folders[^1];

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{where}', but marks '{mark.RecordId}', so it belongs in '{raw[0]}/{layer}/{port}/'.", mark.File, mark.Line, mark.RecordId));
    }

    /// <summary>The port folder a port interface takes: its own name without the leading I.</summary>
    /// <param name="className">The interface name, such as ITextGateway.</param>
    /// <returns>The port folder name, such as TextGateway.</returns>
    private static string PortOf(string className) =>
        className.Length > 1 && className[0] == 'I' && char.IsUpper(className[1])
            ? className[1..]
            : className;

    /// <summary>
    ///     The port folder an adapter takes: the folder of the port it implements, read from the port
    ///     interface in its module that carries the same external-system mark, or its own name without
    ///     the trailing Adapter when no such port is read.
    /// </summary>
    /// <param name="model">The loaded project model that holds every marked port.</param>
    /// <param name="element">The marked adapter whose port is wanted.</param>
    /// <param name="mark">The mark naming the external system the adapter talks to.</param>
    /// <param name="module">The module folder as written on disk.</param>
    /// <returns>The port folder name.</returns>
    private static string AdapterPortOf(ProjectModel model, MarkedElement element, CodeMark mark, string module)
    {
        foreach (var candidate in model.Code.Elements)
        {
            if (candidate.Kind != CodeKind.Port
                || !candidate.Marks.Any(marked => string.Equals(marked.RecordId, mark.RecordId, StringComparison.Ordinal)))
            {
                continue;
            }

            if (RawModulePath(model.Config, candidate.File) is { } portRaw
                && portRaw.Length >= 2
                && SameName(portRaw[0], module))
            {
                return PortOf(candidate.Class);
            }
        }

        var name = element.Class;

        return name.EndsWith("Adapter", StringComparison.Ordinal) && name.Length > "Adapter".Length
            ? name[..^"Adapter".Length]
            : name;
    }

    /// <summary>
    ///     Reports the folder a read model projection or reader misses: the Application ReadModels folder named for the read model's plural,
    ///     <c>Application/ReadModels/&lt;ReadModelPlural&gt;/</c>, which holds files only. A read model in another module folder is reported too.
    /// </summary>
    /// <param name="model">The loaded project model whose configuration names the module roots.</param>
    /// <param name="element">The marked code element that carries the read model mark.</param>
    /// <param name="mark">The read model mark naming the read model the folder should hold.</param>
    /// <param name="readModel">The marked read model record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder was reported, so the namespace stays silent for the same mark.</returns>
    /// <follows>litespec/packs/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</follows>
    private static bool CheckReadModelPlacement(ProjectModel model, MarkedElement element, CodeMark mark, Record readModel, List<Finding> findings)
    {
        var path = PathOf(readModel);

        if (path.Module is null
            || path.Name is null
            || RawModulePath(model.Config, element.File) is not { } raw
            || raw.Length < 2)
        {
            return false;
        }

        if (!SameName(raw[0], path.Module))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in module folder '{raw[0]}', but marks '{mark.RecordId}'.", mark.File, mark.Line, mark.RecordId));
            return true;
        }

        var folders = raw[..^1];
        var plural = ReadModelFolderOf(path.Name);

        if (folders.Length >= 4
            && string.Equals(folders[^3], "Application", StringComparison.OrdinalIgnoreCase)
            && string.Equals(folders[^2], "ReadModels", StringComparison.OrdinalIgnoreCase)
            && SameName(folders[^1], plural))
        {
            return false;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' sits in folder '{folders[^1]}', but marks '{mark.RecordId}', so it belongs in '{raw[0]}/Application/ReadModels/{plural}/'.", mark.File, mark.Line, mark.RecordId));
        return true;
    }

    /// <summary>Whether the file sits in the contracts of the module the mark names: its sibling Contracts project, or its Contracts folder in the one-modules-project deviation.</summary>
    /// <param name="config">The project configuration that holds the module projects.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <param name="module">The module the record's folder path states.</param>
    /// <returns>True when the file sits under the module Contracts project or Contracts folder in the module layout.</returns>
    private static bool IsInModuleContracts(ConsumerConfig config, string file, string module)
    {
        foreach (var modules in config.ModulesRoots)
        {
            if (!SourceRoots.IsUnder(modules, file))
            {
                continue;
            }

            if (SourceRoots.IsSingleModulesProject(modules))
            {
                var segments = PathText.Relative(modules, file).Split('/');

                if (segments.Length >= 3
                    && SameName(segments[0], module)
                    && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (SourceRoots.IsContractsProject(modules)
                && SourceRoots.ModuleNameOf(modules) is { } own
                && SameName(own, module))
            {
                return true;
            }
        }

        return false;
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

        return accepted;
    }

    /// <summary>Adds the aggregate a governed record enforces in, when it sits in the rule module.</summary>
    /// <param name="model">The loaded project model that resolves the governed record.</param>
    /// <param name="id">The governed record identifier.</param>
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
        // names the exception through the owner contracts instead.
        if (path.Module is null || !SameName(path.Module, module) || path.Aggregate is null)
        {
            return;
        }

        accepted.Add(path.Aggregate);
    }

    /// <summary>The layer folders a namespace states after its module: the top folder of the file in its module.</summary>
    static readonly HashSet<string> NamespaceLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Ports", "Infrastructure", "WebApi",
    };

    /// <summary>Reports the backend namespace that names another path than the marked record's folder path, or that names another layer than the file's top folder.</summary>
    /// <param name="model">The loaded project model that resolves the plural aggregate folder.</param>
    /// <param name="element">The marked code element whose namespace is checked.</param>
    /// <param name="mark">The mark naming the record the namespace should place.</param>
    /// <param name="record">The marked record.</param>
    /// <param name="path">The module, aggregate and name the record's folder path states.</param>
    /// <param name="ruleHomes">The aggregates a rule mark accepts, or null when the mark names no rule.</param>
    /// <param name="namespaces">The parsed namespace per file, filled as files are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckBackendNamespace(ProjectModel model, MarkedElement element, CodeMark mark, Record record, PlacementPath path, List<string>? ruleHomes, Dictionary<string, string> namespaces, List<Finding> findings)
    {
        var expected = ExpectedSegments(model, path);

        if (expected.Count == 0)
        {
            return;
        }

        var product = SourceRoots.ProductName(model.Config);

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

        // A rule exception or domain event in its module contracts names no
        // aggregate in its namespace, and a rule exception beside an enforcing
        // aggregate names that aggregate instead of the rule home.
        var significant = placed.Where((segment, index) => index == 0 || !PlacelessSegments.Contains(segment)).ToList();

        if (record.Type is RecordTypes.DomainEvent or RecordTypes.Rule
            && significant.Count >= 1
            && SameName(significant[0], path.Module!)
            && (IsInModuleContracts(model.Config, element.File, path.Module!)
                || (record.Type == RecordTypes.Rule
                    && ruleHomes is not null
                    && significant.Count >= 2
                    && ruleHomes.Any(home => SameAggregate(significant[1], home)))))
        {
            return;
        }

        if (!NamespaceMatches(placed, expected))
        {
            findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but '{mark.RecordId}' places it under '{product}.{string.Join(".", expected)}'.", mark.File, mark.Line, mark.RecordId));
            return;
        }

        CheckNamespaceLayer(model, element, mark, placed, actual, findings);
    }

    /// <summary>Reports the backend namespace that names another layer than the file's top folder in its module: a file moved to another layer keeps its stale namespace. A file under module contracts, or directly in its module with no layer folder, states no layer, so it is left out.</summary>
    /// <param name="model">The loaded project model whose configuration names the module roots.</param>
    /// <param name="element">The marked code element whose namespace is checked.</param>
    /// <param name="mark">The mark naming the record the namespace should place.</param>
    /// <param name="placed">The declared namespace segments after the product.</param>
    /// <param name="actual">The declared namespace as written in the file.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckNamespaceLayer(ProjectModel model, MarkedElement element, CodeMark mark, List<string> placed, string actual, List<Finding> findings)
    {
        if (RawModulePath(model.Config, element.File) is not { } raw || raw.Length < 3)
        {
            return;
        }

        var folder = raw[1];

        if (!NamespaceLayers.Contains(folder) || IsInModuleContracts(model.Config, element.File, raw[0]))
        {
            return;
        }

        if (placed.Count >= 2 && SameName(placed[1], folder))
        {
            return;
        }

        findings.Add(new Finding("code-placement-drift", $"{KindName(element.Kind)} '{DisplayName(element)}' declares namespace '{actual}', but its file sits in '{folder}/', which the namespace does not name.", mark.File, mark.Line, mark.RecordId));
    }

    /// <summary>Whether the endpoint sits in a host, where host endpoints carry their mark without a use case folder.</summary>
    /// <param name="config">The project configuration naming the host roots.</param>
    /// <param name="file">The full path of the endpoint file.</param>
    /// <returns>True when the file sits under a host root.</returns>
    private static bool IsHostEndpoint(ConsumerConfig config, string file) =>
        SourceRoots.IsUnderHost(config, file);

    /// <summary>
    ///     Whether the element carries a backend placement: not a test, a frontend element, an
    ///     aggregate method, a technical class or a host endpoint. A technical class that carries a
    ///     read model mark is the one technical class with a placement, its module ReadModels folder.
    /// </summary>
    /// <param name="config">The project configuration naming the host roots.</param>
    /// <param name="element">The marked code element.</param>
    /// <returns>True when the element's folder and namespace follow its record.</returns>
    private static bool HasBackendPlacement(ConsumerConfig config, MarkedElement element) =>
        !element.InTests
        && (element.Kind is not (CodeKind.Test or CodeKind.BrowserTest
                or CodeKind.FrontendModule or CodeKind.FrontendRoute
                or CodeKind.AggregateMethod or CodeKind.Technical)
            || element.Marks.Any(mark => mark.RecordId.StartsWith("read-model/", StringComparison.Ordinal)))
        && !(element.Kind == CodeKind.Endpoint && IsHostEndpoint(config, element.File));
}
