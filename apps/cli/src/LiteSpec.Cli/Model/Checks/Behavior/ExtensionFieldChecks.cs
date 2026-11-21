using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The extension field checks: a command declares its audit position when the project selects the audit extension, and no use case writes an extension field while its extension is not selected.</summary>
/// <follows>litespec/ext/audit#declare-which-commands-are-audited</follows>
/// <follows>litespec/ext/concurrency#document-write-conflict-behavior</follows>
internal static class ExtensionFieldChecks
{
    /// <summary>Reports a command without audit when the audit extension is selected, and an audit or conflicts field written while its extension is not selected.</summary>
    /// <param name="model">The loaded project model that holds every use case and the selected extensions.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var audit = model.Config.Extensions.Contains("audit", StringComparer.Ordinal);
        var concurrency = model.Config.Extensions.Contains("concurrency", StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (useCase.Field(RecordFields.Kind) == "command" && audit && !useCase.Has(RecordFields.Audit))
            {
                findings.Add(new Finding("audit-missing", $"Command '{useCase.Id}' states no audit; the audit extension is selected, so declare its audit position or exemption.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (useCase.Has(RecordFields.Audit) && !audit)
            {
                findings.Add(new Finding("field-extension-unselected", $"Use case '{useCase.Id}' holds audit, but the audit extension is not selected.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }

            if (useCase.Has(RecordFields.Conflicts) && !concurrency)
            {
                findings.Add(new Finding("field-extension-unselected", $"Use case '{useCase.Id}' holds conflicts, but the concurrency extension is not selected.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }
}
