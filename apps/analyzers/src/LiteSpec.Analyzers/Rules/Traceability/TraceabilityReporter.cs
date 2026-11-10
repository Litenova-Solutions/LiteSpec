using LiteSpec.Model;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     Reports one traceability diagnostic from its finding tier: a finding a
///     live departure covers stays silent, an advisory tier A finding reports
///     as a warning, and every other finding reports as an error. The Roslyn
///     reader is always qualified, so a tier S finding blocks.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class TraceabilityReporter
{
    /// <summary>
    ///     Reports the finding unless a live departure covers it. The message names the code element and the record id.
    /// </summary>
    /// <param name="state">The compilation's traceability context.</param>
    /// <param name="report">How the diagnostic is reported.</param>
    /// <param name="descriptor">The rule's descriptor.</param>
    /// <param name="code">The finding code, such as <c>mark-missing</c>.</param>
    /// <param name="subject">The record id the finding is about, or null.</param>
    /// <param name="location">Where the diagnostic sits.</param>
    /// <param name="messageArgs">The descriptor message arguments.</param>
    public static void Report(
        TraceabilityContext state,
        System.Action<Diagnostic> report,
        DiagnosticDescriptor descriptor,
        string code,
        string? subject,
        Location location,
        params object?[]? messageArgs)
    {
        var path = location.SourceTree is null ? string.Empty : location.SourceTree.FilePath;
        var line = location.SourceTree is null
            ? 1
            : location.SourceTree.GetLineSpan(location.SourceSpan).StartLinePosition.Line + 1;
        var finding = new Finding(code, descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture), path, line, subject);

        if (state.IsKnown(finding))
        {
            return;
        }

        var effective = string.Equals(finding.Tier, "A", System.StringComparison.Ordinal)
            ? DiagnosticSeverity.Warning
            : DiagnosticSeverity.Error;

        report(Diagnostic.Create(descriptor, location, effective, null, null, messageArgs));
    }
}
