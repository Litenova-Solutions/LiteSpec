using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2006: an endpoint implementation ends in Endpoint.</summary>
/// <follows>litespec/workspace/naming#use-architectural-suffixes</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EndpointNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2006);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.WebApi;

    /// <summary>
    ///     Registers the analysis actions for the endpoint naming analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || !ImplementsEndpoint(type))
        {
            return;
        }

        if (!type.Name.EndsWith("Endpoint", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2006, type.Locations[0], type.Name));
        }
    }

    /// <summary>Returns true when the implements-endpoint condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the implements-endpoint condition holds; otherwise false.</returns>
    private static bool ImplementsEndpoint(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IEndpoint")
            {
                return true;
            }
        }

        return false;
    }
}
