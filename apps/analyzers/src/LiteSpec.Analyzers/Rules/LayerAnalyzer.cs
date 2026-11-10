using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules;

/// <summary>
///     The base of every LiteSpec analyzer: skips opted-out compilations, gates a
///     compilation once by the record layers it claims and by its declared or
///     layer-projects layer, and registers the rule for per-file gating under the
///     module layout. Generated code is skipped. A rule runs only when the claim
///     covers every diagnostic it reports, so a rule added later inherits the gate
///     from this base.
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

            var declared = FileScope.DeclaredLayer(start.Options);
            if (declared is not null)
            {
                var layer = LayerResolver.Parse(declared);
                if (layer == Layer.None || !AppliesTo(layer))
                {
                    return;
                }

                Register(start);
                return;
            }

            if (FileScope.DeclaredLayout(start.Options) == Layout.LayerProjects)
            {
                var layer = LayerResolver.FromAssemblyName(start.Compilation.AssemblyName ?? string.Empty);
                if (layer == Layer.None || !AppliesTo(layer))
                {
                    return;
                }

                Register(start);
                return;
            }

            Register(start);
        });
    }

    /// <summary>Whether the rule runs on files of this layer.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected abstract bool AppliesTo(Layer layer);

    /// <summary>Registers the rule's actions for one compilation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected abstract void Register(CompilationStartAnalysisContext context);
}
