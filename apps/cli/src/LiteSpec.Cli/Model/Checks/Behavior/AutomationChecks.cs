using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The automation checks: each reaction is triggered by declared events, each manual job has a guard, and each job carries a schedule a tool can read.</summary>
/// <follows>litespec/core/automation#trigger-every-reaction-from-its-declared-events</follows>
/// <follows>litespec/core/automation#guard-every-manual-job</follows>
/// <follows>litespec/core/automation#run-every-job-from-its-declared-kind</follows>
internal static class AutomationChecks
{
    /// <summary>Reports a manual job with no guard.</summary>
    /// <param name="model">The loaded project model that holds every job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunJobGuard(ProjectModel model, List<Finding> findings)
    {
        foreach (var job in model.Records.Where(record => record.Type == RecordTypes.Job && record.Field(RecordFields.Kind) == "manual"))
        {
            if (!FieldPresent(job, "guard"))
            {
                findings.Add(new Finding("job-guard-missing", $"Job '{job.Id}' is a manual job with no guard.", job.File, job.Line, SubjectOf(job)));
            }
        }
    }

    /// <summary>Decides whether a record field carries a present value in its JSON form.</summary>
    /// <param name="record">The record whose field is tested.</param>
    /// <param name="field">The field name under test.</param>
    /// <returns>True when the field holds a non-empty string, a non-empty array, or any other value; otherwise false.</returns>
    private static bool FieldPresent(Record record, string field)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(field, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => false,
            JsonValueKind.String => value.GetString() is { Length: > 0 },
            JsonValueKind.Array => value.GetArrayLength() > 0,
            _ => true,
        };
    }

    /// <summary>Reports schedule-invalid.</summary>
    /// <param name="model">The loaded project model that holds every job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSchedule(ProjectModel model, List<Finding> findings)
    {
        foreach (var job in model.Records.Where(record => record.Type == RecordTypes.Job))
        {
            var structured = StructuredSchedule(job);

            if (job.Field(RecordFields.Kind) == "scheduled" && !structured)
            {
                var missing = !job.Has(RecordFields.Schedule);

                findings.Add(new Finding("schedule-invalid", missing
                    ? $"Job '{job.Id}' runs on time with no structured schedule."
                    : $"Job '{job.Id}' carries a schedule no tool can read.", job.File, job.Line, SubjectOf(job)));
            }
            else if (job.Field(RecordFields.Kind) != "scheduled" && job.Has(RecordFields.Schedule) && !structured)
            {
                findings.Add(new Finding("schedule-invalid", $"Job '{job.Id}' carries a schedule no tool can read.", job.File, job.Line, SubjectOf(job)));
            }
        }
    }

    /// <summary>Decides whether a job carries a schedule a tool can read.</summary>
    /// <param name="job">The job record whose schedule field is inspected.</param>
    /// <returns>True when the schedule object names an interval, a cron form, or startup; otherwise false.</returns>
    private static bool StructuredSchedule(Record job) =>
        job.Raw.ValueKind == JsonValueKind.Object
        && job.Raw.TryGetProperty(RecordFields.Schedule, out var schedule)
        && schedule.ValueKind == JsonValueKind.Object
        && (Str(schedule, RecordFields.Every) is { Length: > 0 } || Str(schedule, RecordFields.Cron) is { Length: > 0 } || Str(schedule, RecordFields.At) == "startup");

    /// <summary>Reports reaction-trigger-unknown.</summary>
    /// <param name="model">The loaded project model that holds every reaction record and event.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunReactionTrigger(ProjectModel model, List<Finding> findings)
    {
        foreach (var reaction in model.Records.Where(record => record.Type == RecordTypes.Reaction))
        {
            var triggers = ReactionEventTriggers(reaction);

            if (triggers.Count == 0)
            {
                findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names no trigger.", reaction.File, reaction.Line, SubjectOf(reaction)));
                continue;
            }

            foreach (var trigger in triggers.Order(StringComparer.Ordinal))
            {
                var baseId = Identifiers.BaseId(trigger);

                if (!model.ById.TryGetValue(baseId, out var resolved))
                {
                    findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names trigger '{trigger}' which no event holds.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
                else if (resolved.Type is not (RecordTypes.Event or RecordTypes.IntegrationEvent))
                {
                    findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names trigger '{trigger}' which is a {resolved.Type}.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
            }
        }
    }
}
