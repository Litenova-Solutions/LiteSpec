using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model;

/// <summary>
///     The module edges report: per module, the written record links to other modules, and every
///     pair of modules that link each other both ways. Read-only, from the built model.
/// </summary>
/// <follows>litespec/core/model#connect-across-modules-only-through-declared-dependencies</follows>
static class ModelEdges
{
    /// <summary>One link from a record in one module to a record in another module.</summary>
    /// <param name="From">The module holding the record the link starts from.</param>
    /// <param name="Kind">The link kind: its field, or its verb for an effect entry.</param>
    /// <param name="To">The module holding the record the link points at.</param>
    sealed record Edge(string From, string Kind, string To);

    /// <summary>
    ///     The report lines: one block per module with its outgoing and incoming links to other
    ///     modules, each with counts per link kind and per other module, then the mutual pairs.
    /// </summary>
    /// <param name="model">The built project model to report from.</param>
    /// <param name="module">The module to report, or null for every module.</param>
    /// <returns>The report lines in print order.</returns>
    public static IReadOnlyList<string> Lines(ProjectModel model, string? module)
    {
        var edges = CrossModuleEdges(model);
        var modules = Modules(model)
            .Where(name => module is null || name.Equals(module, StringComparison.Ordinal))
            .ToList();
        var lines = new List<string>();

        foreach (var name in modules)
        {
            var outgoing = edges.Where(edge => edge.From.Equals(name, StringComparison.Ordinal)).ToList();
            var incoming = edges.Where(edge => edge.To.Equals(name, StringComparison.Ordinal)).ToList();

            lines.Add($"Module {name}:");
            lines.Add($"  Outgoing: {outgoing.Count} links to {Others(outgoing.Select(edge => edge.To))} modules");

            foreach (var group in KindGroups(outgoing, edge => edge.To))
            {
                lines.Add($"    {group}");
            }

            lines.Add($"  Incoming: {incoming.Count} links from {Others(incoming.Select(edge => edge.From))} modules");

            foreach (var group in KindGroups(incoming, edge => edge.From))
            {
                lines.Add($"    {group}");
            }
        }

        var pairs = MutualPairs(edges)
            .Where(pair => module is null
                || pair.A.Equals(module, StringComparison.Ordinal)
                || pair.B.Equals(module, StringComparison.Ordinal))
            .ToList();

        lines.Add($"Mutual: {pairs.Count} pairs link both ways");

        foreach (var (a, b) in pairs)
        {
            lines.Add($"  {a} <-> {b}");
        }

        return lines;
    }

    /// <summary>Every module the model holds: each declared module and each module folder holding records.</summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The module names in alphabetical order.</returns>
    static IReadOnlyList<string> Modules(ProjectModel model)
    {
        var modules = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record is { Type: RecordTypes.Module, Derived: false }
                && Identifiers.TrySplit(record.Id, out _, out _, out var name)
                && name.Length > 0)
            {
                modules.Add(name);
            }

            if (!record.Derived && record.Module is { Length: > 0 } folder)
            {
                modules.Add(folder);
            }
        }

        return [.. modules];
    }

    /// <summary>
    ///     Every written link from a record in one module to a record in another module. An effect
    ///     entry counts under its verb. A link whose target names no record is skipped.
    /// </summary>
    /// <param name="model">The built project model.</param>
    /// <returns>The cross-module edges.</returns>
    static List<Edge> CrossModuleEdges(ProjectModel model)
    {
        var edges = new List<Edge>();

        foreach (var source in model.Records)
        {
            if (source.Derived || source.Id.Length == 0 || source.Module is not { Length: > 0 } from)
            {
                continue;
            }

            foreach (var (kind, target) in KindTargets(model, source))
            {
                if (!model.ById.TryGetValue(Identifiers.BaseId(target), out var resolved)
                    || resolved.Module is not { Length: > 0 } to
                    || to.Equals(from, StringComparison.Ordinal))
                {
                    continue;
                }

                edges.Add(new Edge(from, kind, to));
            }
        }

        return edges;
    }

    /// <summary>Every written link of the record with its kind, expanding effect entries by verb.</summary>
    /// <param name="model">The built project model the record's links resolve against.</param>
    /// <param name="record">The record whose links are read.</param>
    /// <returns>Each link kind with its target identifier.</returns>
    static IEnumerable<(string Kind, string Target)> KindTargets(ProjectModel model, Record record)
    {
        foreach (var (field, targets) in ModelBuilder.ResolvedLinks(model, record))
        {
            if (field.Equals(RecordFields.Effects, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var target in targets)
            {
                yield return (field, target);
            }
        }

        foreach (var verb in new[] { RecordFields.Runs, RecordFields.Sends, RecordFields.Calls, RecordFields.Queries })
        {
            foreach (var target in EffectsVerbIds(record, verb))
            {
                yield return (verb, target);
            }
        }
    }

    /// <summary>The targets of the verb in the record's effect entries, such as every use case a reaction runs.</summary>
    /// <param name="record">The record whose effect entries are read.</param>
    /// <param name="verb">The edge verb to read: runs, sends, calls or queries.</param>
    /// <returns>The target ids the verb names in the effect entries.</returns>
    static List<string> EffectsVerbIds(Record record, string verb)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Effects, out var effects)
            || effects.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var ids = new List<string>();

        foreach (var entry in effects.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(verb, out var target)
                && target.ValueKind == JsonValueKind.String
                && target.GetString() is { Length: > 0 } id)
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>How many other modules the edges reach: each module once.</summary>
    /// <param name="others">The other modules the edges reach.</param>
    /// <returns>The count of distinct modules.</returns>
    static int Others(IEnumerable<string> others) =>
        others.Distinct(StringComparer.Ordinal).Count();

    /// <summary>One line per link kind: its count with the count to each other module.</summary>
    /// <param name="edges">The edges of one side of one module.</param>
    /// <param name="other">The other module of each edge.</param>
    /// <returns>The kind lines in alphabetical kind order.</returns>
    static List<string> KindGroups(List<Edge> edges, Func<Edge, string> other) =>
        edges
            .GroupBy(edge => edge.Kind, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var split = group
                    .GroupBy(other, StringComparer.Ordinal)
                    .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                    .Select(entry => $"{entry.Key}: {entry.Count()}");

                return $"{group.Key}: {group.Count()} ({string.Join(", ", split)})";
            })
            .ToList();

    /// <summary>Every pair of modules with an edge each way, each pair once with the smaller name first.</summary>
    /// <param name="edges">The cross-module edges.</param>
    /// <returns>The mutual pairs in alphabetical order.</returns>
    static List<(string A, string B)> MutualPairs(List<Edge> edges)
    {
        var directed = edges
            .Select(edge => (edge.From, edge.To))
            .ToHashSet();
        var pairs = new SortedSet<(string A, string B)>(Comparer<(string A, string B)>.Create((left, right) =>
        {
            var first = string.Compare(left.A, right.A, StringComparison.Ordinal);

            return first != 0
                ? first
                : string.Compare(left.B, right.B, StringComparison.Ordinal);
        }));

        foreach (var (from, to) in directed)
        {
            if (directed.Contains((to, from)))
            {
                pairs.Add(string.Compare(from, to, StringComparison.Ordinal) < 0 ? (from, to) : (to, from));
            }
        }

        return [.. pairs];
    }
}
