using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Converts the mark LS5004 reports to the declared style: a documentation
///     tag becomes an attribute, and an attribute becomes a documentation tag
///     inside the existing trivia block. Fix All converts every mark in scope.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertMarkStyleCodeFixProvider))]
[Shared]
public sealed class ConvertMarkStyleCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS5004");

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var fix = await TraceabilityFix.ReadAsync(
            context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);

        if (fix is null
            || MarkAt(fix, diagnostic.Location.SourceSpan) is not Mark mark
            || mark.Style == fix.Store.Settings.Marks)
        {
            return;
        }

        var declared = fix.Store.Settings.Marks;
        var title = declared == MarkStyle.Xml ? "Convert mark to documentation tag" : "Convert mark to attribute";

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                createChangedSolution: token => ConvertAsync(context.Document, diagnostic, declared, token),
                equivalenceKey: "ConvertMarkStyle"),
            diagnostic);
    }

    /// <summary>Converts the solution.</summary>
    /// <param name="document">The document that holds the element being fixed.</param>
    /// <param name="diagnostic">The diagnostic the fix answers.</param>
    /// <param name="declared">The mark style the project declares in its settings.</param>
    /// <param name="token">The token that cancels the operation when the host aborts it.</param>
    /// <returns>A task whose result is the solution.</returns>
    private static async Task<Solution> ConvertAsync(
        Document document, Diagnostic diagnostic, MarkStyle declared, System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);
        var span = diagnostic.Location.SourceSpan;

        if (root is null
            || text is null
            || TraceabilityFix.FindMember(root, span) is not MemberDeclarationSyntax member)
        {
            return document.Project.Solution;
        }

        Mark? mark = null;

        foreach (var candidate in MarkReader.Read(member))
        {
            if (candidate.Span == span)
            {
                mark = candidate;
            }
        }

        if (mark is null || !mark.HasRecordId)
        {
            return document.Project.Solution;
        }

        var changed = declared == MarkStyle.Xml
            ? MarkEdits.ConvertToXml(text, root, member, span, mark.Verb, mark.RecordId)
            : MarkEdits.ConvertToAttribute(text, member, span, mark.Verb, mark.RecordId);

        return document.WithText(changed).Project.Solution;
    }

    /// <summary>The mark holding the diagnostic span, or null.</summary>
    /// <param name="fix">The fix context read for the diagnostic: the records, the marked member and its kind.</param>
    /// <param name="span">The source span the diagnostic covers.</param>
    /// <returns>The at, or null when none applies.</returns>
    private static Mark? MarkAt(TraceabilityFix fix, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        foreach (var mark in MarkReader.Read(fix.Member))
        {
            if (mark.Span == span)
            {
                return mark;
            }
        }

        return null;
    }
}
