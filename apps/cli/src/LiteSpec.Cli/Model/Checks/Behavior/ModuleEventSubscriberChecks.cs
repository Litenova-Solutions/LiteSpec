using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>
///     The module event subscriber check: each module event a module publishes is named by a
///     reaction or a read model projection that answers or copies it.
/// </summary>
/// <follows>litespec/core/automation#subscribe-to-every-module-event</follows>
internal static class ModuleEventSubscriberChecks
{
    /// <summary>Reports each module event that no reaction, read model or other consumer names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            foreach (var trigger in ReactionEventTriggers(reaction))
            {
                named.Add(trigger);
            }
        }

        foreach (var readModel in model.Records.Where(record => record.Type == RecordTypes.ReadModel))
        {
            if (readModel.Raw.ValueKind != JsonValueKind.Object
                || !readModel.Raw.TryGetProperty(RecordFields.Fields, out var fields)
                || fields.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var field in fields.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
            {
                if (Str(field, RecordFields.From) is { Length: > 0 } from)
                {
                    var hash = from.IndexOf('#');
                    Add(named, hash < 0 ? from : from[..hash]);
                }

                Add(named, Str(field, RecordFields.ErasedBy));
            }

            foreach (var id in readModel.Ids(RecordFields.RemovedBy))
            {
                Add(named, id);
            }
        }

        foreach (var published in model.Records.Where(record => record.Type == RecordTypes.ModuleEvent && !record.Derived))
        {
            if (named.Contains(published.Id))
            {
                continue;
            }

            findings.Add(new Finding("module-event-unused",
                $"Module event '{published.Id}' is named by no reaction, read model or other consumer.",
                published.File, published.Line, SubjectOf(published)));
        }
    }

    /// <summary>Adds an identifier when it names a module event.</summary>
    /// <param name="named">The set of module event identifiers that have a consumer.</param>
    /// <param name="id">The identifier to add.</param>
    private static void Add(HashSet<string> named, string? id)
    {
        if (id is { Length: > 0 } && id.StartsWith(RecordTypes.ModuleEvent + "/", StringComparison.Ordinal))
        {
            named.Add(id);
        }
    }
}
