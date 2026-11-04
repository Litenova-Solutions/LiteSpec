using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Layers;

/// <summary>
///     Resolves the type one written name refers to, skipping the names an
///     enclosing construct already covers: implicit var, names inside using
///     directives and documentation, and qualifiers of a member access whose
///     member the rule already reports.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal static class References
{
    /// <summary>Tries to resolve the matching type the node names.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="node">The name syntax under inspection.</param>
    /// <param name="isMatch">Whether the rule reports the type.</param>
    /// <param name="name">The name the diagnostic reports.</param>
    /// <param name="type">The named type the check resolved, or null when the node names none.</param>
    /// <returns>True when the node names a matching type; otherwise false.</returns>
    public static bool TryResolve(
        SyntaxNodeAnalysisContext context,
        SimpleNameSyntax node,
        Func<INamedTypeSymbol, bool> isMatch,
        out string name,
        out INamedTypeSymbol? type)
    {
        name = string.Empty;
        type = null;

        // An implicit "var" names no type the author wrote; the expression it stands for is reported instead.
        if (node is IdentifierNameSyntax { IsVar: true } || IsCoveredByEnclosingNode(node, context, isMatch))
        {
            return false;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol;
        if (!NameAndType(symbol, out name, out var resolved) || resolved is null || !isMatch(resolved))
        {
            name = string.Empty;
            return false;
        }

        type = resolved;
        return true;
    }

    /// <summary>Returns true when the node is covered by enclosing node; otherwise false.</summary>
    /// <param name="node">The syntax node under inspection.</param>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="isMatch">Whether the rule reports the type.</param>
    /// <returns>True when the node is covered by enclosing node; otherwise false.</returns>
    private static bool IsCoveredByEnclosingNode(
        SimpleNameSyntax node, SyntaxNodeAnalysisContext context, Func<INamedTypeSymbol, bool> isMatch)
    {
        var child = (SyntaxNode)node;
        for (var parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is UsingDirectiveSyntax || parent is DocumentationCommentTriviaSyntax)
            {
                return true;
            }

            if (parent is MemberAccessExpressionSyntax access
                && ReferenceEquals(access.Expression, child)
                && IsMatch(context.SemanticModel.GetSymbolInfo(access.Name, context.CancellationToken).Symbol, isMatch))
            {
                return true;
            }

            child = parent;
        }

        return false;
    }

    /// <summary>Returns true when the symbol resolves to a matching type; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="isMatch">Whether the rule reports the type.</param>
    /// <returns>True when the symbol resolves to a matching type; otherwise false.</returns>
    private static bool IsMatch(ISymbol? symbol, Func<INamedTypeSymbol, bool> isMatch)
    {
        var type = symbol switch
        {
            INamedTypeSymbol named => named,
            IMethodSymbol method => method.ContainingType,
            IPropertySymbol property => property.ContainingType,
            IFieldSymbol field => field.ContainingType,
            IEventSymbol @event => @event.ContainingType,
            _ => null,
        };

        return type is not null && isMatch(type);
    }

    /// <summary>Returns the reported name and type of the symbol.</summary>
    /// <param name="symbol">The symbol whose name and type the check reads, or null when it has none.</param>
    /// <param name="name">The name the diagnostic reports.</param>
    /// <param name="type">The named type the check resolved.</param>
    /// <returns>True when the symbol is of a named kind; otherwise false.</returns>
    private static bool NameAndType(ISymbol? symbol, out string name, out INamedTypeSymbol? type)
    {
        name = string.Empty;
        type = null;

        switch (symbol)
        {
            case INamedTypeSymbol named:
                name = named.Name;
                type = named;
                return true;
            case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor when constructor.ContainingType is not null:
                name = constructor.ContainingType.Name;
                type = constructor.ContainingType;
                return true;
            case IMethodSymbol method when method.ContainingType is not null:
                name = method.Name;
                type = method.ContainingType;
                return true;
            case IPropertySymbol property when property.ContainingType is not null:
                name = property.Name;
                type = property.ContainingType;
                return true;
            case IFieldSymbol field when field.ContainingType is not null:
                name = field.Name;
                type = field.ContainingType;
                return true;
            case IEventSymbol @event when @event.ContainingType is not null:
                name = @event.Name;
                type = @event.ContainingType;
                return true;
            default:
                return false;
        }
    }
}
