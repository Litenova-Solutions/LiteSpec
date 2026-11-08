using System;
using System.Collections.Immutable;
using System.Globalization;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Layers;

/// <summary>
///     Whether a rule runs at the conformance level the project claims: at
///     <c>records</c> no rule runs, at <c>traced</c> only the traceability
///     rules <c>LS5001</c> to <c>LS5012</c> run, and at <c>full</c> every rule
///     runs. An empty or unknown value reads as <c>full</c>.
/// </summary>
/// <follows>litespec/core/scope#declare-the-conformance-level</follows>
internal static class ConformanceGate
{
    /// <summary>The analyzer config key the package's build props make visible.</summary>
    public const string PropertyKey = "build_property.LiteSpecConformance";

    /// <summary>Reads the declared conformance property, or full when the project sets none.</summary>
    /// <param name="options">The analyzer options carrying the build properties.</param>
    /// <returns>The conformance level the compilation claims.</returns>
    public static ConformanceLevel Declared(AnalyzerOptions options) =>
        Declared(options.AnalyzerConfigOptionsProvider);

    /// <summary>Reads the declared conformance property, or full when the project sets none.</summary>
    /// <param name="provider">The analyzer configuration options.</param>
    /// <returns>The conformance level the compilation claims.</returns>
    public static ConformanceLevel Declared(AnalyzerConfigOptionsProvider provider)
    {
        if (provider.GlobalOptions.TryGetValue(PropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return ConformanceLevels.TryParse(declared.Trim(), out var level) ? level : ConformanceLevel.Full;
        }

        return ConformanceLevel.Full;
    }

    /// <summary>Whether every diagnostic the rule reports runs at the level.</summary>
    /// <param name="level">The conformance level the compilation claims.</param>
    /// <param name="diagnostics">The diagnostics the rule reports.</param>
    /// <returns>True when the rule runs at the level; otherwise false.</returns>
    public static bool Allows(ConformanceLevel level, ImmutableArray<DiagnosticDescriptor> diagnostics)
    {
        if (level == ConformanceLevel.Full)
        {
            return true;
        }

        if (level == ConformanceLevel.Records || diagnostics.Length == 0)
        {
            return false;
        }

        foreach (var diagnostic in diagnostics)
        {
            if (!IsTraceability(diagnostic.Id))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Whether the diagnostic is a traceability rule, LS5001 to LS5012.</summary>
    /// <param name="id">The diagnostic identifier, such as LS5001.</param>
    /// <returns>True when the identifier names a traceability rule; otherwise false.</returns>
    private static bool IsTraceability(string id) =>
        id.Length == 6
        && id.StartsWith("LS", StringComparison.Ordinal)
        && int.TryParse(id.Substring(2), NumberStyles.None, CultureInfo.InvariantCulture, out var number)
        && number >= 5001
        && number <= 5012;
}
