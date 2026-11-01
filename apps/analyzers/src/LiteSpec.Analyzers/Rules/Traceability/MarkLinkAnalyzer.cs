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
///     that takes no mark; or a <c>follows</c> mark names no decision, exception,
///     or provision of the carried release. LS5003: an element carries more marks
///     than its kind takes, or names one record twice. LS5004: a mark is written
///     in the style the project did not declare.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</follows>
/// <follows>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkLinkAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5002, Descriptors.LS5003, Descriptors.LS5004);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer != Layer.None;

    /// <summary>Registers the analysis actions for the mark link analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        context.RegisterSymbolAction(c => AnalyzeType(c, state), SymbolKind.NamedType);
        context.RegisterSymbolAction(c => AnalyzeMethod(c, state), SymbolKind.Method);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.IsImplicitlyDeclared || type.TypeKind == TypeKind.Error)
        {
            return;
        }

        Analyze(context, state, type, KindReader.KindOf(type, state.Kinds, state.Project));
    }

    /// <summary>Analyzes the method.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private static void AnalyzeMethod(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        Analyze(context, state, method, KindReader.KindOf(method, state.Kinds));
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
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
                    || (kind == CodeKind.Technical
                        ? mark.Verb == MarkVerb.Implements || mark.Verb == MarkVerb.Follows
                        : mark.Verb != MarkVerb.Follows))
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
                "mark-count-invalid",
                subject: null,
                symbol.Locations[0],
                symbol.Name,
                counted.Count);
        }
    }

    /// <summary>Analyzes the mark.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="mark">The mark read from the element.</param>
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
                "mark-unknown",
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
                "mark-unknown",
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
            // A test proves the lowest record it verifies, so a proves mark may
            // name a provision of the carried release, checked for existence as a
            // follows mark is; every other kind still matches no record.
            if (kind == CodeKind.Test && mark.RecordId.StartsWith("litespec/", System.StringComparison.Ordinal))
            {
                if (!ProvisionIds.Contains(mark.RecordId))
                {
                    TraceabilityReporter.Report(
                        state,
                        context.ReportDiagnostic,
                        Descriptors.LS5002,
                        "mark-unknown",
                        mark.RecordId,
                        location,
                        symbol.Name,
                        mark.RecordId,
                        "matches no provision of the carried release");
                }
            }
            else
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unknown",
                    mark.RecordId,
                    location,
                    symbol.Name,
                    mark.RecordId,
                    "matches no record");
            }
        }
        else if (!MarkRules.Accepts(kind.Value, mark.RecordId))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unknown",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "is a " + record.Type + " record, not a " + Expected(kind.Value) + " record");
        }
        else if (kind == CodeKind.Test && record.Type == "requirement" && record.Field("kind") != "quality")
        {
            var field = record.Field("kind");
            var what = field is string held && held.Length > 0
                ? "a " + held + " requirement"
                : "a requirement with no kind";

            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unknown",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "is " + what + ", not a quality requirement");
        }
        else if (kind == CodeKind.Test && record.Type == "decision" && !state.Store.UnderImplementationDecisions(record.File))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unknown",
                mark.RecordId,
                location,
                symbol.Name,
                mark.RecordId,
                "is a product decision, not a technical decision");
        }

        if (mark.Verb != MarkRules.Verb(kind.Value))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5002,
                "mark-unknown",
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
    ///     a provision of the carried release, or a decision or exception that is
    ///     not retired. A <c>follows</c> mark is never stray.
    /// </summary>
    /// <param name="context">The symbol analysis context that carries the compilation state for this pass.</param>
    /// <param name="state">The traceability context holding the record store and the carried provision set.</param>
    /// <param name="symbol">The symbol that carries the follows mark under inspection.</param>
    /// <param name="mark">The follows mark read from the symbol declaration.</param>
    /// <param name="location">The source location of the mark, used as the diagnostic position.</param>
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
                    "mark-unknown",
                    id,
                    location,
                    symbol.Name,
                    id,
                    "matches no provision of the carried release");
            }

            return;
        }

        if (id.StartsWith("decision/", System.StringComparison.Ordinal)
            || id.StartsWith("exception/", System.StringComparison.Ordinal))
        {
            var record = state.Store.Index.ById(id);

            if (record is null || (record.Type != "decision" && record.Type != "exception"))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unknown",
                    id,
                    location,
                    symbol.Name,
                    id,
                    "matches no decision or exception");
            }
            else if (record.Status == "retired")
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5002,
                    "mark-unknown",
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
            "mark-unknown",
            id,
            location,
            symbol.Name,
            id,
            "matches no provision, decision, or exception");
    }

    /// <summary>Reports LS5004 when the mark uses a style other than the declared one.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="mark">The mark read from the element.</param>
    /// <param name="location">The source location the diagnostic reports at.</param>
    private static void AnalyzeStyle(
        SymbolAnalysisContext context, TraceabilityContext state, ISymbol symbol, Mark mark, Location location)
    {
        if (mark.Style != state.Store.Settings.Marks)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5004,
                "mark-style-invalid",
                mark.HasRecordId ? mark.RecordId : null,
                location,
                symbol.Name,
                mark.RecordId,
                mark.Style.ToProjectValue(),
                state.Store.Settings.Marks.ToProjectValue());
        }
    }

    /// <summary>Lists the record types the kind takes, joined as prose for a diagnostic message.</summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <returns>The string as text.</returns>
    private static string Expected(CodeKind kind) =>
        string.Join(" or ", MarkRules.RecordTypes(kind));
}
