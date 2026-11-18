using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules;

/// <summary>
///     The base of every LiteSpec analyzer: skips compilations whose claims exclude
///     the rule, and registers the rule for per-file gating. Generated code is skipped.
///     A rule runs only when the claim covers every diagnostic it reports, so a
///     rule added later inherits the gate from this base.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/scope#claim-the-record-layers-the-project-uses</follows>
public abstract class LayerAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public sealed override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (!ClaimGate.Allows(ClaimGate.Declared(start.Options), SupportedDiagnostics))
            {
                return;
            }

            Register(start);
        });
    }

    /// <summary>Whether the rule runs on files of this layer.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected abstract bool AppliesTo(ArchitectureLayer layer);

    /// <summary>Registers the rule's actions for one compilation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected abstract void Register(CompilationStartAnalysisContext context);
}
