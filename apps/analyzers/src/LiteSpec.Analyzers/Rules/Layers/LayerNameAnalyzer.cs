using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2017: the declared LiteSpecLayer names no known layer.</summary>
/// <follows>litespec/backend/dotnet/ext/layer-projects#use-five-application-projects</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayerNameAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2017);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            if (!start.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ArchitectureLayerResolver.PropertyKey, out var declared)
                || string.IsNullOrWhiteSpace(declared))
            {
                return;
            }

            var value = declared.Trim();
            if (ArchitectureLayerResolver.Parse(value) != ArchitectureLayer.None || IsOptOut(value))
            {
                return;
            }

            start.RegisterSyntaxTreeAction(tree =>
            {
                if (tree.Tree == start.Compilation.SyntaxTrees.First())
                {
                    tree.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LS2017, tree.Tree.GetRoot().GetFirstToken().GetLocation(), value));
                }
            });
        });
    }

    /// <summary>Returns true when the value is opt out; otherwise false.</summary>
    /// <param name="value">The value to convert or test.</param>
    /// <returns>True when the value is opt out; otherwise false.</returns>
    private static bool IsOptOut(string value) =>
        string.Equals(value.Replace(".", string.Empty), "NONE", System.StringComparison.OrdinalIgnoreCase);
}
