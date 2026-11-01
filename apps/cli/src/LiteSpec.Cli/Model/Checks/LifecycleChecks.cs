using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Authoring;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The lifecycle checks: every aggregate transition names states of the aggregate and a use case that performs it, and every state is reached.</summary>
/// <follows>litespec/core/domain-model#connect-every-state-through-transitions</follows>
internal static class LifecycleChecks
{
    /// <summary>Reports transition-unknown and state-unreached.</summary>
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
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition from '{transition.From}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (!states.Contains(transition.To))
                {
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition to '{transition.To}' which is no state of the aggregate.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }

                if (transition.By is not null && !IsUseCase(model, transition.By))
                {
                    findings.Add(new Finding("transition-unknown", $"Aggregate '{aggregate.Id}' holds a transition by '{transition.By}' which no use case record holds.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }

            var targets = transitions.Select(transition => transition.To).ToHashSet(StringComparer.Ordinal);

            foreach (var state in states.Order(StringComparer.Ordinal))
            {
                if (!targets.Contains(state))
                {
                    findings.Add(new Finding("state-unreached", $"Aggregate '{aggregate.Id}' holds state '{state}' that no transition reaches.", aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
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

    /// <summary>Whether the id names a use case record.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="id">The record identifier under test.</param>
    /// <returns>True when the identifier resolves to a use case record; otherwise false.</returns>
    private static bool IsUseCase(ProjectModel model, string id) =>
        model.ById.TryGetValue(id, out var record) && record.Type == RecordTypes.UseCase;
}
