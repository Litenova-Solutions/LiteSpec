using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Adds the missing mark LS5001 reports: one action per candidate record,
///     titled with the record title, written in the project's style. No Fix All:
///     each element answers to its own record. A technical class offers no action:
///     no candidate names the choice behind it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddMarkCodeFixProvider))]
[Shared]
public sealed class AddMarkCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS5001");

    /// <inheritdoc />
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var fix = await TraceabilityFix.ReadAsync(
            context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);

        if (fix?.Kind is not CodeKind kind || fix.Symbol is null)
        {
            return;
        }

        if (kind == CodeKind.Technical)
        {
            return;
        }

        var memberName = fix.Symbol is IMethodSymbol ? fix.Symbol.Name : null;
        var typeName = fix.Symbol is IMethodSymbol method ? method.ContainingType.Name : fix.Symbol.Name;
        var candidates = Candidates.Derive(kind, fix.Folders, typeName, memberName, fix.Store.Index);
        var verb = MarkRules.Verb(kind);
        var style = fix.Store.Settings.Marks;

        foreach (var candidate in candidates)
        {
            var id = candidate.Id;

            context.RegisterCodeFix(
                CodeAction.Create(
                    candidate.Title,
                    createChangedSolution: token => AddMarkAsync(context.Document, diagnostic, verb, style, id, token),
                    equivalenceKey: "AddMark:" + id),
                diagnostic);
        }
    }

    private static async Task<Solution> AddMarkAsync(
        Document document,
        Diagnostic diagnostic,
        MarkVerb verb,
        MarkStyle style,
        string recordId,
        System.Threading.CancellationToken token)
    {
        var root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (root is null
            || text is null
            || TraceabilityFix.FindMember(root, diagnostic.Location.SourceSpan) is not MemberDeclarationSyntax member)
        {
            return document.Project.Solution;
        }

        return document.WithText(MarkEdits.InsertMark(text, member, verb, style, recordId)).Project.Solution;
    }
}
