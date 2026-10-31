using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Model.Roslyn;

/// <summary>
///     Reads one syntax tree in a single pass into every marked or markable
///     element: each type or method declaration that carries at least one mark
///     or is of a markable kind, with its kind, its marks, and its location.
///     Unmarked declarations of no known kind are left out.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
public static class MarkFacts
{
    /// <summary>Reads every marked or markable element of one syntax tree, in document order.</summary>
    /// <param name="tree">The syntax tree to read.</param>
    /// <param name="context">The cross-file names the host collected.</param>
    /// <param name="containingProject">
    ///     The short project name holding the tree, or null when unknown.
    ///     See <see cref="KindReader" /> for how a null project reads ports and endpoints.
    /// </param>
    /// <returns>The marked or markable elements, in document order.</returns>
    /// <exception cref="ArgumentNullException">The tree or context is null.</exception>
    public static IReadOnlyList<MarkableElement> Read(
        SyntaxTree tree, KindContext context, string? containingProject = null)
    {
        if (tree is null)
        {
            throw new ArgumentNullException(nameof(tree));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var elements = new List<MarkableElement>();
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            if (node is TypeDeclarationSyntax type)
            {
                var kind = KindReader.KindOf(type, context, containingProject);
                var marks = MarkReader.Read(type);
                if (kind is not null || marks.Count > 0)
                {
                    elements.Add(new MarkableElement(
                        type.Identifier.ValueText,
                        true,
                        kind,
                        marks,
                        LineOf(tree, type.Identifier.Span),
                        type.Identifier.Span));
                }
            }
            else if (node is MethodDeclarationSyntax method)
            {
                var kind = KindReader.KindOf(method, context);
                var marks = MarkReader.Read(method);
                if (kind is not null || marks.Count > 0)
                {
                    elements.Add(new MarkableElement(
                        method.Identifier.ValueText,
                        false,
                        kind,
                        marks,
                        LineOf(tree, method.Identifier.Span),
                        method.Identifier.Span));
                }
            }
        }

        return elements;
    }

    /// <summary>Reads the 1-based line of a span.</summary>
    /// <param name="tree">The syntax tree.</param>
    /// <param name="span">The span to locate.</param>
    /// <returns>The 1-based line where the span starts.</returns>
    private static int LineOf(SyntaxTree tree, TextSpan span) =>
        tree.GetLineSpan(span).StartLinePosition.Line + 1;
}
