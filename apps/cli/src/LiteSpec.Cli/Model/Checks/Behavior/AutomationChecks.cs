using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The automation checks: each reaction is triggered by its own module's domain events, by module events of modules it depends on, or by inbound integration events, each reaction and job declares the outcomes it can reach, and each job carries a schedule a tool can read.</summary>
/// <follows>litespec/core/automation#trigger-every-reaction-from-its-declared-events</follows>
/// <follows>litespec/core/automation#declare-the-outcomes-of-each-reaction-and-job</follows>
/// <follows>litespec/core/automation#run-every-job-from-its-declared-kind</follows>
internal static class AutomationChecks
{
    /// <summary>The run outcomes a reaction or job may declare.</summary>
    private static readonly string[] Outcomes = ["completed", "skipped", "suppressed", "failed", "retry-scheduled", "nothing-selected"];

    /// <summary>Reports each reaction or job that declares no outcomes, an outcome outside the closed set, an outcome named twice, or an outcome with no meaning.</summary>
    /// <param name="model">The loaded project model that holds every reaction and job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunOutcomes(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type is RecordTypes.Reaction or RecordTypes.Job))
        {
            if (!record.Raw.TryGetProperty(RecordFields.Outcomes, out var list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() == 0)
            {
                findings.Add(new Finding("record-invalid", $"{Label(record.Type)} '{record.Id}' declares no outcomes.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in list.EnumerateArray())
            {
                var name = Str(entry, RecordFields.Name);

                if (name is null || Str(entry, RecordFields.Meaning) is not { Length: > 0 })
                {
                    findings.Add(new Finding("record-invalid", $"{Label(record.Type)} '{record.Id}' holds an outcome with no name and no meaning.", record.File, record.Line, SubjectOf(record)));
                    continue;
                }

                if (!Outcomes.Contains(name, StringComparer.Ordinal))
                {
                    findings.Add(new Finding("field-invalid", $"{Label(record.Type)} '{record.Id}' declares outcome '{name}' outside {string.Join(", ", Outcomes)}.", record.File, record.Line, SubjectOf(record)));
                }
                else if (!seen.Add(name))
                {
                    findings.Add(new Finding("record-key-duplicate", $"{Label(record.Type)} '{record.Id}' declares outcome '{name}' twice.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
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

    /// <summary>Reports each reaction with no trigger, a trigger no event holds or that is no event, a domain-event trigger from another module than the reaction's own, and an outbound integration-event trigger. A module event is the declared crossing between modules, so a reaction may answer one from any module.</summary>
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
                else if (resolved.Type is not (RecordTypes.DomainEvent or RecordTypes.ModuleEvent or RecordTypes.IntegrationEvent))
                {
                    findings.Add(new Finding("reaction-trigger-unknown", $"Reaction '{reaction.Id}' names trigger '{trigger}' which is a {resolved.Type}.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
                else if (resolved.Type == RecordTypes.DomainEvent && resolved.Module is { } eventModule && reaction.Module is { } reactionModule && eventModule != reactionModule)
                {
                    findings.Add(new Finding("reaction-trigger-foreign", $"Reaction '{reaction.Id}' in module '{reactionModule}' triggers on domain event '{trigger}' of module '{eventModule}'; it answers a module event or an inbound integration event instead.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
                else if (resolved.Type == RecordTypes.IntegrationEvent && resolved.Field(RecordFields.Direction) == "out")
                {
                    findings.Add(new Finding("reaction-trigger-foreign", $"Reaction '{reaction.Id}' triggers on outbound integration event '{trigger}'; it answers an inbound integration event instead.", reaction.File, reaction.Line, SubjectOf(reaction)));
                }
            }
        }
    }

}
