using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Threading.Tasks;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Adds the missing mark LS5001 reports: one action per candidate record,
///     titled with the record title, written in the project's style. No Fix All:
///     each element answers to its own record. For a test it offers scenarios
///     only. For a technical class it offers the use-case records that handlers
///     in the same folder mark, so a validator beside its handler takes the
///     handler's use case in one action.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</follows>
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
            var siblings = await SiblingUseCasesAsync(
                context.Document, fix, context.CancellationToken).ConfigureAwait(false);
            var siblingStyle = fix.Store.Settings.Marks;

            foreach (var sibling in siblings)
            {
                var id = sibling.Id;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        sibling.Title,
                        createChangedSolution: token => AddMarkAsync(context.Document, diagnostic, MarkVerb.Implements, siblingStyle, id, token),
                        equivalenceKey: "AddMark:" + id),
                    diagnostic);
            }

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

    /// <summary>
    ///     The use-case records handlers in the same folder mark: every
    ///     <c>use-case/</c> id a handler in the technical class's folder carries
    ///     that resolves to a use-case record, in identifier order.
    /// </summary>
    private static async Task<IReadOnlyList<IRecordFacts>> SiblingUseCasesAsync(
        Document document, TraceabilityFix fix, System.Threading.CancellationToken cancellationToken)
    {
        var folder = FolderOf(document.FilePath);
        var comparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var assembly = document.Project.AssemblyName;

        foreach (var sibling in document.Project.Documents)
        {
            if (sibling.Id == document.Id
                || sibling.FilePath is null
                || !string.Equals(FolderOf(sibling.FilePath), folder, comparison))
            {
                continue;
            }

            var root = await sibling.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await sibling.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (root is null || model is null)
            {
                continue;
            }

            foreach (var node in root.DescendantNodes())
            {
                if (node is not TypeDeclarationSyntax type)
                {
                    continue;
                }

                var symbol = model.GetDeclaredSymbol(type, cancellationToken) as INamedTypeSymbol;
                var kind = symbol is not null
                    ? KindReader.KindOf(symbol, KindContext.Empty, assembly)
                    : KindReader.KindOf(type, KindContext.Empty, assembly);

                if (kind != CodeKind.Handler)
                {
                    continue;
                }

                if (symbol is not null)
                {
                    foreach (var reference in symbol.DeclaringSyntaxReferences)
                    {
                        if (reference.GetSyntax(cancellationToken) is MemberDeclarationSyntax member)
                        {
                            CollectUseCases(member, ids);
                        }
                    }
                }
                else
                {
                    CollectUseCases(type, ids);
                }
            }
        }

        var records = new List<IRecordFacts>();

        foreach (var id in ids)
        {
            if (fix.Store.Index.ById(id) is IRecordFacts record && record.Type == "use-case")
            {
                records.Add(record);
            }
        }

        records.Sort((left, right) => string.Compare(left.Id, right.Id, StringComparison.Ordinal));

        return records;
    }

    private static void CollectUseCases(MemberDeclarationSyntax member, HashSet<string> ids)
    {
        foreach (var mark in MarkReader.Read(member))
        {
            if (mark.Verb == MarkVerb.Implements
                && mark.RecordId.StartsWith("use-case/", StringComparison.Ordinal)
                && mark.RecordId.Length > "use-case/".Length)
            {
                ids.Add(mark.RecordId);
            }
        }
    }

    /// <summary>The folder holding the file, with forward slashes: empty for the root.</summary>
    private static string FolderOf(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return string.Empty;
        }

        var normalized = filePath.Replace('\\', '/');
        var slash = normalized.LastIndexOf('/');

        return slash < 0 ? string.Empty : normalized.Substring(0, slash);
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
