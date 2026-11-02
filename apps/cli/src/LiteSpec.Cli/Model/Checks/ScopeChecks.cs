using System.Text.Json;
using LiteSpec.Model.Readers.Operations;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The scope checks: no Intent area folder names running the company, and
///     every runbook is named by an alert, a job, a reaction or a failure policy.
/// </summary>
/// <follows>litespec/core/scope#reject-company-areas-in-intent</follows>
/// <follows>litespec/quality/operations#name-every-runbook-from-an-alert-job-reaction-or-failure-policy</follows>
internal static class ScopeChecks
{
    /// <summary>The Intent area folder names that hold running the company instead of the product.</summary>
    private static readonly HashSet<string> CompanyAreas = new(StringComparer.Ordinal)
    {
        "business", "company", "go-to-market", "markets", "roadmap",
    };

    /// <summary>Runs intent-area-invalid and runbook-unused.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckIntentAreas(model, findings);
        CheckRunbooksNamed(model, findings);
    }

    /// <summary>Reports each record file under an Intent area folder that names running the company.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckIntentAreas(ProjectModel model, List<Finding> findings)
    {
        var reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (AreaOf(model, record) is not { } area || !reported.Add(record.File))
            {
                continue;
            }

            findings.Add(new Finding(
                "intent-area-invalid",
                $"Intent area '{area}' holds record '{record.Id}' under a company area folder.",
                record.File,
                record.Line,
                SubjectOf(record)));
        }
    }

    /// <summary>The Intent area folder of the record, when its file sits under an Intent root in a forbidden area.</summary>
    /// <param name="model">The loaded project model that supplies the consumer root.</param>
    /// <param name="record">The record whose file is inspected.</param>
    /// <returns>The forbidden area name, or null when the file sits in no forbidden Intent area.</returns>
    private static string? AreaOf(ProjectModel model, Record record)
    {
        var segments = model.Config.Relative(record.File).Split('/');

        for (var index = 0; index + 1 < segments.Length; index++)
        {
            if (segments[index] == "intent")
            {
                return CompanyAreas.Contains(segments[index + 1]) ? segments[index + 1] : null;
            }
        }

        return null;
    }

    /// <summary>Reports each runbook record that no alert, job, reaction or failure policy names.</summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRunbooksNamed(ProjectModel model, List<Finding> findings)
    {
        var named = NamedRunbooks(model);

        foreach (var record in model.Records.Where(record => record.Type == RecordTypes.Runbook))
        {
            if (record.Status == RecordStatuses.Retired || named.Contains(record.Id))
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
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
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
