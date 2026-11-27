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
///     Adds the sealed modifier LS1004 and LS3004 report as missing: a state
///     case that is not sealed, and a concrete class nothing derives from. The
///     fix keeps every trivia and changes nothing else. Fix All seals every
///     reported type in scope.
/// </summary>
/// <follows>litespec/packs/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</follows>
/// <follows>litespec/packs/dotnet/naming#keep-implementation-style-consistent</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SealTypeCodeFixProvider))]
[Shared]
public sealed class SealTypeCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS1004", "LS3004");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not TypeDeclarationSyntax type
            || TakesNoSealed(type))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Declare type sealed",
                createChangedSolution: token => SealAsync(context.Document, diagnostic, token),
                equivalenceKey: "SealType"),
            diagnostic);
    }

    /// <summary>Returns the solution with the sealed modifier added.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> SealAsync(
        Document document, Diagnostic diagnostic, System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (root is null
            || text is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not TypeDeclarationSyntax type
            || TakesNoSealed(type))
        {
            return document.Project.Solution;
        }

        return document.WithText(text.WithChanges(new TextChange(
            new TextSpan(type.Keyword.SpanStart, 0), "sealed "))).Project.Solution;
    }

    /// <summary>Returns true when the type takes no sealed modifier; otherwise false.</summary>
    /// <param name="type">The type declaration under inspection.</param>
    /// <returns>True when the type is already sealed, abstract, static, a struct, or an interface; otherwise false.</returns>
    private static bool TakesNoSealed(TypeDeclarationSyntax type) =>
        type.Modifiers.Any(SyntaxKind.SealedKeyword)
        || type.Modifiers.Any(SyntaxKind.AbstractKeyword)
        || type.Modifiers.Any(SyntaxKind.StaticKeyword)
        || type is StructDeclarationSyntax
        || type is InterfaceDeclarationSyntax
        || (type is RecordDeclarationSyntax record
            && record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword));
}
