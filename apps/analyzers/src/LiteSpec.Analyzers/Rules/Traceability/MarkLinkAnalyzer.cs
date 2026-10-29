using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5002: a mark names no record, a record of a type its element does not
///     take, uses the verb its kind does not take, or sits on a type or method
///     that takes no mark; or a <c>follows</c> mark names no decision, deviation,
///     or provision of the carried release. LS5003: an element carries more marks
///     than its kind takes, or names one record twice. LS5004: a mark is written
///     in the style the project did not declare.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkLinkAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5002, Descriptors.LS5003, Descriptors.LS5004);

    private protected override bool AppliesTo(Layer layer) => layer != Layer.None;

    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        context.RegisterSymbolAction(c => AnalyzeType(c, state), SymbolKind.NamedType);
        context.RegisterSymbolAction(c => AnalyzeMethod(c, state), SymbolKind.Method);
    }

    private static void AnalyzeType(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.IsImplicitlyDeclared || type.TypeKind == TypeKind.Error)
        {
            return;
        }

        Analyze(context, state, type, KindReader.KindOf(type, state.Kinds, state.Project));
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        Analyze(context, state, method, KindReader.KindOf(method, state.Kinds));
    }

    private static void Analyze(
        SymbolAnalysisContext context, TraceabilityContext state, ISymbol symbol, CodeKind? kind)
    {
        var members = Declarations(symbol);
        var counted = new List<string>();

        foreach (var member in members)
        {
            foreach (var mark in MarkReader.Read(member))
            {
                if (kind is null
                    || (kind == CodeKind.Technical ? mark.Verb == MarkVerb.Follows : mark.Verb != MarkVerb.Follows))
                {
                    counted.Add(mark.RecordId);
                }

                AnalyzeMark(context, state, symbol, kind, member, mark);
            }
        }

        if (kind is not null && MarkRules.CheckCount(kind.Value, counted).Violated)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5003,
                "mark-count",
                subject: null,
                symbol.Locations[0],
                symbol.Name,
                counted.Count);
        }
    }

    private static void AnalyzeMark(
        SymbolAnalysisContext context,
        TraceabilityContext state,
        ISymbol symbol,
        CodeKind? kind,
        Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member,
        Mark mark)
    {
        var location = MarkLocation(member, mark);

        if (!mark.HasRecordId)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unresolved",
                subject: null,
                location,
                symbol.Name,
                string.Empty,
                "carries no record identifier");
            AnalyzeStyle(context, state, symbol, mark, location);
            return;
        }

        if (mark.Verb == MarkVerb.Follows)
        {
            AnalyzeFollowsMark(context, state, symbol, mark, location);
            AnalyzeStyle(context, state, symbol, mark, location);
            return;
        }

        if (kind is null)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unresolved",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                symbol is INamedTypeSymbol
                    ? "sits on a type that takes no mark"
                    : "sits on a method that takes no mark");
            AnalyzeStyle(context, state, symbol, mark, location);
            return;
        }

        var record = state.Store.Index.ById(mark.RecordId);

        if (record is null)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unresolved",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "matches no record");
        }
        else if (!MarkRules.Accepts(kind.Value, mark.RecordId))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unresolved",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "is a " + record.Type + " record, not a " + Expected(kind.Value) + " record");
        }

        if (mark.Verb != MarkRules.Verb(kind.Value))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unresolved",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "uses the " + mark.Verb.ToTagName() + " verb, but its kind takes " + MarkRules.Verb(kind.Value).ToTagName());
        }

        AnalyzeStyle(context, state, symbol, mark, location);
    }

    /// <summary>
    ///     Checks one <c>follows</c> mark on a symbol of any kind or none: it names
    ///     a provision of the carried release, or a decision or deviation that is
    ///     not retired. A <c>follows</c> mark is never stray.
    /// </summary>
    private static void AnalyzeFollowsMark(
        SymbolAnalysisContext context,
        TraceabilityContext state,
        ISymbol symbol,
        Mark mark,
        Location location)
    {
        var id = mark.RecordId;

        if (id.StartsWith("litespec/", System.StringComparison.Ordinal))
        {
            if (!ProvisionIds.Contains(id))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unresolved",
                    id,
                    location,
                    symbol.Name,
                    id,
                    "matches no provision of the carried release");
            }

            return;
        }

        if (id.StartsWith("decision/", System.StringComparison.Ordinal)
            || id.StartsWith("deviation/", System.StringComparison.Ordinal))
        {
            var record = state.Store.Index.ById(id);

            if (record is null || (record.Type != "decision" && record.Type != "deviation"))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unresolved",
                    id,
                    location,
                    symbol.Name,
                    id,
                    "matches no decision or deviation");
            }
            else if (record.Status == "retired")
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unresolved",
                    subject: null,
                    location,
                    symbol.Name,
                    id,
                    "is retired");
            }

            return;
        }

        TraceabilityReporter.Report(
            state,
            context.ReportDiagnostic,
            Descriptors.LS5002,
            "mark-unresolved",
            id,
            location,
            symbol.Name,
            id,
            "matches no provision, decision, or deviation");
    }

    private static void AnalyzeStyle(
        SymbolAnalysisContext context, TraceabilityContext state, ISymbol symbol, Mark mark, Location location)
    {
        if (mark.Style != state.Store.Settings.Marks)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5004,
                "mark-style-mixed",
                mark.HasRecordId ? mark.RecordId : null,
                location,
                symbol.Name,
                mark.RecordId,
                mark.Style.ToProjectValue(),
                state.Store.Settings.Marks.ToProjectValue());
        }
    }

    private static string Expected(CodeKind kind) =>
        string.Join(" or ", MarkRules.RecordTypes(kind));
}
