using System;
using System.Collections.Immutable;
using System.Globalization;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     Whether a rule runs for the record layers the project claims. The traceability rules
///     (<c>LS5001</c> to <c>LS5012</c>) run when Implementation is claimed with either practice.
///     The mark rule on tests (<c>LS4001</c>) runs when Proof is claimed with either practice.
///     Every other rule states LiteSpec's practice for Implementation and runs only when that
///     practice is <c>litespec</c>. A layer left out of the claim runs nothing for it.
/// </summary>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
internal static class ClaimGate
{
    /// <summary>The analyzer config key the package's build props make visible for Implementation.</summary>
    public const string ImplementationKey = "build_property.LiteSpecImplementation";

    /// <summary>The analyzer config key the package's build props make visible for Proof.</summary>
    public const string ProofKey = "build_property.LiteSpecProof";

    /// <summary>The mark rule on tests, which governs the Proof layer.</summary>
    private const string ScenarioMarkRule = "LS4001";

    /// <summary>Reads the claim from the build properties.</summary>
    /// <param name="options">The analyzer options carrying the build properties.</param>
    /// <returns>The record layers the compilation claims.</returns>
    public static LayerClaims Declared(AnalyzerOptions options) =>
        Declared(options.AnalyzerConfigOptionsProvider);

    /// <summary>
    ///     Reads the claim from the build properties. A property the build does not set reads as
    ///     <c>litespec</c>, which is what a project file with no layers claims; an empty property
    ///     means the project file leaves that layer out.
    /// </summary>
    /// <param name="provider">The analyzer configuration options.</param>
    /// <returns>The record layers the compilation claims.</returns>
    public static LayerClaims Declared(AnalyzerConfigOptionsProvider provider)
    {
        var global = provider.GlobalOptions;
        return new LayerClaims(true, true, Practice(global, ImplementationKey), Practice(global, ProofKey));
    }

    /// <summary>Whether the traceability rules run: Implementation is claimed with either practice.</summary>
    /// <param name="claims">The claim.</param>
    /// <returns>True when the traceability rules run; otherwise false.</returns>
    public static bool RunsTraceability(LayerClaims claims) =>
        claims.Covers(RecordLayer.Implementation, trace: true);

    /// <summary>Whether the architecture and layer rules run: Implementation follows LiteSpec's practice.</summary>
    /// <param name="claims">The claim.</param>
    /// <returns>True when the architecture and layer rules run; otherwise false.</returns>
    public static bool RunsLayerRules(LayerClaims claims) =>
        claims.Covers(RecordLayer.Implementation, trace: false);

    /// <summary>Whether every diagnostic the rule reports runs under the claim.</summary>
    /// <param name="claims">The claim.</param>
    /// <param name="diagnostics">The diagnostics the rule reports.</param>
    /// <returns>True when the rule runs under the claim; otherwise false.</returns>
    public static bool Allows(LayerClaims claims, ImmutableArray<DiagnosticDescriptor> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var (layer, trace) = Governs(diagnostic.Id);
            if (!claims.Covers(layer, trace))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The record layer and whether the diagnostic ties it to the records.</summary>
    /// <param name="id">The diagnostic identifier, such as LS5001.</param>
    /// <returns>The layer the diagnostic governs, and its trace flag.</returns>
    private static (RecordLayer Layer, bool Trace) Governs(string id)
    {
        if (IsTraceability(id))
        {
            return (RecordLayer.Implementation, true);
        }

        return id == ScenarioMarkRule
            ? (RecordLayer.Proof, true)
            : (RecordLayer.Implementation, false);
    }

    /// <summary>Reads one practice from the build properties.</summary>
    /// <param name="options">The global options.</param>
    /// <param name="key">The property key.</param>
    /// <returns>The practice, or null when the property is set empty.</returns>
    private static LayerPractice? Practice(AnalyzerConfigOptions options, string key)
    {
        if (!options.TryGetValue(key, out var value))
        {
            return LayerPractice.LiteSpec;
        }

        var name = value.Trim();
        if (name.Length == 0)
        {
            return null;
        }

        return name == "own" ? LayerPractice.Own : LayerPractice.LiteSpec;
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
