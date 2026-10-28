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
///     Declares partial the marked event or exception LS5006 reports, so the
///     generator can add the runtime value as a second part. Fix All declares
///     every reported type in scope.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialCodeFixProvider))]
[Shared]
public sealed class AddPartialCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS5006");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not TypeDeclarationSyntax type
            || type.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Declare type partial",
                createChangedSolution: token => AddPartialAsync(context.Document, diagnostic, token),
                equivalenceKey: "AddPartial"),
            diagnostic);
    }

    private static async Task<Solution> AddPartialAsync(
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

        return document.WithText(MarkEdits.AddPartial(text, type)).Project.Solution;
    }
}
