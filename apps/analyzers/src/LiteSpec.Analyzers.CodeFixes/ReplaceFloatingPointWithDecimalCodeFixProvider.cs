using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Replaces the floating point type LS1008 reports with decimal at the reported
///     location, and rewrites a numeric literal initializer on the same declaration
///     with the decimal suffix. Fix All replaces every reported type in scope.
/// </summary>
/// <follows>litespec/core/backend-domain#keep-binary-floating-point-out-of-exact-quantities</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceFloatingPointWithDecimalCodeFixProvider))]
[Shared]
public sealed class ReplaceFloatingPointWithDecimalCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS1008");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null || TypeAt(root, diagnostic.Location.SourceSpan) is not TypeSyntax)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Replace with decimal",
                createChangedSolution: token => ReplaceAsync(context.Document, diagnostic, token),
                equivalenceKey: "ReplaceFloatingPointWithDecimal"),
            diagnostic);
    }

    /// <summary>Returns the solution with the reported type as decimal.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> ReplaceAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);

        if (root is null || TypeAt(root, diagnostic.Location.SourceSpan) is not TypeSyntax type)
        {
            return document.Project.Solution;
        }

        var decimalType = SyntaxFactory.ParseTypeName("decimal")
            .WithLeadingTrivia(type.GetLeadingTrivia())
            .WithTrailingTrivia(type.GetTrailingTrivia());

        var replacements = new List<(SyntaxNode Old, SyntaxNode New)> { (type, decimalType) };

        foreach (var literal in NumericInitializers(type))
        {
            if (ToDecimalLiteral(literal) is LiteralExpressionSyntax fixedLiteral)
            {
                replacements.Add((literal, fixedLiteral));
            }
        }

        var changed = root.ReplaceNodes(
            replacements.Select(pair => pair.Old),
            (original, _) => replacements.First(pair => pair.Old.Span == original.Span).New);

        return document.WithSyntaxRoot(changed).Project.Solution;
    }

    /// <summary>The type syntax the diagnostic covers, or null when the span names no exact type.</summary>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <returns>The type, or null when none applies.</returns>
    private static TypeSyntax? TypeAt(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var type = root.FindNode(span, getInnermostNodeForTie: true).FirstAncestorOrSelf<TypeSyntax>();

        return type is not null && type.Span == span ? type : null;
    }

    /// <summary>The numeric literals initializing the declaration the type belongs to, through a leading sign.</summary>
    /// <param name="type">The reported type syntax under inspection.</param>
    /// <returns>The numeric literals.</returns>
    private static IEnumerable<LiteralExpressionSyntax> NumericInitializers(TypeSyntax type)
    {
        if (type.FirstAncestorOrSelf<VariableDeclarationSyntax>() is VariableDeclarationSyntax declaration)
        {
            foreach (var variable in declaration.Variables)
            {
                if (LiteralOf(variable.Initializer?.Value) is LiteralExpressionSyntax literal)
                {
                    yield return literal;
                }
            }

            yield break;
        }

        if (type.FirstAncestorOrSelf<PropertyDeclarationSyntax>() is PropertyDeclarationSyntax property
            && LiteralOf(property.Initializer?.Value) is LiteralExpressionSyntax propertyLiteral)
        {
            yield return propertyLiteral;
            yield break;
        }

        if (type.FirstAncestorOrSelf<ParameterSyntax>() is ParameterSyntax parameter
            && LiteralOf(parameter.Default?.Value) is LiteralExpressionSyntax parameterLiteral)
        {
            yield return parameterLiteral;
        }
    }

    /// <summary>The numeric literal the initializer holds, through a leading sign, or null.</summary>
    /// <param name="value">The initializer value under inspection.</param>
    /// <returns>The numeric literal, or null when the value holds none.</returns>
    private static LiteralExpressionSyntax? LiteralOf(ExpressionSyntax? value) =>
        value switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NumericLiteralExpression) => literal,
            PrefixUnaryExpressionSyntax unary when unary.Operand is LiteralExpressionSyntax operand
                && operand.IsKind(SyntaxKind.NumericLiteralExpression) => operand,
            _ => null,
        };

    /// <summary>The literal rewritten with the decimal suffix, or null when the text has no decimal form.</summary>
    /// <param name="literal">The numeric literal under inspection.</param>
    /// <returns>The decimal literal, or null when the text stays as it is.</returns>
    private static LiteralExpressionSyntax? ToDecimalLiteral(LiteralExpressionSyntax literal)
    {
        var text = literal.Token.Text;

        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("0b", StringComparison.OrdinalIgnoreCase)
            || text.EndsWith("m", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var end = text.Length;

        while (end > 0 && "uUlLfFdD".IndexOf(text[end - 1]) >= 0)
        {
            end--;
        }

        if (SyntaxFactory.ParseExpression(text.Substring(0, end) + "m") is not LiteralExpressionSyntax parsed)
        {
            return null;
        }

        return parsed
            .WithLeadingTrivia(literal.GetLeadingTrivia())
            .WithTrailingTrivia(literal.GetTrailingTrivia());
    }
}
