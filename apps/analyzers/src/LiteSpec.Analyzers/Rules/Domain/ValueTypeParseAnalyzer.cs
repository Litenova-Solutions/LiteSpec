using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1017: a typed identifier or value type whose constructor throws on invalid input offers a non-throwing parse.</summary>
/// <follows>litespec/packs/dotnet/domain#offer-a-non-throwing-parse-on-validated-value-types</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueTypeParseAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1017);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the value type parse analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => Analyze(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the named type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Struct || type.DeclaringSyntaxReferences.Length == 0)
        {
            return;
        }

        if (!HasThrowingConstructor(type))
        {
            return;
        }

        if (HasNonThrowingParse(type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1017, type.Locations[0], type.Name));
    }

    /// <summary>Returns true when an explicit instance constructor throws; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when a constructor body holds a throw statement or expression; otherwise false.</returns>
    private static bool HasThrowingConstructor(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol constructor
                || constructor.MethodKind != MethodKind.Constructor
                || constructor.IsStatic)
            {
                continue;
            }

            foreach (var reference in constructor.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is ConstructorDeclarationSyntax declaration
                    && ((declaration.Body is not null && ContainsThrow(declaration.Body))
                        || (declaration.ExpressionBody is not null && ContainsThrow(declaration.ExpressionBody))))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Returns true when the node holds a throw statement or expression; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <returns>True when a throw sits inside the node; otherwise false.</returns>
    private static bool ContainsThrow(SyntaxNode node)
    {
        foreach (var descendant in node.DescendantNodes())
        {
            if (descendant is ThrowStatementSyntax or ThrowExpressionSyntax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type offers a non-throwing parse; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when a static TryParse, TryCreate or TryFrom returns bool with an out parameter of the type; otherwise false.</returns>
    private static bool HasNonThrowingParse(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol parse || !parse.IsStatic)
            {
                continue;
            }

            if (!parse.Name.Equals("TryParse", StringComparison.Ordinal)
                && !parse.Name.Equals("TryCreate", StringComparison.Ordinal)
                && !parse.Name.Equals("TryFrom", StringComparison.Ordinal))
            {
                continue;
            }

            if (parse.ReturnType.SpecialType != SpecialType.System_Boolean)
            {
                continue;
            }

            foreach (var parameter in parse.Parameters)
            {
                if (parameter.RefKind == RefKind.Out
                    && SymbolEqualityComparer.Default.Equals(parameter.Type, type))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
