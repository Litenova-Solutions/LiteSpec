using System.Text.Json;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;
using static LiteSpec.Cli.Model.Checks.RecordEntries;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema flow shapes: every flow states one reason and holds its branches on the steps they leave, every step is reached, and every branch states one condition and continues at a step of its own flow.</summary>
/// <follows>litespec/core/model#state-the-common-fields-on-every-record</follows>
internal static class SchemaFlowShapes
{
    /// <summary>The word joining two branch conditions, which two branches state instead.</summary>
    private static readonly Regex JoinedCondition = new(
        @"\bor\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    ///     Reports a flow that still holds its branches at the top level, and a step
    ///     that no path reaches: not the first step, with no earlier step falling
    ///     through to it and no branch continuing to it. A step falls through to its
    ///     list successor unless one of its branches continues elsewhere with then;
    ///     a branch that only ends leaves the main path through.
    /// </summary>
    /// <param name="record">The flow record whose steps are read.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFlowBranches(Record record, List<Finding> findings)
    {
        if (record.Has(RecordFields.Branches))
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds {RecordFields.Branches}; state each branch on the step it leaves.", record.File, record.Line, SubjectOf(record)));
        }

        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Steps, out var steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var ids = new List<string>();
        var diverts = new HashSet<string>(StringComparer.Ordinal);
        var continued = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in steps.EnumerateArray())
        {
            if (step.ValueKind != JsonValueKind.Object
                || Str(step, RecordFields.Id) is not { Length: > 0 } id)
            {
                continue;
            }

            ids.Add(id);

            if (step.TryGetProperty(RecordFields.Branches, out var branches)
                && branches.ValueKind == JsonValueKind.Array)
            {
                foreach (var branch in branches.EnumerateArray())
                {
                    if (branch.ValueKind == JsonValueKind.Object
                        && Str(branch, RecordFields.Then) is { Length: > 0 } next)
                    {
                        diverts.Add(id);
                        continued.Add(next);
                    }
                }
            }
        }

