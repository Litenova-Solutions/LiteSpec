using System.Text.Json;
using System.Xml.Linq;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Implementation;

/// <summary>
///     The locality checks, kept in one class so the partial checker holds only the entry point.
/// </summary>
/// <follows>litespec/core/model#connect-across-modules-only-through-declared-dependencies</follows>
/// <follows>litespec/core/model#keep-module-dependencies-free-of-cycles</follows>
/// <follows>litespec/core/model#publish-each-integration-event-from-one-module</follows>
internal static class LocalityChecks
{
    /// <summary>
    ///     Runs link-locality-invalid, module-dependency-invalid, event-publisher-invalid and external-system-placement-wide.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckLinks(model, findings);
        CheckCycles(model, findings);
        CheckPublishers(model, findings);
        CheckExternalSystemPlacement(model, findings);
    }

    /// <summary>
    ///     Reports a product-level external system that one module alone calls through its effects. It sits at the lowest node
    ///     holding every record that calls it, which is that module.
    /// </summary>
    /// <param name="model">The loaded project model that holds every external system and effect holder.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExternalSystemPlacement(ProjectModel model, List<Finding> findings)
    {
        var callers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            foreach (var entry in RecordEntries.EffectEntries(holder))
            {
                foreach (var verb in new[] { RecordFields.Calls, RecordFields.Sends })
                {
                    if (RecordEntries.Str(entry, verb) is not { Length: > 0 } target
                        || !model.ById.TryGetValue(Identifiers.BaseId(target), out var system)
                        || system.Type != RecordTypes.ExternalSystem)
                    {
                        continue;
                    }

                    if (!callers.TryGetValue(system.Id, out var modules))
                    {
                        modules = new HashSet<string>(StringComparer.Ordinal);
                        callers[system.Id] = modules;
                    }

                    modules.Add(holder.Module ?? "");
                }
            }
        }

        foreach (var system in model.Records.Where(record => record.Type == RecordTypes.ExternalSystem && record.Module is null).OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (callers.TryGetValue(system.Id, out var modules) && modules.Count == 1 && modules.Single() is { Length: > 0 } only)
            {
                findings.Add(new Finding("external-system-placement-wide", $"External system '{system.Id}' sits at product level, and module '{only}' alone calls it; place it in that module.", system.File, system.Line, SubjectOf(system)));
            }
        }
    }

    /// <summary>Reports a same-layer link across modules with no declared dependency and no module event or integration event between them.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckLinks(ProjectModel model, List<Finding> findings)
    {
        var depends = DependsByModule(model);

        foreach (var source in model.Records.OrderBy(record => record.Id, StringComparer.Ordinal))
        {
            if (source.Derived)
            {
                continue;
            }

            var sourceModule = ModuleOf(model, source);

            if (sourceModule is not { Length: > 0 })
            {
                continue;
            }

            if (source.Raw.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var sourceLayer = LayerOf(source);

            foreach (var property in source.Raw.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal))
            {
                if (!RecordTypes.LinkTargets.ContainsKey(property.Name)
                    || property.Name == RecordFields.DependsOn)
                {
                    continue;
                }

                var targets = ModelBuilder.TargetsOf(property.Value)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
                    .ToList();

                foreach (var targetId in targets)
                {
                    var baseId = Identifiers.BaseId(targetId);

                    if (!model.ById.TryGetValue(baseId, out var target))
                    {
                        continue;
                    }

                    var targetModule = ModuleOf(model, target);

                    if (targetModule is not { Length: > 0 }
                        || targetModule.Equals(sourceModule, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!LayerOf(target).Equals(sourceLayer, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (source.Type is RecordTypes.ModuleEvent or RecordTypes.IntegrationEvent
                        || target.Type is RecordTypes.ModuleEvent or RecordTypes.IntegrationEvent)
                    {
                        continue;
                    }

                    // A public query lists its consumer modules in reachedThrough; the
                    // consumer's queries edge and dependsOn carry the forward dependency,
                    // so the back-link needs none.
                    if (property.Name == RecordFields.ReachedThrough
                        && target.Type == RecordTypes.Module)
                    {
                        continue;
                    }

                    if (depends.TryGetValue(sourceModule, out var edges)
                        && edges.Contains(targetModule))
                    {
                        continue;
                    }

                    if (source.Type == RecordTypes.Rule && property.Name == RecordFields.Governs)
                    {
                        findings.Add(new Finding(
                            "link-locality-invalid",
                            $"Rule '{source.Id}' in module '{sourceModule}' governs '{targetId}' in module '{targetModule}' with no declared dependency. State the check as its own rule in module '{targetModule}' at its boundary and name this rule in prose; module '{sourceModule}' checks again when it acts.",
                            source.File,
                            source.Line,
                            SubjectOf(source)));
                        continue;
                    }

                    findings.Add(new Finding(
                        "link-locality-invalid",
                        $"{TypeLabel(source.Type)} '{source.Id}' in module '{sourceModule}' links to '{targetId}' in module '{targetModule}' through '{property.Name}' with no declared dependency.",
                        source.File,
                        source.Line,
                        SubjectOf(source)));
                }
            }
        }
    }

    /// <summary>Reports a module dependency cycle, as the effect-cycle rule does for edges.</summary>
    /// <param name="model">The loaded project model that holds every module record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCycles(ProjectModel model, List<Finding> findings)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var edges = new Dictionary<(string From, string To), HashSet<string>>();

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Module))
        {
            graph[record.Id] = [];
        }

        AddDependsOnEdges(model, graph, edges);
        AddContractsReferenceEdges(model, graph, edges);
        AddQueriesEdges(model, graph, edges);
        AddTriggerEdges(model, graph, edges);

        foreach (var targets in graph.Values)
        {
            targets.Sort(StringComparer.Ordinal);
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var start in graph.Keys.Order(StringComparer.Ordinal))
        {
            var path = new List<string> { start };
            var inPath = new HashSet<string>(StringComparer.Ordinal) { start };

            WalkCycle(model, graph, edges, start, path, inPath, reported, findings);
        }
    }

    /// <summary>Adds one edge per declared module dependency.</summary>
    /// <param name="model">The loaded project model that holds every module record.</param>
    /// <param name="graph">The module graph by module identifier, receiving each target.</param>
    /// <param name="edges">The edge labels by hop, receiving each label.</param>
    private static void AddDependsOnEdges(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Module))
        {
            foreach (var depended in record.Ids(RecordFields.DependsOn))
            {
                var baseId = Identifiers.BaseId(depended);

                if (!model.ById.TryGetValue(baseId, out var resolved)
                    || resolved.Type != RecordTypes.Module)
                {
                    continue;
                }

                AddEdge(graph, edges, record.Id, baseId, $"'{record.Id}' declares 'dependsOn' '{baseId}'");
            }
        }
    }

    /// <summary>Adds one edge per Contracts project reference to another module's Contracts project.</summary>
    /// <param name="model">The loaded project model that holds every module record and the module roots.</param>
    /// <param name="graph">The module graph by module identifier, receiving each target.</param>
    /// <param name="edges">The edge labels by hop, receiving each label.</param>
    private static void AddContractsReferenceEdges(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges)
    {
        foreach (var root in model.Config.ModulesRoots)
        {
            if (!Readers.Dotnet.SourceRoots.IsContractsProject(root)
                || Readers.Dotnet.SourceRoots.ModuleNameOf(root) is not { Length: > 0 } owner
                || ModuleIdOf(model, owner) is not { } from)
            {
                continue;
            }

            foreach (var referenced in ContractsReferences(root))
            {
                if (ModuleIdOf(model, referenced) is not { } to)
                {
                    continue;
                }

                AddEdge(graph, edges, from, to, $"'{from}' references the Contracts project of '{to}'");
            }
        }
    }

    /// <summary>The module identifier a folder name states: the declared module it spells with case, hyphens and underscores ignored.</summary>
    /// <param name="model">The loaded project model that holds every module record.</param>
    /// <param name="name">The folder name as written on disk.</param>
    /// <returns>The module identifier, or null when no declared module spells it.</returns>
    private static string? ModuleIdOf(ProjectModel model, string name)
    {
        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Module)
            {
                continue;
            }

            if (Identifiers.TrySplit(record.Id, out _, out _, out var idName)
                && Placement.PlacementPath.SameName(idName, name))
            {
                return record.Id;
            }
        }

        return null;
    }

    /// <summary>The modules whose Contracts project the Contracts project references.</summary>
    /// <param name="contracts">The Contracts project root.</param>
    /// <returns>Each owner module name of a referenced Contracts project, once.</returns>
    private static HashSet<string> ContractsReferences(string contracts)
    {
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        List<string> projects;

        try
        {
            if (!Directory.Exists(contracts))
            {
                return referenced;
            }

            projects = Directory.EnumerateFiles(contracts, "*.csproj", SearchOption.TopDirectoryOnly)
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return referenced;
        }

        foreach (var project in projects)
        {
            System.Xml.Linq.XDocument document;

            try
            {
                document = System.Xml.Linq.XDocument.Load(project);
            }
            catch (Exception exception) when (exception is System.Xml.XmlException or IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var folder = Path.GetDirectoryName(project) ?? project;

            foreach (var element in document.Descendants())
            {
                if (!string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.Ordinal))
                {
                    continue;
                }

                var include = element.Attribute("Include")?.Value;

                if (include is not { Length: > 0 })
                {
                    continue;
                }

                string? target;

                try
                {
                    target = Path.GetFileName(Path.GetDirectoryName(
                        Path.GetFullPath(Path.Combine(folder, include.Replace('\\', Path.DirectorySeparatorChar)))));
                }
                catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    continue;
                }

                if (target is { Length: > 0 }
                    && Readers.Dotnet.SourceRoots.IsContractsProject(Path.Combine(folder, target))
                    && Readers.Dotnet.SourceRoots.ModuleNameOf(Path.Combine(folder, target)) is { Length: > 0 } module)
                {
                    referenced.Add(module);
                }
            }
        }

        return referenced;
    }

    /// <summary>Adds one edge per queries edge from a record in one module to a use case in another.</summary>
    /// <param name="model">The loaded project model that holds every use case, reaction and job.</param>
    /// <param name="graph">The module graph by module identifier, receiving each target.</param>
    /// <param name="edges">The edge labels by hop, receiving each label.</param>
    private static void AddQueriesEdges(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges)
    {
        foreach (var holder in model.Records.Where(record =>
            !record.Derived && record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            if (ModuleOf(model, holder) is not { Length: > 0 } from
                || ModuleIdOf(model, from) is not { } source)
            {
                continue;
            }

            foreach (var target in holder.EdgeIds(RecordFields.Queries))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved)
                    || resolved.Type != RecordTypes.UseCase
                    || ModuleOf(model, resolved) is not { Length: > 0 } to
                    || to.Equals(from, StringComparison.Ordinal)
                    || ModuleIdOf(model, to) is not { } provider)
                {
                    continue;
                }

                AddEdge(graph, edges, source, provider, $"'{source}' queries '{resolved.Id}'");
            }
        }
    }

    /// <summary>Adds one edge per reaction triggered by a fact another module owns.</summary>
    /// <param name="model">The loaded project model that holds every reaction and published fact.</param>
    /// <param name="graph">The module graph by module identifier, receiving each target.</param>
    /// <param name="edges">The edge labels by hop, receiving each label.</param>
    private static void AddTriggerEdges(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges)
    {
        var publishers = PublishersByEvent(model);

        foreach (var reaction in model.Records.Where(record => !record.Derived && record.Type == RecordTypes.Reaction))
        {
            if (ModuleOf(model, reaction) is not { Length: > 0 } from
                || ModuleIdOf(model, from) is not { } source)
            {
                continue;
            }

            foreach (var trigger in reaction.Ids(RecordFields.TriggeredBy))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(trigger), out var fact)
                    || fact.Type is not (RecordTypes.ModuleEvent or RecordTypes.IntegrationEvent))
                {
                    continue;
                }

                foreach (var owner in OwnerModulesOf(model, fact, publishers))
                {
                    if (owner.Equals(from, StringComparison.Ordinal)
                        || ModuleIdOf(model, owner) is not { } provider)
                    {
                        continue;
                    }

                    AddEdge(graph, edges, source, provider, $"'{source}' reacts to '{fact.Id}'");
                }
            }
        }
    }

    /// <summary>The modules owning a published fact: its module folder, else its publishing modules.</summary>
    /// <param name="model">The loaded project model that resolves the fact's module folder.</param>
    /// <param name="fact">The module event or integration event whose owners are wanted.</param>
    /// <param name="publishers">The publishing modules by published event identifier.</param>
    /// <returns>Each owner module name, once.</returns>
    private static HashSet<string> OwnerModulesOf(
        ProjectModel model, Record fact, Dictionary<string, HashSet<string>> publishers)
    {
        if (ModuleOf(model, fact) is { Length: > 0 } folder)
        {
            return [folder];
        }

        return publishers.TryGetValue(fact.Id, out var owned)
            ? new HashSet<string>(owned, StringComparer.Ordinal)
            : [];
    }

    /// <summary>Adds one labeled hop to the module graph.</summary>
    /// <param name="graph">The module graph by module identifier, receiving the target.</param>
    /// <param name="edges">The edge labels by hop, receiving the label.</param>
    /// <param name="from">The source module identifier.</param>
    /// <param name="to">The target module identifier.</param>
    /// <param name="label">The edge label naming the hop.</param>
    private static void AddEdge(
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges,
        string from,
        string to,
        string label)
    {
        if (!graph.TryGetValue(from, out var targets))
        {
            targets = [];
            graph[from] = targets;
        }

        if (!targets.Contains(to, StringComparer.Ordinal))
        {
            targets.Add(to);
        }

        if (!edges.TryGetValue((from, to), out var labels))
        {
            labels = new HashSet<string>(StringComparer.Ordinal);
            edges[(from, to)] = labels;
        }

        labels.Add(label);
    }

    /// <summary>Walks the module graph depth-first from one node, reporting each loop back to the start.</summary>
    /// <param name="model">The loaded project model that resolves each module in a found cycle.</param>
    /// <param name="graph">The module graph by module identifier.</param>
    /// <param name="edges">The edge labels by hop.</param>
    /// <param name="current">The node the walk stands on.</param>
    /// <param name="path">The nodes on the current walk, starting with the start node.</param>
    /// <param name="inPath">The nodes on the current walk, for constant-time membership.</param>
    /// <param name="reported">The cycle keys already reported, so each cycle reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void WalkCycle(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
        Dictionary<(string From, string To), HashSet<string>> edges,
        string current,
        List<string> path,
        HashSet<string> inPath,
        HashSet<string> reported,
        List<Finding> findings)
    {
        if (!graph.TryGetValue(current, out var targets))
        {
            return;
        }

        foreach (var next in targets)
        {
            if (next == path[0] && path.Count >= 1)
            {
                ReportCycle(model, path, edges, reported, findings);
            }
            else if (inPath.Add(next))
            {
                path.Add(next);
                WalkCycle(model, graph, edges, next, path, inPath, reported, findings);
                path.RemoveAt(path.Count - 1);
                inPath.Remove(next);
            }
        }
    }

    /// <summary>Reports one found module cycle once, in canonical order starting at the smallest identifier, naming the edges that close it.</summary>
    /// <param name="model">The loaded project model that resolves the module holding the cycle.</param>
    /// <param name="path">The nodes on the walk that closes the cycle.</param>
    /// <param name="edges">The edge labels by hop.</param>
    /// <param name="reported">The cycle keys already reported, so each cycle reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportCycle(
        ProjectModel model,
        List<string> path,
        Dictionary<(string From, string To), HashSet<string>> edges,
        HashSet<string> reported,
        List<Finding> findings)
    {
        var turn = path.IndexOf(path.Min(StringComparer.Ordinal)!);
        var rotated = path.Skip(turn).Concat(path.Take(turn)).ToList();
        var key = string.Join(" -> ", rotated);

        if (!reported.Add(key))
        {
            return;
        }

        var holder = model.ById[rotated[0]];
        var hops = new List<string>();

        for (var at = 0; at < rotated.Count; at++)
        {
            var from = rotated[at];
            var to = rotated[(at + 1) % rotated.Count];

            if (edges.TryGetValue((from, to), out var labels))
            {
                hops.AddRange(labels.Order(StringComparer.Ordinal));
            }
        }

        findings.Add(new Finding(
            "module-dependency-invalid",
            $"Module '{holder.Id}' holds a dependency cycle: {key} -> {rotated[0]}. The edges are: {string.Join("; ", hops)}.",
            holder.File,
            holder.Line,
            SubjectOf(holder)));
    }

    /// <summary>
    ///     Reports a module event or integration event published from more than one module, and a module event
    ///     published from a module other than its own. A use case publishes each module event and each outbound
    ///     integration event that translates an event the use case raises, so the publishing modules are the
    ///     modules of those use cases.
    /// </summary>
    /// <param name="model">The loaded project model that holds every module event, integration event and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates</follows>
    private static void CheckPublishers(ProjectModel model, List<Finding> findings)
    {
        var publishers = PublishersByEvent(model);

        foreach (var eventId in publishers.Keys.Order(StringComparer.Ordinal))
        {
            var modules = publishers[eventId].Order(StringComparer.Ordinal).ToList();
            var holder = model.ById[eventId];
            var label = holder.Type == RecordTypes.ModuleEvent ? "Module event" : "Integration event";

            if (modules.Count > 1)
            {
                var listed = string.Join(", ", modules.Select(module => $"'module/{module}'"));

                findings.Add(new Finding(
                    "event-publisher-invalid",
                    $"{label} '{holder.Id}' is published from {modules.Count} modules: {listed}.",
                    holder.File,
                    holder.Line,
                    SubjectOf(holder)));

                continue;
            }

            if (holder.Type == RecordTypes.ModuleEvent && modules.Count == 1
                && Identifiers.TrySplit(holder.Id, out _, out var home, out _)
                && home is { Length: > 0 }
                && modules[0] != home)
            {
                findings.Add(new Finding(
                    "event-publisher-invalid",
                    $"Module event '{holder.Id}' is published from module '{modules[0]}' instead of its own module '{home}'.",
                    holder.File,
                    holder.Line,
                    SubjectOf(holder)));
            }
        }
    }

    /// <summary>The publishing modules by published event identifier: the modules of the use cases raising the domain event the fact translates.</summary>
    /// <param name="model">The loaded project model that holds every module event, integration event and use case.</param>
    /// <returns>Each published event identifier with its publishing module names.</returns>
    private static Dictionary<string, HashSet<string>> PublishersByEvent(ProjectModel model)
    {
        var publishers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var translators = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var published in model.Records.Where(record => record.Type is RecordTypes.ModuleEvent or RecordTypes.IntegrationEvent))
        {
            foreach (var translated in published.Ids(RecordFields.Translates))
            {
                var eventId = Identifiers.BaseId(translated);

                if (!translators.TryGetValue(eventId, out var events))
                {
                    events = [];
                    translators[eventId] = events;
                }

                events.Add(published.Id);
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            var module = ModuleOf(model, useCase);

            if (module is not { Length: > 0 })
            {
                continue;
            }

            foreach (var raised in useCase.Ids(RecordFields.Raises))
            {
                if (!translators.TryGetValue(Identifiers.BaseId(raised), out var events))
                {
                    continue;
                }

                foreach (var publishedId in events)
                {
                    if (!publishers.TryGetValue(publishedId, out var set))
                    {
                        set = new HashSet<string>(StringComparer.Ordinal);
                        publishers[publishedId] = set;
                    }

                    set.Add(module);
                }
            }
        }

        return publishers;
    }

    /// <summary>The declared module dependencies by module name.</summary>
    /// <param name="model">The loaded project model that holds every module record.</param>
    /// <returns>Each module name with the module names its dependsOn declares.</returns>
    private static Dictionary<string, HashSet<string>> DependsByModule(ProjectModel model)
    {
        var depends = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Module))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name.Length == 0)
            {
                continue;
            }

            var edges = new HashSet<string>(StringComparer.Ordinal);

            foreach (var depended in record.Ids(RecordFields.DependsOn))
            {
                var baseId = Identifiers.BaseId(depended);

                if (Identifiers.TrySplit(baseId, out var prefix, out _, out var target)
                    && prefix == RecordTypes.Module
                    && target.Length > 0)
                {
                    edges.Add(target);
                }
                else if (baseId.StartsWith("module/", StringComparison.Ordinal)
                    && baseId.Length > "module/".Length)
                {
                    edges.Add(baseId["module/".Length..]);
                }
            }

            depends[name] = edges;
        }

        return depends;
    }

    /// <summary>The module holding the record: its modules folder, else its identifier home.</summary>
    /// <param name="model">The loaded project model that resolves the module and parent records.</param>
    /// <param name="record">The record whose module is wanted.</param>
    /// <returns>The module name, or null when the record sits at product level.</returns>
    internal static string? ModuleOf(ProjectModel model, Record record)
    {
        var segments = record.File.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var at = 0; at + 1 < segments.Length; at++)
        {
            if (segments[at].Equals("modules", StringComparison.Ordinal))
            {
                return segments[at + 1];
            }
        }

        if (record.Type == RecordTypes.Module
            && Identifiers.TrySplit(record.Id, out _, out _, out var own)
            && own.Length > 0)
        {
            return own;
        }

        if (!Identifiers.TrySplit(record.Id, out _, out var home, out _)
            || home is not { Length: > 0 })
        {
            return null;
        }

        if (record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job
            or RecordTypes.Notification or RecordTypes.Setting or RecordTypes.Aggregate)
        {
            return model.ById.ContainsKey($"module/{home}") ? home : null;
        }

        if (record.Type is RecordTypes.Rule or RecordTypes.DomainEvent)
        {
            if (record.Type == RecordTypes.Rule && model.ById.ContainsKey($"module/{home}"))
            {
                return home;
            }

            var aggregate = model.Records.FirstOrDefault(candidate =>
                candidate.Type == RecordTypes.Aggregate
                && Identifiers.TrySplit(candidate.Id, out _, out _, out var aggregateName)
                && aggregateName.Equals(home, StringComparison.Ordinal));

            return aggregate is null ? null : ModuleOf(model, aggregate);
        }

        if (record.Type is RecordTypes.Result or RecordTypes.Scenario)
        {
            var useCase = ModelBuilder.UseCaseOf(model, home);

            return useCase is null ? null : ModuleOf(model, useCase);
        }

        if (record.Type == RecordTypes.Requirement
            && model.ById.TryGetValue($"feature/{home}", out var feature))
        {
            return ModuleOf(model, feature);
        }

        return null;
    }

    /// <summary>The record layer the locality rule reads: root, intent, behavior, implementation or governance.</summary>
    /// <param name="record">The record whose layer is wanted.</param>
    /// <returns>The layer name in lowercase.</returns>
    private static string LayerOf(Record record)
    {
        if (record.Type == RecordTypes.Product)
        {
            return "root";
        }

        if (record.Type == RecordTypes.Term)
        {
            return record.Field(RecordFields.Layer) == "behavior" ? "behavior" : "intent";
        }

        if (record.Type == RecordTypes.Runbook)
        {
            return "implementation";
        }

        if (record.Type == RecordTypes.Waiver)
        {
            return "governance";
        }

        return record.Type switch
        {
            RecordTypes.Goal or RecordTypes.Direction or RecordTypes.Problem or RecordTypes.Feature
                or RecordTypes.Constraint or RecordTypes.Research or RecordTypes.Decision
                or RecordTypes.Requirement or RecordTypes.Flow or RecordTypes.Actor
                or RecordTypes.Role => "intent",
            _ => "behavior",
        };
    }

    /// <summary>The prose label a finding shows for a record type.</summary>
    /// <param name="type">The record type identifier.</param>
    /// <returns>The capitalized prose label with hyphens as spaces.</returns>
    private static string TypeLabel(string type)
    {
        var words = type.Split('-');
        var first = char.ToUpperInvariant(words[0][0]) + words[0][1..];

        return words.Length == 1 ? first : first + " " + string.Join(" ", words[1..]);
    }
}
