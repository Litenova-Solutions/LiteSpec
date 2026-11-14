using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.FindingText;

namespace LiteSpec.Cli.Model.Checks.Intent;

/// <summary>
///     The source-links checks: a research or constraint explanation that declares sources holds the generated
///     source-links region, and the region matches the sources its record states. The region is derived, so a
///     hand edit to it is drift, and <c>model render</c> writes the correct one.
/// </summary>
/// <follows>litespec/core/intent#cite-a-source-for-each-constraint-rule</follows>
/// <follows>litespec/core/model#render-generated-regions-from-the-model</follows>
internal static class SourceLinkChecks
{
    /// <summary>Reports source-links-missing and source-links-drift for each sourced explanation.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        foreach (var record in model.Records.Where(SourceLinks.Carries))
        {
            if (record.ExplanationFile is null || record.Explanation is null)
            {
                continue;
            }

            var expected = SourceLinks.Content(record).ReplaceLineEndings("\n").Trim();

            if (SourceLinks.BodyOf(record.Explanation) is not { } actual)
            {
                findings.Add(new Finding(
                    "source-links-missing",
                    $"Explanation of '{record.Id}' holds no source-links region; run `litespec model render`.",
                    record.ExplanationFile,
                    1,
                    SubjectOf(record)));
            }
            else if (actual != expected)
            {
                findings.Add(new Finding(
                    "source-links-drift",
                    $"Explanation of '{record.Id}' holds a source-links region that differs from its sources; run `litespec model render`.",
                    record.ExplanationFile,
                    1,
                    SubjectOf(record)));
            }
        }
    }
}
