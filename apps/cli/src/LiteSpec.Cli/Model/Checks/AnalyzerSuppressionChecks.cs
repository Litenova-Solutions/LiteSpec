using LiteSpec.Model.Readers.Suppressions;
using static LiteSpec.Model.Checks.FindingHelpers;

namespace LiteSpec.Model.Checks;

/// <summary>
///     The analyzer suppression check, kept in one class so the partial checker holds only the entry point.
/// </summary>
/// <follows>litespec/core/model#record-a-project-wide-lowering-as-an-exception</follows>
internal static class AnalyzerSuppressionChecks
{
    /// <summary>
    ///     Runs analyzer-lowering-unrecorded: every LiteSpec analyzer rule lowered or
    ///     suppressed in an .editorconfig, a .globalconfig, a NoWarn element, a
    ///     #pragma warning disable or a SuppressMessage, while no live departure
    ///     covers the provision the rule enforces. A departure covers the provision
    ///     when it is live and its covers names the provision, its page, or this finding
    ///     code. A rule the carried release names no provision for reports only when
    ///     no release map could be read at all.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record and the project configuration.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    public static void Run(ProjectModel model, List<Finding> findings)
    {
        var suppressions = SuppressionReader.Read(model.Config.Root);

        if (suppressions.Count == 0)
        {
            return;
        }

        var provisions = AnalyzerRuleProvisions.Read();
        var covered = CoveredProvisions(model, out var codeCovered);

        foreach (var suppression in suppressions)
        {
            var known = provisions.TryGetValue(suppression.Rule, out var provision);

            if (provisions.Count > 0 && !known)
            {
                continue;
            }

            if (codeCovered || (known && (covered.Contains(provision!) || covered.Contains(provision!.Split('#')[0]))))
            {
                continue;
            }

            findings.Add(new Finding(
                "analyzer-lowering-unrecorded",
                Message(suppression, known ? provision : null),
                suppression.File,
                suppression.Line,
                null)
            {
                Reader = Finding.RoslynReader,
                ReaderIsQualified = true,
            });
        }
    }

    /// <summary>
    ///     Every provision a live departure covers, and whether a live departure covers
    ///     this finding code itself. A departure is live when it has no expiresOn date, or its
    ///     expiresOn date is today or later, as the suppression partition reads it.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <param name="codeCovered">Whether a live departure names analyzer-lowering-unrecorded in its covers.</param>
    /// <returns>The provision and page identifiers the live departures name in their covers.</returns>
    private static HashSet<string> CoveredProvisions(ProjectModel model, out bool codeCovered)
    {
        var covered = new HashSet<string>(StringComparer.Ordinal);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        codeCovered = false;

        foreach (var record in model.Records.Where(candidate => candidate.Type == RecordTypes.Departure))
        {
            if (record.Field(RecordFields.ExpiresOn) is { } expiresOn
                && (!IsoDate(expiresOn, out var expires) || today > expires))
            {
                continue;
            }

            foreach (var cover in record.Ids(RecordFields.Covers))
            {
                if (cover.Equals("analyzer-lowering-unrecorded", StringComparison.Ordinal))
                {
                    codeCovered = true;
                }
                else if (cover.StartsWith("litespec/", StringComparison.Ordinal))
                {
                    covered.Add(cover);
                }
            }
        }

        return covered;
    }

    /// <summary>The message one unrecorded lowering or suppression reports: the rule, how it is lowered or suppressed, and the provision no live departure covers.</summary>
    /// <param name="suppression">The lowering or suppression the finding reports.</param>
    /// <param name="provision">The provision the rule enforces, or null when no release map names one.</param>
    /// <returns>The finding message for the suppression.</returns>
    private static string Message(AnalyzerSuppression suppression, string? provision)
    {
        var lowered = suppression.Kind switch
        {
            ".editorconfig" or ".globalconfig" => $"lowered to '{suppression.Detail}' in '{suppression.Kind}'",
            "NoWarn" => $"suppressed by 'NoWarn' in '{Path.GetFileName(suppression.File)}'",
            "#pragma warning disable" => "suppressed by '#pragma warning disable'",
            "SuppressMessage" => "suppressed by 'SuppressMessage'",
            _ => $"lowered or suppressed in '{suppression.Kind}'",
        };

        return provision is not null
            ? $"Analyzer rule '{suppression.Rule}' is {lowered} with no live departure covering '{provision}'."
            : $"Analyzer rule '{suppression.Rule}' is {lowered} with no live departure covering the provision it enforces.";
    }
}
