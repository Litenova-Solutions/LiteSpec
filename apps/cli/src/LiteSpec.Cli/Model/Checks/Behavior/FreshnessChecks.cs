using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The freshness decider check: a decidedBy names the command use case or aggregate that decides the figure, in the module that owns the figure's source.</summary>
/// <follows>litespec/core/use-cases#name-the-use-case-that-decides-a-stale-figure</follows>
internal static class FreshnessChecks
{
    /// <summary>The freshness field naming the use case or aggregate that decides the figure.</summary>
    const string DecidedByField = "decidedBy";

    /// <summary>Reports a freshness decidedBy naming a query or a use case or aggregate outside the module that owns the figure's source.</summary>
    /// <param name="model">The loaded project model that holds every query use case and read model.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var query in model.Records.Where(record => record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "query"))
        {
            foreach (var group in IncludeGroups(query))
            {
                CheckDecidedBy(model, query, $"Use case '{query.Id}' output group '{group.Key}'", group.Value, SourceModules(model, group.Value), findings);
            }
        }

        foreach (var readModel in model.Records.Where(record => record.Type == RecordTypes.ReadModel))
        {
            CheckDecidedBy(model, readModel, $"ReadModel '{readModel.Id}'", readModel.Raw, KeyedByModules(model, readModel), findings);
        }
    }

    /// <summary>Reports a decidedBy on one freshness holder that names a query or a decider outside the source modules.</summary>
    /// <param name="model">The loaded project model that resolves each cited identifier.</param>
    /// <param name="record">The query or read model record carrying the freshness.</param>
    /// <param name="holder">The words naming the freshness holder in the message.</param>
    /// <param name="element">The element holding the freshness object: an output group or a read model.</param>
    /// <param name="sources">The homes of the modules that own the figure's source.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDecidedBy(ProjectModel model, Record record, string holder, JsonElement element, HashSet<string> sources, List<Finding> findings)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(RecordFields.Freshness, out var freshness)
            || freshness.ValueKind != JsonValueKind.Object
            || Str(freshness, DecidedByField) is not { Length: > 0 } decidedBy)
        {
            return;
        }

        // An unknown target is left to reference-unknown, and a target of another type to the schema;
        // a source no record holds leaves nothing to compare the decider against.
        if (!model.ById.TryGetValue(decidedBy, out var target)
            || target.Type is not (RecordTypes.UseCase or RecordTypes.Aggregate)
            || sources.Count == 0)
        {
            return;
        }

        if (target.Type == RecordTypes.UseCase && target.Field(RecordFields.Kind) == "query")
        {
            findings.Add(new Finding("freshness-decider-invalid", $"{holder} names '{decidedBy}' in decidedBy, a query, which decides nothing.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (!Identifiers.TrySplit(decidedBy, out _, out var home, out _) || home is null || !sources.Contains(home))
        {
            findings.Add(new Finding("freshness-decider-invalid", $"{holder} names '{decidedBy}' in decidedBy, outside the module that owns the figure's source.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>The output include groups of a query, in name order.</summary>
    /// <param name="query">The query use case record whose output is read.</param>
    /// <returns>Each group name with its value, or an empty list when the query declares no include groups.</returns>
    private static List<KeyValuePair<string, JsonElement>> IncludeGroups(Record query)
    {
        if (query.Raw.ValueKind == JsonValueKind.Object
            && query.Raw.TryGetProperty(RecordFields.Output, out var output)
            && output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            return include.EnumerateObject().OrderBy(entry => entry.Name, StringComparer.Ordinal).Select(entry => new KeyValuePair<string, JsonElement>(entry.Name, entry.Value)).ToList();
        }

        return [];
    }

    /// <summary>The homes of the modules that own the sources an output group reads.</summary>
    /// <param name="model">The loaded project model that resolves each read identifier.</param>
    /// <param name="group">The output group whose reads are read.</param>
    /// <returns>The home of each read a record holds.</returns>
    private static HashSet<string> SourceModules(ProjectModel model, JsonElement group)
    {
        var homes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in LinkStrings(group, RecordFields.Reads))
        {
            if (!model.ById.TryGetValue(id, out var read))
            {
                continue;
            }

            // A group reads a read model for the rows it copies, so the figure's
            // source is the aggregate the read model is keyed by, as for a read
            // model itself. A read model with no resolvable key leaves nothing
            // its group can be compared against.
            if (read.Type == RecordTypes.ReadModel)
            {
                if (read.Field(RecordFields.KeyedBy) is { Length: > 0 } keyedBy
                    && model.ById.ContainsKey(keyedBy)
                    && Identifiers.TrySplit(keyedBy, out _, out var keyHome, out _)
                    && keyHome is not null)
                {
                    homes.Add(keyHome);
                }

                continue;
            }

            if (Identifiers.TrySplit(id, out _, out var home, out _) && home is not null)
            {
                homes.Add(home);
            }
        }

        return homes;
    }

    /// <summary>The homes of the modules that own the aggregate a read model is keyed by.</summary>
    /// <param name="model">The loaded project model that resolves the keyedBy identifier.</param>
    /// <param name="readModel">The read model record whose keyedBy is read.</param>
    /// <returns>The home of the keyed aggregate when a record holds it; otherwise empty.</returns>
    private static HashSet<string> KeyedByModules(ProjectModel model, Record readModel)
    {
        var homes = new HashSet<string>(StringComparer.Ordinal);

        if (readModel.Field(RecordFields.KeyedBy) is { Length: > 0 } keyedBy
            && model.ById.ContainsKey(keyedBy)
            && Identifiers.TrySplit(keyedBy, out _, out var home, out _)
            && home is not null)
        {
            homes.Add(home);
        }

        return homes;
    }
}
