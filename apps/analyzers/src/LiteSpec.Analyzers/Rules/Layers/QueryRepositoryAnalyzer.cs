using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2011: a query handler projects through the read session, not a repository.</summary>
/// <follows>litespec/backend/application#project-queries-directly</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QueryRepositoryAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2011);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the query repository analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);

    /// <summary>Analyzes the parameter.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeParameter(SymbolAnalysisContext context)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.IsImplicitlyDeclared)
        {
            return;
        }

        if (!(parameter.ContainingSymbol is IMethodSymbol method) || method.MethodKind != MethodKind.Constructor)
        {
            return;
        }

        if (!IsQueryHandler(method.ContainingType))
        {
            return;
        }

        var type = Unwrap(parameter.Type);
        if (!IsRepositoryInterface(type))
        {
            return;
        }

        foreach (var declaration in parameter.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is ParameterSyntax syntax && syntax.Type != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2011, syntax.Type.GetLocation(), method.ContainingType.Name, type.Name));
                return;
            }
        }
    }

    /// <summary>Returns true when the type is query handler; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is query handler; otherwise false.</returns>
    private static bool IsQueryHandler(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (type.Name.EndsWith("QueryHandler", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IQueryHandler")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is repository interface; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is repository interface; otherwise false.</returns>
    private static bool IsRepositoryInterface(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Interface
        && type.Name.StartsWith("I", StringComparison.Ordinal)
        && type.Name.EndsWith("Repository", StringComparison.Ordinal);

    /// <summary>Returns the i type symbol.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The i type symbol.</returns>
    private static ITypeSymbol Unwrap(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }
}
