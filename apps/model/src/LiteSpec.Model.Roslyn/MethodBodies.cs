using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Every method and local-function body of one syntax tree, each with its size and
///     its normalized text. Generated files, and methods declared in a generated or
///     migrated type, read as no bodies, so generated code never compares against
///     written code. The duplicate-logic check hashes the normalized text.
/// </summary>
/// <follows>litespec/core/domain-model#keep-one-home-for-each-rule-calculation-and-projection</follows>
public static class MethodBodies
{
    /// <summary>Reads every method and local-function body of one syntax tree, in document order.</summary>
    /// <param name="tree">The syntax tree to read.</param>
    /// <returns>The bodies, in document order; empty for a generated file.</returns>
    /// <exception cref="ArgumentNullException">The tree is null.</exception>
    public static IReadOnlyList<MethodBody> Read(SyntaxTree tree)
    {
        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        var bodies = new List<MethodBody>();

        if (GeneratedCode.IsGeneratedFile(tree))
        {
            return bodies;
        }

        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            if (node is MethodDeclarationSyntax method)
            {
                if (BodyOf(method) is SyntaxNode body && !InGeneratedType(method))
                {
                    bodies.Add(ReadBody(tree, method.Identifier, method.ParameterList, body));
                }
            }
            else if (node is LocalFunctionStatementSyntax local)
            {
                if (BodyOf(local) is SyntaxNode body && !InGeneratedType(local))
                {
                    bodies.Add(ReadBody(tree, local.Identifier, local.ParameterList, body));
                }
            }
        }

