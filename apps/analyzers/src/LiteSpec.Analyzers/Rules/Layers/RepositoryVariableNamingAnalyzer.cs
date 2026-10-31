using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2010: a repository variable is named after its interface.</summary>
/// <follows>litespec/workspace/naming#name-repository-variables-after-their-interface</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositoryVariableNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2010);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    /// <summary>
    ///     Registers the analysis actions for the repository variable naming analyzer rule with the
    ///     compilation context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeParameter, SymbolKind.Parameter);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        context.RegisterOperationAction(AnalyzeDeclarator, OperationKind.VariableDeclarator);
    }

    /// <summary>Analyzes the parameter.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeParameter(SymbolAnalysisContext context)
    {
        var parameter = (IParameterSymbol)context.Symbol;
        if (parameter.IsImplicitlyDeclared || !TryGetExpectedName(parameter.Type, out var expected))
        {
            return;
        }

        if (parameter.Name.Equals(expected, StringComparison.Ordinal))
        {
            return;
        }

        foreach (var declaration in parameter.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is ParameterSyntax syntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2010, syntax.Identifier.GetLocation(), parameter.Name, parameter.Type.Name, expected));
                return;
            }
        }
    }

    /// <summary>Analyzes the field.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (field.IsImplicitlyDeclared || !TryGetExpectedName(field.Type, out var expected))
        {
            return;
        }

        if (field.Name.Equals(expected, StringComparison.Ordinal)
            || field.Name.Equals("_" + expected, StringComparison.Ordinal))
        {
            return;
        }

        foreach (var declaration in field.DeclaringSyntaxReferences)
        {
            if (declaration.GetSyntax() is VariableDeclaratorSyntax declarator)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2010, declarator.Identifier.GetLocation(), field.Name, field.Type.Name, expected));
                return;
            }
        }
    }

    /// <summary>Analyzes the declarator.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void AnalyzeDeclarator(OperationAnalysisContext context)
    {
        var declarator = (IVariableDeclaratorOperation)context.Operation;
        var local = declarator.Symbol;
        if (!TryGetExpectedName(local.Type, out var expected))
        {
            return;
        }

        if (local.Name.Equals(expected, StringComparison.Ordinal))
        {
            return;
        }

        if (declarator.Syntax is VariableDeclaratorSyntax node)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2010, node.Identifier.GetLocation(), local.Name, local.Type.Name, expected));
        }
    }

    /// <summary>Attempts to get expected name and returns true when it succeeds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="expected">The value the record states.</param>
    /// <returns>True when the attempt succeeds; otherwise false.</returns>
    private static bool TryGetExpectedName(ITypeSymbol type, out string expected)
    {
        expected = string.Empty;
        if (type.TypeKind != TypeKind.Interface)
        {
            return false;
        }

        var name = type.Name;
        if (!name.StartsWith("I", StringComparison.Ordinal)
            || !name.EndsWith("Repository", StringComparison.Ordinal)
            || name.Length <= "IRepository".Length)
        {
            return false;
        }

        var bare = name.Substring("I".Length);
        expected = char.ToLowerInvariant(bare[0]) + bare.Substring(1);
        return true;
    }
}
