using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The audit target checks: an audited use case that changes a record names its target input, the target names an input carrying identifies, and the target kind matches what that input identifies.</summary>
/// <follows>litespec/core/use-cases#state-the-audit-position-of-every-command</follows>
/// <follows>litespec/ext/audit#declare-which-commands-are-audited</follows>
internal static class AuditTargetChecks
{
    /// <summary>Reports an audited use case changing a record with no target, a target naming no identified input, and a target kind differing from what the target identifies.</summary>
    /// <param name="model">The loaded project model that holds every use case and the selected extensions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        if (!model.Config.Extensions.Contains("audit", StringComparer.Ordinal))
        {
            return;
        }

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!AuditedAudit(useCase, out var audit))
            {
                continue;
            }

            var target = Str(audit, RecordFields.Target);

            if (target is not { Length: > 0 })
            {
                if (useCase.Ids(RecordFields.Writes).Count > 0)
                {
                    findings.Add(new Finding(
                        "audit-target-missing",
                        $"Use case '{useCase.Id}' changes a record and is audited, but states no audit target.",
                        useCase.File, useCase.Line, SubjectOf(useCase)));
                }

                continue;
            }

            var input = useCase.Objects(RecordFields.Input)
                .FirstOrDefault(entry => string.Equals(Str(entry, RecordFields.Name), target, StringComparison.Ordinal));

            if (input.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding(
                    "audit-target-unknown",
                    $"Use case '{useCase.Id}' names audit target '{target}', which no input of the use case carries.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            var identifies = Str(input, RecordFields.Identifies);

            if (identifies is not { Length: > 0 })
            {
                findings.Add(new Finding(
                    "audit-target-unknown",
                    $"Use case '{useCase.Id}' names audit target '{target}', whose input carries no identifies.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            var targetKind = Str(audit, RecordFields.TargetKind);

            if (targetKind is { Length: > 0 } && !string.Equals(targetKind, identifies, StringComparison.Ordinal))
            {
                findings.Add(new Finding(
                    "audit-target-kind-drift",
                    $"Use case '{useCase.Id}' states audit target kind '{targetKind}', but its target input '{target}' identifies '{identifies}'.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Whether the use case is audited: its audit block carries level required or optional.</summary>
    /// <param name="useCase">The use case record whose audit block is read.</param>
    /// <param name="audit">The audit block, when the use case is audited.</param>
    /// <returns>True when the audit block carries an audited level; otherwise false.</returns>
    private static bool AuditedAudit(Record useCase, out JsonElement audit)
    {
        audit = default;

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Audit, out var value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (Str(value, RecordFields.Level) is not ("required" or "optional"))
        {
            return false;
        }

        audit = value;
        return true;
    }
}
