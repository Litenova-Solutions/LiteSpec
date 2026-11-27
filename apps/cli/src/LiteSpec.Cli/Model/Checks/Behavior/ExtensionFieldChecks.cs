using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The extension field checks: every use case declares its audit position or exemption, and no use case writes an extension field while its extension is not selected.</summary>
/// <follows>litespec/core/audit#declare-which-commands-are-audited</follows>
/// <follows>litespec/core/concurrency#document-write-conflict-behavior</follows>
internal static class ExtensionFieldChecks
{
    /// <summary>Reports a use case without an audit declaration: every command and query states an audit object or an exemption (audit and concurrency are part of Core).</summary>
    /// <param name="model">The loaded project model that holds every use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase && !record.Derived))
        {
            if (!HasAudit(useCase))
            {
                findings.Add(new Finding("audit-missing", $"Use case '{useCase.Id}' states no audit position or exemption.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }

    /// <summary>Whether the use case states an audit object or an exemption.</summary>
    /// <param name="useCase">The use case record whose audit block is read.</param>
    /// <returns>True when the audit block carries an audit object or exemptBecause; otherwise false.</returns>
    private static bool HasAudit(Record useCase)
    {
        if (!useCase.Has(RecordFields.Audit))
        {
            return false;
        }

        if (useCase.Raw.ValueKind != System.Text.Json.JsonValueKind.Object
            || !useCase.Raw.TryGetProperty(RecordFields.Audit, out var audit)
            || audit.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in audit.EnumerateObject())
        {
            if (property.NameEquals(RecordFields.ExemptBecause))
            {
                return true;
            }
        }

        return audit.EnumerateObject().Any();
    }
}
