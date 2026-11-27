using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Renames the method LS3001 reports to its name plus Async, so every call
///     site in the solution follows through the rename service. The provider
///     offers no Fix All: each rename applies on its own.
/// </summary>
/// <follows>litespec/workspace/naming#reveal-asynchrony-and-cancellation-in-operation-names</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameAsyncMethodCodeFixProvider))]
[Shared]
public sealed class RenameAsyncMethodCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS3001");

    /// <inheritdoc />
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null
            || model is null
            || DeclaredMethod(root, model, diagnostic.Location.SourceSpan, context.CancellationToken) is not IMethodSymbol method
            || method.Name.EndsWith("Async", System.StringComparison.Ordinal))
        {
            return;
        }

        var renamed = method.Name + "Async";

        context.RegisterCodeFix(
            CodeAction.Create(
                "Rename to '" + renamed + "'",
                createChangedSolution: token => Renamer.RenameSymbolAsync(
                    context.Document.Project.Solution,
                    method,
                    new SymbolRenameOptions(),
                    renamed,
                    token),
                equivalenceKey: "RenameAsyncMethod"),
            diagnostic);
    }

    /// <summary>The method the diagnostic span names, or null.</summary>
    /// <param name="root">The syntax root of the tree being searched.</param>
    /// <param name="model">The semantic model of the document holding the diagnostic.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>The method, or null when none applies.</returns>
    private static IMethodSymbol? DeclaredMethod(
        SyntaxNode root,
        SemanticModel model,
        Microsoft.CodeAnalysis.Text.TextSpan span,
        System.Threading.CancellationToken token)
    {
        var node = root.FindNode(span, getInnermostNodeForTie: true);
        SyntaxNode? declaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        declaration ??= node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();

        if (declaration is MethodDeclarationSyntax method)
        {
            return model.GetDeclaredSymbol(method, token) as IMethodSymbol;
        }

        if (declaration is LocalFunctionStatementSyntax local)
        {
            return model.GetDeclaredSymbol(local, token) as IMethodSymbol;
        }

        return null;
    }
}
