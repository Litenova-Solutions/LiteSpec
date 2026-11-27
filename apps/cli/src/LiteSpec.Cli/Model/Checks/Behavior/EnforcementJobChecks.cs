using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>
///     The retention enforcement check: a job named by a data class as its enforcer reaches the
///     aggregate holding the class through the use case it runs or sends.
/// </summary>
/// <follows>litespec/core/privacy#enforce-a-data-class-through-its-aggregate</follows>
internal static class EnforcementJobChecks
{
    /// <summary>Reports each enforcement job that writes no use case for the aggregate it enforces.</summary>
    /// <param name="model">The loaded project model that holds every aggregate, job and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var written = WrittenAggregates(model);

        foreach (var aggregate in model.Records.Where(record => record.Type == RecordTypes.Aggregate))
        {
            var home = Identifiers.BaseId(aggregate.Id);

            foreach (var (className, enforcers) in DataClassesOf(aggregate))
            {
                foreach (var enforcer in enforcers
                    .Where(id => id.StartsWith(RecordTypes.Job + "/", StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal))
                {
                    if (written.TryGetValue(Identifiers.BaseId(enforcer), out var targets)
                        && targets.Contains(home, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    findings.Add(new Finding("enforcement-job-unreached",
                        $"Aggregate '{aggregate.Id}' data class '{className}' names enforcement job '{enforcer}', which runs no use case writing '{aggregate.Id}'.",
                        aggregate.File, aggregate.Line, SubjectOf(aggregate)));
                }
            }
        }
    }

    /// <summary>The aggregates each job writes through the use cases it runs or sends.</summary>
    /// <param name="model">The loaded project model that resolves each job's run use cases.</param>
    /// <returns>The aggregates each job writes, keyed by job identifier.</returns>
    private static Dictionary<string, HashSet<string>> WrittenAggregates(ProjectModel model)
    {
        var written = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var job in model.Records.Where(record => record.Type == RecordTypes.Job))
        {
            var targets = new HashSet<string>(StringComparer.Ordinal);

            foreach (var effect in EffectEntries(job))
            {
                foreach (var id in LinkStrings(effect, RecordFields.Runs))
                {
                    if (model.ById.TryGetValue(Identifiers.BaseId(id), out var useCase)
                        && useCase.Type == RecordTypes.UseCase)
                    {
                        foreach (var aggregate in useCase.Ids(RecordFields.Writes))
                        {
                            targets.Add(Identifiers.BaseId(aggregate));
                        }
                    }
                }
            }

            written[job.Id] = targets;
        }

        return written;
    }

    /// <summary>The data classes of an aggregate with their enforcement identifiers.</summary>
    /// <param name="aggregate">The aggregate whose data classes are read.</param>
    /// <returns>Each class name with its enforcedBy identifiers.</returns>
    private static List<(string Name, IReadOnlyList<string> EnforcedBy)> DataClassesOf(Record aggregate)
    {
        var found = new List<(string, IReadOnlyList<string>)>();

        if (aggregate.Raw.ValueKind != JsonValueKind.Object
            || !aggregate.Raw.TryGetProperty(RecordFields.DataClasses, out var classes)
            || classes.ValueKind != JsonValueKind.Array)
        {
            return found;
        }

        foreach (var entry in classes.EnumerateArray().Where(entry => entry.ValueKind == JsonValueKind.Object))
        {
            if (Str(entry, RecordFields.Name) is { Length: > 0 } name)
            {
                found.Add((name, LinkStrings(entry, RecordFields.EnforcedBy)));
            }
        }

        return found;
    }
}
