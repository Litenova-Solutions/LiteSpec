using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1002: Domain models a closed set as a record hierarchy, never an enum.</summary>
/// <follows>litespec/backend/dotnet/domain#model-every-closed-set-of-domain-values-without-enums</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainEnumAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1002);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>Registers the analysis actions for the domain enum analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => Analyze(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind == TypeKind.Enum)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1002, type.Locations[0], type.Name));
        }
    }
}
