using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1014: a file outside an aggregate's Domain folder constructs no type from that folder, the root's input types and typed ids aside.</summary>
/// <follows>litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AggregateAccessAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1014);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the aggregate access analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterOperationAction(c => AnalyzeCreation(c, scope, map), OperationKind.ObjectCreation);
    }

    /// <summary>Analyzes the creation.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeCreation(OperationAnalysisContext context, FileScope scope, CodeMap map)
    {
        if (context.Operation.Syntax is null || !scope.AppliesTo(context.Operation.Syntax.SyntaxTree))
        {
            return;
        }

        if (((IObjectCreationOperation)context.Operation).Type is not INamedTypeSymbol created)
        {
            return;
        }

        if (CodeMap.SourceFileOf(created, context.Compilation) is not string declared
            || map.AggregateOf(declared) is not string aggregate)
        {
            return;
        }

        var file = context.Operation.Syntax.SyntaxTree.FilePath;
        if (map.AggregateOf(file) is string home && home.Equals(aggregate, StringComparison.Ordinal))
        {
            return;
        }

        if (IsAllowed(created, aggregate))
        {
            return;
        }

        var location = context.Operation.Syntax is ObjectCreationExpressionSyntax creation
            ? creation.Type.GetLocation()
            : context.Operation.Syntax.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS1014, location, created.Name, CodeMap.FolderName(aggregate)));
    }

    /// <summary>Returns true when the aggregate type may be constructed outside its folder; otherwise false.</summary>
    /// <param name="created">The constructed type.</param>
    /// <param name="aggregate">The aggregate folder holding the constructed type.</param>
    /// <returns>True for an input type or a typed id.</returns>
    private static bool IsAllowed(INamedTypeSymbol created, string aggregate)
    {
        if (created.Name.EndsWith("Id", StringComparison.Ordinal) || ImplementsStronglyTypedId(created))
        {
            return true;
        }

        return IsInputType(created, aggregate);
    }

    /// <summary>Returns true when the parameter type is the created type, or carries it as a type argument or array element.</summary>
    /// <param name="parameter">The parameter type.</param>
    /// <param name="created">The created type.</param>
    /// <returns>True when the parameter takes the created type.</returns>
    private static bool Names(ITypeSymbol parameter, INamedTypeSymbol created)
    {
        if (parameter is IArrayTypeSymbol array)
        {
            return Names(array.ElementType, created);
        }

        if (parameter is not INamedTypeSymbol named)
        {
            return false;
        }

        if (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, created.OriginalDefinition))
        {
            return true;
        }

        foreach (var argument in named.TypeArguments)
        {
            if (Names(argument, created))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type implements the strongly typed identifier contract; otherwise false.</summary>
    /// <param name="created">The constructed type.</param>
    /// <returns>True when an implemented contract is the strongly typed identifier.</returns>
    private static bool ImplementsStronglyTypedId(INamedTypeSymbol created)
    {
        foreach (var contract in created.AllInterfaces)
        {
            if (contract.Name.Equals("IStronglyTypedId", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is an input of the aggregate root's factories or methods; otherwise false.</summary>
    /// <param name="created">The constructed type.</param>
    /// <param name="aggregate">The aggregate folder holding the constructed type.</param>
    /// <returns>True when the root takes the type as a parameter.</returns>
    private static bool IsInputType(INamedTypeSymbol created, string aggregate)
    {
        for (var space = created.ContainingNamespace; space is not null && !space.IsGlobalNamespace; space = space.ContainingNamespace)
        {
            foreach (var candidate in space.GetTypeMembers())
            {
                if (!Aggregates.IsRoot(candidate) || !Aggregates.DeclaredIn(candidate, aggregate))
                {
                    continue;
                }

                foreach (var member in candidate.GetMembers())
                {
                    if (member is IMethodSymbol method && !method.IsImplicitlyDeclared)
                    {
                        foreach (var parameter in method.Parameters)
                        {
                            if (Names(parameter.Type, created))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }
}
