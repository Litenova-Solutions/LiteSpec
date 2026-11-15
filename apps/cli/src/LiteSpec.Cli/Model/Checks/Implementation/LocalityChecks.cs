using System.Text.Json;
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

    /// <summary>Reports a same-layer link across modules with no declared dependency and no integration event between them.</summary>
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

                    if (source.Type == RecordTypes.IntegrationEvent
                        || target.Type == RecordTypes.IntegrationEvent)
                    {
                        continue;
                    }

                    if (depends.TryGetValue(sourceModule, out var edges)
                        && edges.Contains(targetModule))
                    {
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

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Module))
        {
            var targets = new List<string>();

            foreach (var depended in record.Ids(RecordFields.DependsOn))
            {
                var baseId = Identifiers.BaseId(depended);

                if (!model.ById.TryGetValue(baseId, out var resolved)
                    || resolved.Type != RecordTypes.Module)
                {
                    continue;
                }

                if (!targets.Contains(baseId, StringComparer.Ordinal))
                {
                    targets.Add(baseId);
                }
            }

            targets.Sort(StringComparer.Ordinal);
            graph[record.Id] = targets;
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var start in graph.Keys.Order(StringComparer.Ordinal))
        {
            var path = new List<string> { start };
            var inPath = new HashSet<string>(StringComparer.Ordinal) { start };

            WalkCycle(model, graph, start, path, inPath, reported, findings);
        }
    }

    /// <summary>Walks the module graph depth-first from one node, reporting each loop back to the start.</summary>
    /// <param name="model">The loaded project model that resolves each module in a found cycle.</param>
    /// <param name="graph">The module graph by module identifier.</param>
    /// <param name="current">The node the walk stands on.</param>
    /// <param name="path">The nodes on the current walk, starting with the start node.</param>
    /// <param name="inPath">The nodes on the current walk, for constant-time membership.</param>
    /// <param name="reported">The cycle keys already reported, so each cycle reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void WalkCycle(
        ProjectModel model,
        Dictionary<string, List<string>> graph,
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
                ReportCycle(model, path, reported, findings);
            }
            else if (inPath.Add(next))
            {
                path.Add(next);
                WalkCycle(model, graph, next, path, inPath, reported, findings);
                path.RemoveAt(path.Count - 1);
                inPath.Remove(next);
            }
        }
    }

    /// <summary>Reports one found module cycle once, in canonical order starting at the smallest identifier.</summary>
    /// <param name="model">The loaded project model that resolves the module holding the cycle.</param>
    /// <param name="path">The nodes on the walk that closes the cycle.</param>
    /// <param name="reported">The cycle keys already reported, so each cycle reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportCycle(
        ProjectModel model, List<string> path, HashSet<string> reported, List<Finding> findings)
    {
        var turn = path.IndexOf(path.Min(StringComparer.Ordinal)!);
        var rotated = path.Skip(turn).Concat(path.Take(turn)).ToList();
        var key = string.Join(" -> ", rotated);

        if (!reported.Add(key))
        {
            return;
        }

        var holder = model.ById[rotated[0]];

        findings.Add(new Finding(
            "module-dependency-invalid",
            $"Module '{holder.Id}' holds a dependency cycle: {key} -> {rotated[0]}.",
            holder.File,
            holder.Line,
            SubjectOf(holder)));
    }

    /// <summary>
    ///     Reports an integration event published from more than one module. A use case publishes each
    ///     outbound integration event that translates an event the use case raises, so the publishing
    ///     modules are the modules of those use cases.
    /// </summary>
    /// <param name="model">The loaded project model that holds every integration event and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates</follows>
    private static void CheckPublishers(ProjectModel model, List<Finding> findings)
    {
        var publishers = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var translators = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var integration in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent))
        {
            foreach (var translated in integration.Ids(RecordFields.Translates))
            {
                var eventId = Identifiers.BaseId(translated);

                if (!translators.TryGetValue(eventId, out var integrations))
                {
                    integrations = [];
                    translators[eventId] = integrations;
                }

                integrations.Add(integration.Id);
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
                if (!translators.TryGetValue(Identifiers.BaseId(raised), out var integrations))
                {
                    continue;
                }

                foreach (var integrationId in integrations)
                {
                    if (!publishers.TryGetValue(integrationId, out var set))
                    {
                        set = new HashSet<string>(StringComparer.Ordinal);
                        publishers[integrationId] = set;
                    }

                    set.Add(module);
                }
            }
        }

        foreach (var eventId in publishers.Keys.Order(StringComparer.Ordinal))
        {
            var modules = publishers[eventId].Order(StringComparer.Ordinal).ToList();

            if (modules.Count <= 1)
            {
                continue;
            }

            var holder = model.ById[eventId];
            var listed = string.Join(", ", modules.Select(module => $"'module/{module}'"));

            findings.Add(new Finding(
                "event-publisher-invalid",
                $"Integration event '{holder.Id}' is published from {modules.Count} modules: {listed}.",
                holder.File,
                holder.Line,
                SubjectOf(holder)));
        }
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
    private static string? ModuleOf(ProjectModel model, Record record)
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

        if (record.Type == RecordTypes.Departure)
        {
            return "governance";
        }

        return record.Type switch
        {
            RecordTypes.Goal or RecordTypes.Vision or RecordTypes.Problem or RecordTypes.Feature
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
