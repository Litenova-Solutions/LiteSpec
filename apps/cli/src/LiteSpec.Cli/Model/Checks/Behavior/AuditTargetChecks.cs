using System.Text.Json;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The audit target checks: an audited use case that changes a record names its target by input, by inputs, or by output field, each input carries identifies, each output field exists, and the target kind matches what a single target input identifies.</summary>
/// <follows>litespec/core/use-cases#state-the-audit-position-of-every-command</follows>
/// <follows>litespec/core/audit#declare-which-commands-are-audited</follows>
internal static class AuditTargetChecks
{
    /// <summary>Reports an audited use case changing a record with no target, a target naming no identified input or output field, and a target kind differing from what a single target input identifies.</summary>
    /// <param name="model">The loaded project model that holds every use case and the selected extensions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (!AuditedAudit(useCase, out var audit))
            {
                continue;
            }

            if (!audit.TryGetProperty(RecordFields.Target, out var target)
                || target.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
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

            if (target.ValueKind == JsonValueKind.Array)
            {
                CheckComposite(useCase, target, findings);
                continue;
            }

            if (target.ValueKind == JsonValueKind.Object)
            {
                CheckFromOutput(useCase, target, findings);
                continue;
            }

            if (target.ValueKind != JsonValueKind.String || target.GetString() is not { Length: > 0 } name)
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

            CheckSingle(useCase, audit, name, findings);
        }
    }

    /// <summary>Reports a single target naming no identified input, and a target kind differing from what it identifies.</summary>
    /// <param name="useCase">The audited use case record.</param>
    /// <param name="audit">The audit block holding the target kind.</param>
    /// <param name="target">The input name the target states.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSingle(Record useCase, JsonElement audit, string target, List<Finding> findings)
    {
        var input = useCase.Objects(RecordFields.Inputs)
            .FirstOrDefault(entry => string.Equals(Str(entry, RecordFields.Name), target, StringComparison.Ordinal));

        if (input.ValueKind != JsonValueKind.Object)
        {
            findings.Add(new Finding(
                "audit-target-unknown",
                $"Use case '{useCase.Id}' names audit target '{target}', which no input of the use case carries.",
                useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
        }

        var identifies = Str(input, RecordFields.Identifies);

        if (identifies is not { Length: > 0 })
        {
            findings.Add(new Finding(
                "audit-target-unknown",
                $"Use case '{useCase.Id}' names audit target '{target}', whose input carries no identifies.",
                useCase.File, useCase.Line, SubjectOf(useCase)));
            return;
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

    /// <summary>Reports a composite target naming no identified input; the stated kind is not held to any one input.</summary>
    /// <param name="useCase">The audited use case record.</param>
    /// <param name="target">The target array of input names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckComposite(Record useCase, JsonElement target, List<Finding> findings)
    {
        foreach (var entry in target.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.String || entry.GetString() is not { Length: > 0 } name)
            {
                continue;
            }

            var input = useCase.Objects(RecordFields.Inputs)
                .FirstOrDefault(candidate => string.Equals(Str(candidate, RecordFields.Name), name, StringComparison.Ordinal));

            if (input.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding(
                    "audit-target-unknown",
                    $"Use case '{useCase.Id}' names audit target '{name}', which no input of the use case carries.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
                continue;
            }

            if (Str(input, RecordFields.Identifies) is not { Length: > 0 })
            {
                findings.Add(new Finding(
                    "audit-target-unknown",
                    $"Use case '{useCase.Id}' names audit target '{name}', whose input carries no identifies.",
                    useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Reports a target read from an output field the use case carries no output for.</summary>
    /// <param name="useCase">The audited use case record.</param>
    /// <param name="target">The target object holding the output field.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFromOutput(Record useCase, JsonElement target, List<Finding> findings)
    {
        var field = Str(target, RecordFields.Field);

        if (field is not { Length: > 0 })
        {
            return;
        }

        if (!OutputFieldsOf(useCase).Contains(field, StringComparer.Ordinal))
        {
            findings.Add(new Finding(
                "audit-target-unknown",
                $"Use case '{useCase.Id}' names audit target output field '{field}', which no output field of the use case carries.",
                useCase.File, useCase.Line, SubjectOf(useCase)));
        }
    }

    /// <summary>The output field names of a use case: its command array output, or its query groups flattened.</summary>
    /// <param name="useCase">The use case record whose output is read.</param>
    /// <returns>The output field names.</returns>
    private static List<string> OutputFieldsOf(Record useCase)
    {
        var fields = new List<string>();

        if (useCase.Raw.ValueKind != JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Outputs, out var output))
        {
            return fields;
        }

        if (output.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in output.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.Object
                    && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                {
                    fields.Add(name);
                }
            }

            return fields;
        }

        if (output.ValueKind == JsonValueKind.Object
            && output.TryGetProperty(RecordFields.Include, out var include)
            && include.ValueKind == JsonValueKind.Object)
        {
            foreach (var group in include.EnumerateObject())
            {
                if (group.Value.ValueKind != JsonValueKind.Object
                    || !group.Value.TryGetProperty(RecordFields.Fields, out var array)
                    || array.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var entry in array.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Object
                        && Str(entry, RecordFields.Name) is { Length: > 0 } name)
                    {
                        fields.Add(name);
                    }
                }
            }
        }

        return fields;
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
