using System.Text.Json;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The effect-loop check: the effect graph that use cases, reactions, jobs and events declare holds no loop without a stated end.</summary>
/// <follows>litespec/core/use-cases#keep-the-effect-graph-free-of-cycles</follows>
/// <follows>litespec/core/principles#end-every-effect-cycle-in-a-domain-event</follows>
internal static class EffectLoopChecks
{
    /// <summary>Reports an effect loop with no stated end.</summary>
    /// <param name="model">The loaded project model that holds every record and its effect edges.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var graph = EffectGraph(model);
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var start in graph.Keys.Order(StringComparer.Ordinal))
        {
            var path = new List<string> { start };
            var inPath = new HashSet<string>(StringComparer.Ordinal) { start };

            WalkLoop(model, graph, start, path, inPath, reported, findings);
        }
    }

    /// <summary>
    ///     The declared effect edges: use-case effects and raises, reaction triggers
    ///     reversed into event edges with the reaction effects, and job effects.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and its effect edges.</param>
    /// <returns>The effect graph by source identifier, with each target list in ordinal order.</returns>
    private static Dictionary<string, List<string>> EffectGraph(ProjectModel model)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Edge(string from, string to)
        {
            var baseId = Identifiers.BaseId(to);

            if (!model.ById.ContainsKey(baseId))
            {
                return;
            }

            if (!graph.TryGetValue(from, out var targets))
            {
                targets = [];
                graph[from] = targets;
            }

            if (!targets.Contains(baseId, StringComparer.Ordinal))
            {
                targets.Add(baseId);
            }
        }

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.UseCase)
            {
                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }

                foreach (var raised in record.Ids(RecordFields.Raises))
                {
                    Edge(record.Id, raised);
                }
            }
            else if (record.Type == RecordTypes.Reaction)
            {
                foreach (var trigger in ReactionEventTriggers(record))
                {
                    var baseId = Identifiers.BaseId(trigger);

                    if (model.ById.TryGetValue(baseId, out var resolved)
                        && resolved.Type is RecordTypes.Event or RecordTypes.IntegrationEvent)
                    {
                        Edge(baseId, record.Id);
                    }
                }

                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }
            }
            else if (record.Type == RecordTypes.Job)
            {
                foreach (var entry in EffectEntries(record))
                {
                    if (EffectTarget(entry) is { } target)
                    {
                        Edge(record.Id, target);
                    }
                }
            }
        }

        foreach (var targets in graph.Values)
        {
            targets.Sort(StringComparer.Ordinal);
        }

        return graph;
    }

    /// <summary>Walks the effect graph depth-first from one node, reporting each loop back to the start.</summary>
    /// <param name="model">The loaded project model that resolves each record in a found loop.</param>
    /// <param name="graph">The effect graph by source identifier.</param>
    /// <param name="current">The node the walk stands on.</param>
    /// <param name="path">The nodes on the current walk, starting with the start node.</param>
    /// <param name="inPath">The nodes on the current walk, for constant-time membership.</param>
    /// <param name="reported">The loop keys already reported, so each loop reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void WalkLoop(
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
                ReportLoop(model, path, reported, findings);
            }
            else if (inPath.Add(next))
            {
                path.Add(next);
                WalkLoop(model, graph, next, path, inPath, reported, findings);
                path.RemoveAt(path.Count - 1);
                inPath.Remove(next);
            }
        }
    }

    /// <summary>Reports one found effect loop once, unless a domain event and a skipping reaction exempt it.</summary>
    /// <param name="model">The loaded project model that resolves each record in the loop.</param>
    /// <param name="path">The nodes on the walk that closes the loop.</param>
    /// <param name="reported">The loop keys already reported, so each loop reports once.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void ReportLoop(
        ProjectModel model, List<string> path, HashSet<string> reported, List<Finding> findings)
    {
        var turn = path.IndexOf(path.Min(StringComparer.Ordinal)!);
        var rotated = path.Skip(turn).Concat(path.Take(turn)).ToList();
        var key = string.Join(" -> ", rotated);

        if (!reported.Add(key) || LoopExempt(model, rotated))
        {
            return;
        }

        var holder = rotated
            .Select(id => model.ById[id])
            .First(record => record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job);

        findings.Add(new Finding("effect-loop-invalid", $"{Label(holder.Type)} '{holder.Id}' holds an effect loop with no stated end: {key} -> {rotated[0]}.", holder.File, holder.Line, holder.Id));
    }

    /// <summary>
    ///     Whether the loop is allowed: it passes through a domain event, and a
    ///     reaction in the loop states its skip condition as the end condition.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each record in the loop.</param>
    /// <param name="rotated">The loop nodes in canonical order, starting at the smallest identifier.</param>
    /// <returns>True when the loop passes through a domain event and a reaction that states its skip condition; otherwise false.</returns>
    private static bool LoopExempt(ProjectModel model, IReadOnlyList<string> rotated) =>
        rotated.Any(id => model.ById.TryGetValue(id, out var record) && record.Type == RecordTypes.Event)
        && rotated.Any(id => model.ById.TryGetValue(id, out var record)
            && record.Type == RecordTypes.Reaction
            && record.Field(RecordFields.SkippedWhen) is { Length: > 0 });

    /// <summary>The one target an effect entry names, across its four verbs.</summary>
    /// <param name="entry">The effect entry whose target is read.</param>
    /// <returns>The first non-empty target across the four edge verbs, or null when the entry names none.</returns>
    private static string? EffectTarget(JsonElement entry) =>
        (Str(entry, RecordFields.Runs) ?? Str(entry, RecordFields.Sends) ?? Str(entry, RecordFields.Emits) ?? Str(entry, RecordFields.Calls)) is { Length: > 0 } target
            ? target
            : null;
}
