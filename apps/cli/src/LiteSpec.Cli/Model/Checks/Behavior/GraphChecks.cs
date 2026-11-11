using System.Text.Json;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The record-graph checks: every behavior record links up to intent, every journey step and notification has a record serving or sending it, every setting default has a requirement behind it, and every scenario exercises a rule.</summary>
/// <follows>litespec/core/model#connect-every-behavior-record-to-intent</follows>
/// <follows>litespec/core/model#serve-every-journey-step</follows>
/// <follows>litespec/core/edges#describe-every-notification-with-audience-channel-and-consent</follows>
/// <follows>litespec/core/use-cases#hold-every-setting-default-to-a-requirement</follows>
/// <follows>litespec/core/use-cases#exercise-rules-from-scenarios</follows>
internal static class GraphChecks
{
    /// <summary>Reports a use case, reaction, job, notification or setting naming neither serves nor fulfills.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunOrphaned(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record =>
            RecordTypes.UpLinkedTypes.Contains(record.Type, StringComparer.Ordinal)))
        {
            if (record.Ids(RecordFields.Serves).Count == 0 && record.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("record-link-missing", $"{Label(record.Type)} '{record.Id}' names neither serves nor fulfills.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports a journey step served by no use case, reaction or job.</summary>
    /// <param name="model">The loaded project model that holds every journey and serving record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunStepUnserved(ProjectModel model, List<Finding> findings)
    {
        var served = model.Records
            .Where(record => RecordTypes.StepServingTypes.Contains(record.Type, StringComparer.Ordinal))
            .SelectMany(record => record.Ids(RecordFields.Serves))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var journey in model.Records.Where(record => record.Type == RecordTypes.Journey))
        {
            foreach (var step in journey.StepIds().Order(StringComparer.Ordinal))
            {
                if (!served.Contains(step))
                {
                    findings.Add(new Finding("step-use-case-missing", $"Journey step '{step}' is served by no {OrList(RecordTypes.StepServingTypes)}.", journey.File, journey.Line, step));
                }
            }
        }
    }

    /// <summary>Reports a setting that sets a default with no requirement behind it.</summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunSettingDefault(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            var carries = setting.Raw.ValueKind == JsonValueKind.Object
                && setting.Raw.TryGetProperty(RecordFields.Default, out var value)
                && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined);

            if (carries && setting.Ids(RecordFields.Fulfills).Count == 0)
            {
                findings.Add(new Finding("setting-requirement-missing", $"Setting '{setting.Id}' sets a default with no requirement behind it.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports a notification sent by no reaction, job or use case.</summary>
    /// <param name="model">The loaded project model that holds every notification and holder record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunNotificationSender(ProjectModel model, List<Finding> findings)
    {
        var sent = new HashSet<string>(StringComparer.Ordinal);

        foreach (var holder in model.Records.Where(record =>
            record.Type is RecordTypes.UseCase or RecordTypes.Reaction or RecordTypes.Job))
        {
            foreach (var entry in EffectEntries(holder))
            {
                if (Str(entry, RecordFields.Sends) is { Length: > 0 } target)
                {
                    sent.Add(target);
                }
            }
        }

        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            if (!sent.Contains(notification.Id))
            {
                findings.Add(new Finding("notification-sender-missing", $"Notification '{notification.Id}' is sent by no reaction, job or use case.", notification.File, notification.Line, SubjectOf(notification)));
            }
        }
    }

    /// <summary>Reports a scenario exercise naming no rule.</summary>
    /// <param name="model">The loaded project model that holds every scenario record and rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunExercises(ProjectModel model, List<Finding> findings)
    {
        foreach (var scenario in model.Records.Where(record => record.Type == RecordTypes.Scenario))
        {
            foreach (var id in scenario.Ids(RecordFields.Exercises).Order(StringComparer.Ordinal))
            {
                if (!model.ById.TryGetValue(id, out var target))
                {
                    findings.Add(new Finding("reference-unknown", $"Scenario '{scenario.Id}' exercises '{id}', which no record holds.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
                else if (target.Type != RecordTypes.Rule)
                {
                    findings.Add(new Finding("field-invalid", $"Scenario '{scenario.Id}' exercises '{id}', which is a {target.Type}.", scenario.File, scenario.Line, SubjectOf(scenario)));
                }
            }
        }
    }
}