        return bodies;
    }

    /// <summary>The body a method declares: its block, or its expression when written with an arrow.</summary>
    /// <param name="method">The method declaration.</param>
    /// <returns>The body node, or null when the method declares no body.</returns>
    private static SyntaxNode? BodyOf(MethodDeclarationSyntax method) =>
        (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression;

    /// <summary>The body a local function declares: its block, or its expression when written with an arrow.</summary>
    /// <param name="local">The local-function statement.</param>
    /// <returns>The body node, or null when the local function declares no body.</returns>
    private static SyntaxNode? BodyOf(LocalFunctionStatementSyntax local) =>
        (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression;

    /// <summary>Whether the node sits in a generated or migrated type: a GeneratedCode attribute or a Migration or ModelSnapshot base on any enclosing type.</summary>
    /// <param name="node">The node whose enclosing types are tested.</param>
    /// <returns>True when an enclosing type is generated or migrated.</returns>
    private static bool InGeneratedType(SyntaxNode node)
    {
        foreach (var type in node.Ancestors().OfType<TypeDeclarationSyntax>())
        {
            if (GeneratedCode.HasGeneratedCodeAttribute(type) || GeneratedCode.IsMigration(type))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reads one body: its name, type, line, size and normalized text.</summary>
    /// <param name="tree">The syntax tree holding the declaration.</param>
    /// <param name="name">The declaration name token.</param>
    /// <param name="parameters">The declared parameters.</param>
    /// <param name="body">The body node.</param>
    /// <returns>The method body.</returns>
    private static MethodBody ReadBody(SyntaxTree tree, SyntaxToken name, ParameterListSyntax parameters, SyntaxNode body)
    {
        var containing = name.Parent is null
            ? ""
            : name.Parent.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault() is TypeDeclarationSyntax type
                ? type.Identifier.ValueText
                : "";

        return new MethodBody(
            name.ValueText,
            containing,
            tree.GetLineSpan(name.Span).StartLinePosition.Line + 1,
            body.DescendantNodes().OfType<StatementSyntax>().Count(),
            body.DescendantTokens().Count(),
            Normalize(parameters, body));
    }

    /// <summary>
    ///     Normalizes one body: its tokens joined with single spaces, with local variable
    ///     and parameter names replaced by positional placeholders. Member names, types and
    ///     literals stay as written.
    /// </summary>
    /// <param name="parameters">The declared parameters, replaced by position.</param>
    /// <param name="body">The body node.</param>
    /// <returns>The normalized body text.</returns>
    private static string Normalize(ParameterListSyntax parameters, SyntaxNode body)
    {
        var map = Placeholders(parameters, body);
        var rewritten = new PlaceholderWriter(map).Visit(body) ?? body;

        return string.Join(" ", rewritten.DescendantTokens().Select(token => token.Text));
    }

    /// <summary>
    ///     Maps each local variable and parameter name of one body to its positional
    ///     placeholder: parameters read $p0, $p1 and so on in declared order, and locals
    ///     read $v0, $v1 and so on in first-appearance order. A name declared twice keeps
    ///     its first placeholder.
    /// </summary>
    /// <param name="parameters">The declared parameters.</param>
    /// <param name="body">The body node.</param>
    /// <returns>The placeholder of each declared name.</returns>
    private static Dictionary<string, string> Placeholders(ParameterListSyntax parameters, SyntaxNode body)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var taken = 0;

        foreach (var parameter in parameters.Parameters)
        {
            var name = parameter.Identifier.ValueText;

            if (name.Length > 0 && !map.ContainsKey(name))
            {
                map[name] = "$p" + taken;
                taken++;
            }
        }

        var locals = 0;

        foreach (var node in body.DescendantNodes())
        {
            var name = DeclaredName(node);

            if (name is not null && name.Length > 0 && !map.ContainsKey(name))
            {
                map[name] = "$v" + locals;
                locals++;
            }
        }

        return map;
    }

    /// <summary>The local name a node declares, or null when the node declares none.</summary>
    /// <param name="node">The node under test.</param>
    /// <returns>The declared name, or null.</returns>
    private static string? DeclaredName(SyntaxNode node)
    {
        switch (node)
        {
            case VariableDeclaratorSyntax declarator:
                return declarator.Identifier.ValueText;
            case SingleVariableDesignationSyntax designation:
                return designation.Identifier.ValueText;
            case ForEachStatementSyntax each:
                return each.Identifier.ValueText;
            case CatchDeclarationSyntax caught:
                return caught.Identifier.ValueText;
            case ParameterSyntax parameter:
                return parameter.Identifier.ValueText;
            case FromClauseSyntax from:
                return from.Identifier.ValueText;
            case LetClauseSyntax let:
                return let.Identifier.ValueText;
            case JoinClauseSyntax join:
                return join.Identifier.ValueText;
            default:
                return null;
        }
    }

    /// <summary>Rewrites each local variable and parameter use and declaration to its positional placeholder, leaving member names, types and literals as written.</summary>
    private sealed class PlaceholderWriter : CSharpSyntaxRewriter
    {
        /// <summary>The placeholder of each declared name.</summary>
        private readonly Dictionary<string, string> map;

        /// <summary>Initializes a new instance of the <see cref="PlaceholderWriter"/> class.</summary>
        /// <param name="mapped">The placeholder of each declared name.</param>
        internal PlaceholderWriter(Dictionary<string, string> mapped) => map = mapped;

        /// <summary>Rewrites a name use to its placeholder, unless it names a member or a type.</summary>
        /// <param name="node">The name use.</param>
        /// <returns>The placeholder, or the node unchanged.</returns>
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (map.TryGetValue(node.Identifier.ValueText, out var placeholder)
                && !IsMemberName(node)
                && !IsTypeName(node))
            {
                return SyntaxFactory.IdentifierName(placeholder);
            }

            return base.VisitIdentifierName(node);
        }

        /// <summary>Rewrites a declared local name to its placeholder.</summary>
        /// <param name="token">The token under test.</param>
        /// <returns>The placeholder, or the token unchanged.</returns>
        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.IdentifierToken)
                && map.TryGetValue(token.ValueText, out var placeholder)
                && IsLocalDeclaration(token))
            {
                return SyntaxFactory.Identifier(placeholder);
            }

            return base.VisitToken(token);
        }

        /// <summary>Whether the name use names a member: the name of a member access or member binding, which stays as written.</summary>
        /// <param name="node">The name use.</param>
        /// <returns>True when the name is a member name.</returns>
        private static bool IsMemberName(IdentifierNameSyntax node) =>
            (node.Parent is MemberAccessExpressionSyntax access && access.Name == node)
            || (node.Parent is MemberBindingExpressionSyntax binding && binding.Name == node);

        /// <summary>Whether the name use names a type: a declared, created, cast, tested or qualified type, which stays as written.</summary>
        /// <param name="node">The name use.</param>
        /// <returns>True when the name is a type name.</returns>
        private static bool IsTypeName(IdentifierNameSyntax node)
        {
            switch (node.Parent)
            {
                case VariableDeclarationSyntax declaration when declaration.Type == node:
                case ParameterSyntax parameter when parameter.Type == node:
                case MethodDeclarationSyntax method when method.ReturnType == node:
                case ObjectCreationExpressionSyntax created when created.Type == node:
                case CastExpressionSyntax cast when cast.Type == node:
                case TypeOfExpressionSyntax:
                case DefaultExpressionSyntax:
                case SizeOfExpressionSyntax:
                case TypeArgumentListSyntax:
                case NullableTypeSyntax:
                case ArrayTypeSyntax:
                case QualifiedNameSyntax:
                case AliasQualifiedNameSyntax:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Whether the token declares a local: a variable, parameter, loop, catch, designation or query name, which reads its placeholder.</summary>
        /// <param name="token">The token under test.</param>
        /// <returns>True when the token declares a local.</returns>
        private static bool IsLocalDeclaration(SyntaxToken token)
        {
            switch (token.Parent)
            {
                case VariableDeclaratorSyntax declarator when declarator.Identifier == token:
                case SingleVariableDesignationSyntax designation when designation.Identifier == token:
                case ForEachStatementSyntax each when each.Identifier == token:
                case CatchDeclarationSyntax caught when caught.Identifier == token:
                case ParameterSyntax parameter when parameter.Identifier == token:
                case FromClauseSyntax from when from.Identifier == token:
                case LetClauseSyntax let when let.Identifier == token:
                case JoinClauseSyntax join when join.Identifier == token:
                    return true;
                default:
                    return false;
            }
        }
    }
}
