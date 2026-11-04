using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>The name-shape checks on records: template keys, setting units, published event names and job names.</summary>
/// <follows>litespec/core/use-cases#state-the-unit-of-every-quantity-setting</follows>
/// <follows>litespec/core/automation#name-each-job-for-the-use-case-it-runs</follows>
/// <follows>litespec/core/edges#name-published-events-in-the-domains-words</follows>
/// <follows>litespec/core/edges#name-each-template-for-its-subject-event-and-audience</follows>
internal static class RecordNameChecks
{
    /// <summary>Audience words a template key may end with whatever the record declares.</summary>
    private static readonly HashSet<string> TemplateAudienceAliases = new(StringComparer.Ordinal)
    {
        "customer", "member", "owner", "team", "staff", "approver", "requester", "reader",
    };

    /// <summary>Setting value types that measure a quantity and need a unit.</summary>
    private static readonly HashSet<string> QuantityValueTypes = new(StringComparer.Ordinal)
    {
        "number", "integer", "decimal", "duration",
    };

    /// <summary>Internal machinery words an outbound integration event never carries.</summary>
    private static readonly HashSet<string> InternalEventWords = new(StringComparer.Ordinal)
    {
        "staged", "dispatch", "dispatched", "outbox", "handler", "queued", "worker", "job",
    };

    /// <summary>Reports template-key-invalid, setting-unit-missing, integration-event-name-invalid.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckTemplateKeyForm(model, findings);
        CheckSettingUnit(model, findings);
        CheckIntegrationEventWording(model, findings);
    }

    /// <summary>Reports a notification template key ending in no audience the record declares.</summary>
    /// <param name="model">The loaded project model that holds every notification record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckTemplateKeyForm(ProjectModel model, List<Finding> findings)
    {
        foreach (var notification in model.Records.Where(record => record.Type == RecordTypes.Notification))
        {
            if (notification.Field(RecordFields.Template) is not { Length: > 0 } template)
            {
                continue;
            }

            var accepted = new HashSet<string>(TemplateAudienceAliases, StringComparer.Ordinal);

            foreach (var audience in notification.Ids(RecordFields.Addresses))
            {
                accepted.Add(audience);

                var slash = audience.LastIndexOf('/');

                if (slash >= 0 && slash < audience.Length - 1)
                {
                    accepted.Add(audience[(slash + 1)..]);
                }
            }

            var keys = template.Split(',')
                .Select(key => key.Trim())
                .Where(key => key.Length > 0 && !key.StartsWith("none", StringComparison.Ordinal));

            foreach (var key in keys)
            {
                var names = accepted.Any(ending =>
                    key.Equals(ending, StringComparison.Ordinal)
                    || key.EndsWith("-" + ending, StringComparison.Ordinal));

                if (!names)
                {
                    findings.Add(new Finding("template-key-invalid", $"Notification '{notification.Id}' has template key '{key}', which names no addresses the record declares.", notification.File, notification.Line, SubjectOf(notification)));
                }
            }
        }
    }

    /// <summary>Reports a setting holding a number or a duration with no unit.</summary>
    /// <param name="model">The loaded project model that holds every setting record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSettingUnit(ProjectModel model, List<Finding> findings)
    {
        foreach (var setting in model.Records.Where(record => record.Type == RecordTypes.Setting))
        {
            if (setting.Field(RecordFields.ValueType) is not { } valueType || !QuantityValueTypes.Contains(valueType))
            {
                continue;
            }

            if (setting.Field(RecordFields.Unit) is not { Length: > 0 })
            {
                findings.Add(new Finding("setting-unit-missing", $"Setting '{setting.Id}' holds a {valueType} with no unit.", setting.File, setting.Line, SubjectOf(setting)));
            }
        }
    }

    /// <summary>Reports an outbound integration event named with an internal word.</summary>
    /// <param name="model">The loaded project model that holds every integration event record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIntegrationEventWording(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.IntegrationEvent && record.Field(RecordFields.Direction) == "out"))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var hit = name.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => InternalEventWords.Contains(word));

            if (hit is not null)
            {
                findings.Add(new Finding("integration-event-name-invalid", $"Integration event '{record.Id}' names the internal word '{hit}' instead of the business fact.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>Reports job-name-invalid.</summary>
    /// <param name="model">The loaded project model that holds every job record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void RunNames(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Job))
        {
            if (!Identifiers.TrySplit(record.Id, out _, out _, out var name) || name is null)
            {
                continue;
            }

            var verb = FirstWord(name);

            // A job carries the verb of the use case it runs.
            var runs = record.EdgeIds(RecordFields.Runs)
                             .Where(id => Identifiers.TrySplit(id, out _, out _, out var target) && target is not null)
                             .Select(id => { Identifiers.TrySplit(id, out _, out _, out var target); return FirstWord(target!); })
                             .ToList();

            if (runs.Count > 0 && !runs.Contains(verb, StringComparer.Ordinal))
            {
                findings.Add(new Finding("job-name-invalid", $"Job '{record.Id}' starts with '{verb}' but runs '{string.Join("', '", runs.Distinct())}'.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>The opening verb of a name: its first word.</summary>
    /// <param name="name">The kebab-case record name.</param>
    /// <returns>The name up to its first dash, or the whole name when it holds none.</returns>
    private static string FirstWord(string name)
    {
        var dash = name.IndexOf('-');

        return dash < 0 ? name : name[..dash];
    }
}
