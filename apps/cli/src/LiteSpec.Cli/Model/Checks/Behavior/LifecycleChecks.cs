using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.ProseText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The lifecycle checks: every aggregate transition names states of the aggregate and a use case that performs it, and every state is reached.</summary>
/// <follows>litespec/core/domain-model#connect-every-state-through-transitions</follows>
internal static class LifecycleChecks
{
    /// <summary>Reports reference-unknown and state-unreached.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var states = StateNames(aggregate);
            var transitions = TransitionsOf(aggregate);

            foreach (var transition in transitions)
            {
                if (transition.From != RecordReader.Creation && !states.Contains(transition.From))
                {
                    findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' holds a transition from '{transition.From}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (!states.Contains(transition.To))
                {
                    findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' holds a transition to '{transition.To}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (transition.MovedBy is not null && !IsMover(model, transition.MovedBy))
                {
                    findings.Add(new Finding("reference-unknown", $"Aggregate '{aggregate.Id}' holds a transition moved by '{transition.MovedBy}' which names no use case or job.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            foreach (var entry in RawTransitions(aggregate))
            {
                var after = Str(entry, RecordFields.After);
                var mover = Str(entry, RecordFields.MovedBy);

                if (after is not null && !IsoDuration.IsMatch(after))
                {
                    findings.Add(new Finding("record-invalid", $"Aggregate '{aggregate.Id}' holds a transition after '{after}', which is not an ISO 8601 duration.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (mover is not null && model.ById.TryGetValue(mover, out var moverRecord) && moverRecord.Type == RecordTypes.Job && after is null)
                {
                    findings.Add(new Finding("record-invalid", $"Aggregate '{aggregate.Id}' holds a transition moved by job '{mover}' with no after duration.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var targets = transitions.Select(transition => transition.To).ToHashSet(StringComparer.Ordinal);
            var unreached = states.Order(StringComparer.Ordinal).Where(state => !targets.Contains(state)).ToList();

            if (unreached.Count == 1)
            {
                findings.Add(new Finding("state-unreached", $"Aggregate '{aggregate.Id}' holds state '{unreached[0]}' that no transition reaches.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
            else if (unreached.Count > 1)
            {
                findings.Add(new Finding("state-unreached", $"Aggregate '{aggregate.Id}' holds states '{string.Join("', '", unreached)}' that no transition reaches.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
            }
        }
    }

    /// <summary>The aggregate's state names.</summary>
    /// <param name="aggregate">The aggregate record whose declared states are read.</param>
    /// <returns>The declared state names with ordinal comparison, or an empty set when the aggregate declares no states array.</returns>
    private static HashSet<string> StateNames(Record aggregate)
    {
        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.States, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return value.EnumerateArray()
            .Where(entry => entry.ValueKind == JsonValueKind.Object
                && entry.TryGetProperty(RecordFields.Name, out var name)
                && name.ValueKind == JsonValueKind.String
                && name.GetString() is { Length: > 0 })
            .Select(entry => entry.GetProperty(RecordFields.Name).GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The transition objects an aggregate declares, as written.</summary>
    /// <param name="aggregate">The aggregate record whose transitions array is read.</param>
    /// <returns>Each object entry of the transitions array, or an empty list when it holds none.</returns>
    private static List<JsonElement> RawTransitions(Record aggregate)
    {
        var found = new List<JsonElement>();

        if (aggregate.Raw.ValueKind == JsonValueKind.Object
            && aggregate.Raw.TryGetProperty(RecordFields.Transitions, out var list)
            && list.ValueKind == JsonValueKind.Array)
        {
            found.AddRange(list.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object));
        }

        return found;
    }

    /// <summary>Whether the id names a use case or a job, the two records that move a state.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="id">The record identifier under test.</param>
    /// <returns>True when the identifier resolves to a use case or job record; otherwise false.</returns>
    private static bool IsMover(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record) && record.Type is RecordTypes.UseCase or RecordTypes.Job;
}
