using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace LiteSpec.Analyzers.CodeFixes;

/// <summary>
///     Corrects the mistyped id LS5002 reports: the three nearest records of the
///     expected type by edit distance, one action each, titled with the record
///     id. No Fix All: each mistyped id is its own mistake.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</follows>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CorrectMarkCodeFixProvider))]
[Shared]
public sealed class CorrectMarkCodeFixProvider : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create("LS5002");

    /// <inheritdoc />
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];
        var fix = await TraceabilityFix.ReadAsync(
            context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);

        if (fix is null)
        {
            return;
        }

        var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);

        if (text is null)
        {
            return;
        }

        var typed = text.ToString(diagnostic.Location.SourceSpan);

        if (!IsIdentifier(typed))
        {
            return;
        }

        IReadOnlyList<IRecordFacts> candidates;

        if (VerbAt(fix, diagnostic.Location.SourceSpan) == MarkVerb.Follows)
        {
            if (typed.StartsWith("litespec/", StringComparison.Ordinal))
            {
                return;
            }

            candidates = FollowsRecords(fix);
        }
        else
        {
            candidates = ExpectedRecords(fix);
        }

        var nearest = Candidates.NearestThree(typed, candidates);

        foreach (var candidate in nearest)
        {
            var id = candidate.Id;

            context.RegisterCodeFix(
                CodeAction.Create(
                    id,
                    createChangedSolution: token => CorrectAsync(context.Document, diagnostic, id, token),
                    equivalenceKey: "CorrectMark:" + id),
                diagnostic);
        }
    }

    private static async Task<Solution> CorrectAsync(
        Document document, Diagnostic diagnostic, string recordId, System.Threading.CancellationToken token)
    {
        var text = await document.GetTextAsync(token).ConfigureAwait(false);

        if (text is null)
        {
            return document.Project.Solution;
        }

        return document.WithText(MarkEdits.Replace(text, diagnostic.Location.SourceSpan, recordId)).Project.Solution;
    }

    /// <summary>
    ///     The records of the kind's expected types, or every record when the
    ///     marked element is of no known kind.
    /// </summary>
    private static IReadOnlyList<IRecordFacts> ExpectedRecords(TraceabilityFix fix)
    {
        if (fix.Kind is not CodeKind kind)
        {
            return fix.Store.Index.Records;
        }

        var records = new List<IRecordFacts>();

        foreach (var type in MarkRules.RecordTypes(kind))
        {
            records.AddRange(fix.Store.Index.ByType(type));
        }

        return records;
    }

    /// <summary>The decision and exception records a <c>follows</c> mark may name.</summary>
    private static IReadOnlyList<IRecordFacts> FollowsRecords(TraceabilityFix fix)
    {
        var records = new List<IRecordFacts>();

        records.AddRange(fix.Store.Index.ByType("decision"));
        records.AddRange(fix.Store.Index.ByType("exception"));

        return records;
    }

    /// <summary>The verb of the mark holding the span, or null when none holds it.</summary>
    private static MarkVerb? VerbAt(TraceabilityFix fix, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        foreach (var mark in MarkReader.Read(fix.Member))
        {
            if (mark.Span.Start <= span.Start && mark.Span.End >= span.End)
            {
                return mark.Verb;
            }
        }

        return null;
    }

    /// <summary>Whether the span text is an identifier a mark could carry: no tag or quote characters.</summary>
    private static bool IsIdentifier(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (!(character >= 'a' && character <= 'z')
                && !(character >= 'A' && character <= 'Z')
                && !(character >= '0' && character <= '9')
                && character != '/' && character != '.' && character != '_' && character != '-')
            {
                return false;
            }
        }

        return true;
    }
}