        for (var at = 0; at < ids.Count; at++)
        {
            if (at == 0 || continued.Contains(ids[at]) || !diverts.Contains(ids[at - 1]))
            {
                continue;
            }

            findings.Add(new Finding("step-unreached", $"Flow step '{ids[at]}' is reached by no path.", record.File, record.Line, ids[at]));
        }
    }

    /// <summary>
    ///     Reports a step whose use case or carried requirements name a record
    ///     of another type. A step performs with an actor and carries
    ///     the requirements it cites, and nothing else. The step's actor is a link
    ///     field, so the link check judges it.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each link target.</param>
    /// <param name="record">The step record whose links are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckStepLinks(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (Str(record.Raw, RecordFields.UseCase) is { Length: > 0 } id
            && model.ById.TryGetValue(id, out var resolved)
            && resolved.Type != RecordTypes.UseCase)
        {
            findings.Add(new Finding("record-invalid", $"Step '{record.Id}' steps to '{id}', which is a {resolved.Type}.", record.File, record.Line, SubjectOf(record)));
        }

        foreach (var cited in LinkStrings(record.Raw, RecordFields.Carries))
        {
            if (model.ById.TryGetValue(Identifiers.BaseId(cited), out var citedRecord) && citedRecord.Type != RecordTypes.Requirement)
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' carries '{cited}', which is a {citedRecord.Type}, not a requirement.", record.File, record.Line, SubjectOf(record)));
            }
        }
    }

    /// <summary>
    ///     Reports a step branch that lacks its condition, holds then and ends together
    ///     or neither, joins two conditions with or, continues at a step of another
    ///     flow, or names a carried requirement of another type. A branch
    ///     that continues nowhere names no step, so the citation check owns it.
    /// </summary>
    /// <param name="model">The loaded project model that resolves each branch target.</param>
    /// <param name="record">The step record whose branches are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckStepBranches(ProjectModel model, Record record, List<Finding> findings)
    {
        if (record.Raw.ValueKind != JsonValueKind.Object
            || !record.Raw.TryGetProperty(RecordFields.Branches, out var branches))
        {
            return;
        }

        if (branches.ValueKind != JsonValueKind.Array)
        {
            findings.Add(new Finding("record-invalid", $"Step '{record.Id}' holds {RecordFields.Branches} as no array.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (branches.GetArrayLength() == 0)
        {
            findings.Add(new Finding("record-invalid", $"Step '{record.Id}' holds an empty {RecordFields.Branches}.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        foreach (var branch in branches.EnumerateArray())
        {
            if (branch.ValueKind != JsonValueKind.Object)
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' holds a branch as no object.", record.File, record.Line, SubjectOf(record)));
                continue;
            }

            if (Str(branch, RecordFields.When) is not { Length: > 0 } when)
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' has a branch with no {RecordFields.When}.", record.File, record.Line, SubjectOf(record)));
            }
            else if (JoinedCondition.IsMatch(when))
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' joins two conditions with 'or' in {RecordFields.When}; write one branch per condition.", record.File, record.Line, SubjectOf(record)));
            }

            var then = Str(branch, RecordFields.Then);
            var ends = Str(branch, RecordFields.Ends);

            if (then is { Length: > 0 } && ends is { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' has a branch with {RecordFields.Then} and {RecordFields.Ends} together.", record.File, record.Line, SubjectOf(record)));
            }
            else if (then is not { Length: > 0 } && ends is not { Length: > 0 })
            {
                findings.Add(new Finding("record-invalid", $"Step '{record.Id}' has a branch with neither {RecordFields.Then} nor {RecordFields.Ends}.", record.File, record.Line, SubjectOf(record)));
            }
            else if (then is { Length: > 0 } && model.ById.TryGetValue(then, out var next))
            {
                if (next.Type != RecordTypes.Step)
                {
                    findings.Add(new Finding("record-invalid", $"Step '{record.Id}' continues at '{then}', which is a {next.Type}, not a step.", record.File, record.Line, SubjectOf(record)));
                }
                else if (Identifiers.TrySplit(then, out _, out var thenHome, out _)
                    && Identifiers.TrySplit(record.Id, out _, out var home, out _)
                    && !string.Equals(thenHome, home, StringComparison.Ordinal))
                {
                    findings.Add(new Finding("record-invalid", $"Step '{record.Id}' continues at '{then}', which is a step of another flow.", record.File, record.Line, SubjectOf(record)));
                }
            }

            foreach (var cited in LinkStrings(branch, RecordFields.Carries))
            {
                if (model.ById.TryGetValue(Identifiers.BaseId(cited), out var citedRecord) && citedRecord.Type != RecordTypes.Requirement)
                {
                    findings.Add(new Finding("record-invalid", $"Step '{record.Id}' branch carries '{cited}', which is a {citedRecord.Type}, not a requirement.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }

    /// <summary>Reports a flow that does not state exactly one reason, serves, solves or basedOn, held as a list of one item.</summary>
    /// <param name="record">The flow record whose reason fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckFlowReason(Record record, List<Finding> findings)
    {
        var serves = record.Has(RecordFields.Serves);
        var solves = record.Has(RecordFields.Solves);
        var basedOn = record.Has(RecordFields.BasedOn);
        var count = (serves ? 1 : 0) + (solves ? 1 : 0) + (basedOn ? 1 : 0);

        if (count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' states neither serves, solves nor basedOn.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        if (count > 1)
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' states more than one of serves, solves and basedOn.", record.File, record.Line, SubjectOf(record)));
            return;
        }

        var field = serves ? RecordFields.Serves : solves ? RecordFields.Solves : RecordFields.BasedOn;
        if (!IsOneIdList(record, field))
        {
            findings.Add(new Finding("record-invalid", $"Flow '{record.Id}' holds its {field} as a list of exactly one identifier.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Whether a record holds the named field as a list of exactly one string.</summary>
    /// <param name="record">The record to read.</param>
    /// <param name="name">The field name.</param>
    /// <returns>True when the field is an array of one string.</returns>
    internal static bool IsOneIdList(Record record, string name) =>
        record.Raw.ValueKind == JsonValueKind.Object
        && record.Raw.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Array
        && value.GetArrayLength() == 1
        && value[0].ValueKind == JsonValueKind.String;
}
