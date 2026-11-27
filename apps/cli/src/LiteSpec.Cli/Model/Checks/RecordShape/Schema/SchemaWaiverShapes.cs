using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.RecordShape.Schema;

/// <summary>The schema waiver shapes: every waiver names the records or paths it covers, and every gap waiver holds its expiry, exit and closing records while a lasting waiver holds its consequences and no gap field.</summary>
/// <follows>litespec/core/waivers#record-accepted-gaps-as-waivers</follows>
/// <follows>litespec/core/waivers#state-the-kind-of-every-waiver</follows>
/// <follows>litespec/core/waivers#name-the-records-that-close-a-gap</follows>
internal static class SchemaWaiverShapes
{
    /// <summary>The waiver field naming the records whose change closes the gap.</summary>
    const string WaiverClosedBy = "closedBy";

    /// <summary>Reports a waiver naming neither the records nor the paths it covers.</summary>
    /// <param name="record">The waiver record whose appliesTo and paths fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckWaiverScope(Record record, List<Finding> findings)
    {
        if (record.Ids(RecordFields.AppliesTo).Count == 0 && record.Ids(RecordFields.Paths).Count == 0)
        {
            findings.Add(new Finding("record-invalid", $"Waiver '{record.Id}' names neither appliesTo nor paths.", record.File, record.Line, SubjectOf(record)));
        }
    }

    /// <summary>Reports a gap waiver without its expiry date, exit or closing records, and a lasting waiver missing its consequences or holding a gap field.</summary>
    /// <param name="record">The waiver record whose kind fields are judged.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void CheckWaiverKind(Record record, List<Finding> findings)
    {
        if (record.Field(RecordFields.Kind) == "gap")
        {
            SchemaFieldRules.RequireDate(record, RecordFields.ExpiresOn, findings);
            SchemaFieldRules.RequireString(record, RecordFields.ExitWhen, findings);
            SchemaFieldRules.RequireIds(record, WaiverClosedBy, 1, findings);
        }
        else if (record.Field(RecordFields.Kind) == "lasting")
        {
            SchemaFieldRules.RequireString(record, RecordFields.Consequences, findings);

            foreach (var field in new[] { RecordFields.ExpiresOn, RecordFields.ExitWhen, WaiverClosedBy })
            {
                if (record.Has(field))
                {
                    findings.Add(new Finding("record-invalid", $"Waiver '{record.Id}' is lasting and holds {field}.", record.File, record.Line, SubjectOf(record)));
                }
            }
        }
    }
}
