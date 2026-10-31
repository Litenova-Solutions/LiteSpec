using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2017: the declared LiteSpecLayer names no known layer.</summary>
/// <follows>litespec/backend/dotnet/architecture#use-five-application-projects</follows>
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
            if (!start.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(LayerResolver.PropertyKey, out var declared)
                || string.IsNullOrWhiteSpace(declared))
            {
                return;
            }

            var value = declared.Trim();
            if (LayerResolver.Parse(value) != Layer.None || IsOptOut(value))
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

    private static bool IsOptOut(string value) =>
        value.Replace(".", string.Empty).ToUpperInvariant() == "NONE";
}
