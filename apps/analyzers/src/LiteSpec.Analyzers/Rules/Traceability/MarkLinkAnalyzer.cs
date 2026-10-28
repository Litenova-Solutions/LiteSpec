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
///     take, or uses the verb its kind does not take. LS5003: an element carries
///     more marks than its kind takes, or names one record twice. LS5004: a mark
///     is written in the style the project did not declare.
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
        var ids = new List<string>();

        foreach (var member in members)
        {
            foreach (var mark in MarkReader.Read(member))
            {
                ids.Add(mark.RecordId);
                AnalyzeMark(context, state, symbol, kind, member, mark);
            }
        }

        if (kind is not null && MarkRules.CheckCount(kind.Value, ids).Violated)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5003,
                "mark-count",
                subject: null,
                symbol.Locations[0],
                symbol.Name,
                ids.Count);
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
        else if (kind is not null && !MarkRules.Accepts(kind.Value, mark.RecordId))
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

        if (kind is not null && mark.Verb != MarkRules.Verb(kind.Value))
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
