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
using Microsoft.CodeAnalysis.Text;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Declares internal sealed the implementation type LS2001 reports: public,
///     protected internal and private protected become internal, and the fix
///     adds sealed unless the class is abstract or static. Fix All declares
///     every reported type in scope.
/// </summary>
/// <follows>litespec/packs/dotnet/architecture#keep-implementation-types-internal</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeInternalSealedCodeFixProvider))]
[Shared]
public sealed class MakeInternalSealedCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS2001");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not TypeDeclarationSyntax type
            || Changes(type).Length == 0)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Declare type internal sealed",
                createChangedSolution: token => FixAsync(context.Document, diagnostic, token),
                equivalenceKey: "MakeInternalSealed"),
            diagnostic);
    }

    /// <summary>Returns the solution with the type declared internal sealed.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> FixAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (root is null
            || text is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not TypeDeclarationSyntax type)
        {
            return document.Project.Solution;
        }

        var changes = Changes(type);

        if (changes.Length == 0)
        {
            return document.Project.Solution;
        }

        return document.WithText(text.WithChanges(changes)).Project.Solution;
    }

    /// <summary>
    ///     The text changes declaring the type internal sealed: the accessibility
    ///     replacement when the type is public, protected internal or private
    ///     protected, and the sealed insert when the type is neither abstract nor
    ///     static.
    /// </summary>
    /// <param name="type">The type declaration under inspection.</param>
    /// <returns>The changes, or an empty array when the type needs no change.</returns>
    private static TextChange[] Changes(TypeDeclarationSyntax type)
    {
        if (type is StructDeclarationSyntax
            || type is InterfaceDeclarationSyntax
            || (type is RecordDeclarationSyntax record
                && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword)))
        {
            return [];
        }

        var changes = new List<TextChange>();

        if (AccessibilitySpan(type) is TextSpan accessibility)
        {
            changes.Add(new TextChange(accessibility, "internal"));
        }

        if (!type.Modifiers.Any(SyntaxKind.SealedKeyword)
            && !type.Modifiers.Any(SyntaxKind.AbstractKeyword)
            && !type.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            changes.Add(new TextChange(new TextSpan(type.Keyword.SpanStart, 0), "sealed "));
        }

        return [.. changes];
    }

    /// <summary>
    ///     The span of the accessibility to replace with internal: public, or the
    ///     whole protected internal or private protected pair in either order.
    /// </summary>
    /// <param name="type">The type declaration under inspection.</param>
    /// <returns>The span, or null when the accessibility stays as written.</returns>
    private static TextSpan? AccessibilitySpan(TypeDeclarationSyntax type)
    {
        var modifiers = type.Modifiers;
        var @public = modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.PublicKeyword));

        if (@public.IsKind(SyntaxKind.PublicKeyword))
        {
            return @public.Span;
        }

        var @protected = modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.ProtectedKeyword));
        var @internal = modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.InternalKeyword));
        var @private = modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.PrivateKeyword));

        if (@protected.IsKind(SyntaxKind.ProtectedKeyword)
            && @internal.IsKind(SyntaxKind.InternalKeyword))
        {
            return TextSpan.FromBounds(
                Math.Min(@protected.SpanStart, @internal.SpanStart),
                Math.Max(@protected.Span.End, @internal.Span.End));
        }

        if (@protected.IsKind(SyntaxKind.ProtectedKeyword)
            && @private.IsKind(SyntaxKind.PrivateKeyword))
        {
            return TextSpan.FromBounds(
                Math.Min(@protected.SpanStart, @private.SpanStart),
                Math.Max(@protected.Span.End, @private.Span.End));
        }

        return null;
    }
}
