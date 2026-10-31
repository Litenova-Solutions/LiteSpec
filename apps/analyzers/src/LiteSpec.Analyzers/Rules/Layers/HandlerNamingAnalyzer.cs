using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2005: a command handler ends in CommandHandler and a query handler in QueryHandler.</summary>
/// <follows>litespec/workspace/naming#use-architectural-suffixes</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2005);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the handler naming analyzer rule with the compilation
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
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "ICommandHandler")
            {
                CheckSuffix(context, type, "ICommandHandler", "CommandHandler");
            }
            else if (contract.Name == "IQueryHandler")
            {
                CheckSuffix(context, type, "IQueryHandler", "QueryHandler");
            }
        }
    }

    /// <summary>Checks the suffix.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="type">The type under inspection.</param>
    /// <param name="contract">The interface contract under inspection.</param>
    /// <param name="suffix">The suffix the name must carry or lose.</param>
    private static void CheckSuffix(
        SymbolAnalysisContext context, INamedTypeSymbol type, string contract, string suffix)
    {
        if (!type.Name.EndsWith(suffix, StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2005, type.Locations[0], type.Name, contract, suffix));
        }
    }
}
