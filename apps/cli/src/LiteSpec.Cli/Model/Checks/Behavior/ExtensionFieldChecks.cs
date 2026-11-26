using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Behavior;

/// <summary>The extension field checks: a command declares its audit position when the project selects the audit extension, and no use case writes an extension field while its extension is not selected.</summary>
/// <follows>litespec/core/audit#declare-which-commands-are-audited</follows>
/// <follows>litespec/core/concurrency#document-write-conflict-behavior</follows>
internal static class ExtensionFieldChecks
{
    /// <summary>Reports a command without an audit declaration (audit and concurrency are part of Core).</summary>
    /// <param name="model">The loaded project model that holds every use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            if (useCase.Field(RecordFields.Kind) == "command" && !useCase.Has(RecordFields.Audit))
            {
                findings.Add(new Finding("audit-missing", $"Command '{useCase.Id}' states no audit; declare its audit position or exemption.", useCase.File, useCase.Line, SubjectOf(useCase)));
            }
        }
    }
}
