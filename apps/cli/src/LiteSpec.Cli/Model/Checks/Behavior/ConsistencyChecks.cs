using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The cross-aggregate consistency checks: each use case states a known consistency mode, an atomic use case writes the group its rule binds, and each reservation names its release.</summary>
/// <follows>litespec/core/use-cases#state-the-consistency-of-every-use-case</follows>
/// <follows>litespec/core/domain-model#bind-every-atomic-write-group-in-its-rule</follows>
/// <follows>litespec/core/domain-model#release-every-reservation-by-a-named-use-case</follows>
internal static class ConsistencyChecks
{
    /// <summary>Reports a use case with a consistency mode outside atomic, per-record or compensated, or changing aggregates atomically with no cites rule.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunConsistency(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            var mode = ConsistencyMode(useCase);

            if (mode is not null && mode is not ("atomic" or "per-record" or "compensated"))
            {
                findings.Add(new Finding("field-invalid", $"Use case '{useCase.Id}' has consistency mode '{mode}' outside atomic, per-record, compensated.", useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (mode != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count > 1 && CitesCount(useCase) == 0)
            {
                findings.Add(new Finding("consistency-cites-missing", $"Use case '{useCase.Id}' changes {changes.Count} aggregates atomically with no cites rule.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports an atomic use case whose changes differ from its atomicity rule's binds.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicGroupDrift(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count < 2)
            {
                continue;
            }

            foreach (var cited in ConsistencyCites(useCase).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(cited, out var rule) || rule.Type != RecordTypes.Rule || rule.Field(RecordFields.Kind) != "atomicity")
                {
                    continue;
                }

                var binds = rule.Ids(RecordFields.Binds).Order(StringComparer.Ordinal).ToList();
                var written = changes.Order(StringComparer.Ordinal).ToList();

                if (binds.Count > 0 && !binds.SequenceEqual(written, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("atomic-group-drift", $"Use case '{useCase.Id}' changes '{string.Join("', '", written)}' but its rule '{cited}' binds '{string.Join("', '", binds)}'.", useCase.File, useCase.Line, SubjectOf(useCase)));
                }
            }
        }
    }

    /// <summary>Reports an atomic group binding more than four aggregates.</summary>
    /// <param name="model">The loaded project model that holds every use case and rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunAtomicGroupInvalid(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "atomicity"))
        {
            var binds = rule.Ids(RecordFields.Binds);

            if (binds.Count > 4)
            {
                findings.Add(new Finding("atomic-group-invalid", $"Rule '{rule.Id}' binds {binds.Count} aggregates.", rule.File, rule.Line, SubjectOf(rule)));
            }
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (ConsistencyMode(useCase) != "atomic")
            {
                continue;
            }

            var changes = useCase.Ids(RecordFields.Changes);

            if (changes.Count > 4 && ConsistencyCites(useCase).Count == 0)
            {
                findings.Add(new Finding("atomic-group-invalid", $"Use case '{useCase.Id}' changes {changes.Count} aggregates atomically.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>The consistency cites rule ids of the use case.</summary>
    /// <param name="useCase">The use case record whose consistency cites are read.</param>
    /// <returns>The cited rule identifiers as a single value or an array, or an empty list when it cites none.</returns>
    private static IReadOnlyList<string> ConsistencyCites(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(RecordFields.Cites, out var cites))
        {
            return [];
        }

        if (cites.ValueKind == JsonValueKind.String && cites.GetString() is { Length: > 0 } single)
        {
            return [single];
        }

        if (cites.ValueKind == JsonValueKind.Array)
        {
            return
            [
                .. cites.EnumerateArray()
                          .Where(entry => entry.ValueKind == JsonValueKind.String)
                          .Select(entry => entry.GetString()!),
            ];
        }

        return [];
    }

    /// <summary>The consistency mode a use case declares, as an object field or a leading word.</summary>
    /// <param name="useCase">The use case record whose consistency field is read.</param>
    /// <returns>The declared mode text, or null when the use case declares no consistency.</returns>
    private static string? ConsistencyMode(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency))
        {
            return null;
        }

        if (consistency.ValueKind == JsonValueKind.Object
            && consistency.TryGetProperty(RecordFields.Mode, out var mode)
            && mode.ValueKind == JsonValueKind.String)
        {
            return mode.GetString();
        }

        if (consistency.ValueKind == JsonValueKind.String
            && consistency.GetString() is { } text)
        {
            return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }

        return null;
    }

    /// <summary>Counts the consistency cites of a use case across its string and array forms.</summary>
    /// <param name="useCase">The use case record whose consistency cites are counted.</param>
    /// <returns>The number of cited rules, or zero when it cites none.</returns>
    private static int CitesCount(Record useCase)
    {
        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Consistency, out var consistency)
            || consistency.ValueKind != JsonValueKind.Object
            || !consistency.TryGetProperty(RecordFields.Cites, out var cites))
        {
            return 0;
        }

        return cites.ValueKind switch
        {
            JsonValueKind.Array => cites.GetArrayLength(),
            JsonValueKind.String when cites.GetString() is { Length: > 0 } => 1,
            _ => 0,
        };
    }

    /// <summary>Reports a reservation rule with no releasedBy, or naming never with no reason.</summary>
    /// <param name="model">The loaded project model that holds every rule record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunReservation(ProjectModel model, List<Finding> findings)
    {
        foreach (var rule in model.Records.Where(record => record.Type == RecordTypes.Rule && record.Field(RecordFields.Kind) == "reservation"))
        {
            if (rule.Raw.ValueKind != JsonValueKind.Object
                || !rule.Raw.TryGetProperty(RecordFields.ReleasedBy, out var releasedBy)
                || releasedBy.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                findings.Add(new Finding("reservation-release-missing", $"Rule '{rule.Id}' reserves with no releasedBy.", rule.File, rule.Line, SubjectOf(rule)));
                continue;
            }

            // A releasing record id; the link checks own its target.
            if (releasedBy.ValueKind == JsonValueKind.String
                && releasedBy.GetString() is { Length: > 0 } id
                && Identifiers.TrySplit(id, out _, out _, out _))
            {
                continue;
            }

            if (releasedBy.ValueKind == JsonValueKind.Object
                && releasedBy.TryGetProperty(RecordFields.Mode, out var mode)
                && mode.ValueKind == JsonValueKind.String
                && mode.GetString() == "never")
            {
                if (!releasedBy.TryGetProperty(RecordFields.Reason, out var reason)
                    || reason.ValueKind != JsonValueKind.String
                    || reason.GetString() is not { Length: > 0 })
                {
                    findings.Add(new Finding("reservation-release-missing", $"Rule '{rule.Id}' names never with no reason.", rule.File, rule.Line, SubjectOf(rule)));
                }

                continue;
            }

            findings.Add(new Finding("field-invalid", $"Rule '{rule.Id}' holds releasedBy as neither a record id nor a never object.", rule.File, rule.Line, SubjectOf(rule)));
        }
    }

    /// <summary>Runs the many-aggregates check.</summary>
    /// <param name="model">The loaded project model that holds every use case record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunManyAggregates(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record =>
            record.Type == RecordTypes.UseCase && record.Field(RecordFields.Kind) == "command"))
        {
            if (ConsistencyMode(useCase) == "atomic" && CitesCount(useCase) > 1)
            {
                findings.Add(new Finding("atomic-group-duplicate", $"Use case '{useCase.Id}' saves more than one atomic write group.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }
}
