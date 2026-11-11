using System.Text.Json;
using LiteSpec.Cli.Model.Readers.Operations;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks;

/// <summary>The scope checks: every runbook is named by an alert, a job, a reaction or a failure policy.</summary>
/// <follows>litespec/ext/delivery#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</follows>
internal static class ScopeChecks
{
    /// <summary>Runs runbook-unused.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckRunbooksNamed(model, findings);
    }

    /// <summary>Reports each runbook record that no alert, job, reaction or failure policy names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRunbooksNamed(ProjectModel model, List<Finding> findings)
    {
        var named = NamedRunbooks(model);

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Runbook))
        {
            if (named.Contains(record.Id))
            {
                continue;
            }

            findings.Add(new Finding(
                "runbook-unused",
                $"Runbook '{record.Id}' is named by no alert, job, reaction or failure policy.",
                record.File,
                record.Line,
                SubjectOf(record))
            {
                TierOverride = "A",
            });
        }
    }

    /// <summary>Every runbook identifier an alert, job, reaction or failure policy names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The runbook identifiers named by an alert label or by a runbook field in any record.</returns>
    private static HashSet<string> NamedRunbooks(ProjectModel model)
    {
        var named = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in AlertRuleReader.Read(model.Config))
        {
            if (rule.Labels.TryGetValue(RecordFields.Runbook, out var runbook) && runbook.Length > 0)
            {
                named.Add(runbook);
            }
        }

        foreach (var record in model.Records)
        {
            CollectRunbooks(record.Raw, named);
        }

        return named;
    }

    /// <summary>Collects every runbook identifier a record names, at any depth of its written fields.</summary>
    /// <param name="element">The written field value to walk.</param>
    /// <param name="named">The identifiers collected so far, added to in place.</param>
    private static void CollectRunbooks(JsonElement element, HashSet<string> named)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name == RecordFields.Runbook
                        && property.Value.ValueKind == JsonValueKind.String
                        && property.Value.GetString() is { Length: > 0 } id)
                    {
                        named.Add(id);
                    }

                    CollectRunbooks(property.Value, named);
                }

                break;
            case JsonValueKind.Array:
                foreach (var entry in element.EnumerateArray())
                {
                    CollectRunbooks(entry, named);
                }

                break;
        }
    }
}
