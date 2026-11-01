using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>LS5001: a code element of a recognized kind carries no mark naming the record it serves.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
/// <follows>litespec/core/system#point-every-technical-class-up-to-its-record-or-choice</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkPresenceAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5001);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer != Layer.None;

    /// <summary>
    ///     Registers the analysis actions for the mark presence analyzer rule with the compilation
    ///     context.
    /// </summary>
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

        var kind = KindReader.KindOf(type, state.Kinds, state.Project);

        if (kind is null || !CheckedIn(kind.Value, state.Layer))
        {
            return;
        }

        if (HasKindMark(type, kind.Value))
        {
            return;
        }

        TraceabilityReporter.Report(
            state,
            context.ReportDiagnostic,
            Descriptors.LS5001,
            "mark-missing",
            subject: null,
            type.Locations[0],
            type.Name,
            ExpectedTypes(kind.Value));
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

        var kind = KindReader.KindOf(method, state.Kinds);

        if (kind is null || !CheckedIn(kind.Value, state.Layer))
        {
            return;
        }

        if (HasKindMark(method, kind.Value))
        {
            return;
        }

        TraceabilityReporter.Report(
            state,
            context.ReportDiagnostic,
            Descriptors.LS5001,
            "mark-missing",
            subject: null,
            method.Locations[0],
            method.Name,
            ExpectedTypes(kind.Value));
    }

    /// <summary>
    ///     Whether the layer checks the kind: Domain checks aggregate roots,
    ///     state-changing methods, domain events and rule exceptions; Application
    ///     checks handlers, event handlers, jobs, and technical classes;
    ///     Infrastructure checks adapters, jobs, and technical classes; WebApi
    ///     checks endpoints and technical classes; Worker checks jobs and
    ///     technical classes; test projects check tests.
    ///     Ports and domain services are recognized only by their mark, so no
    ///     layer checks their absence.
    /// </summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when the checked-in condition holds; otherwise false.</returns>
    private static bool CheckedIn(CodeKind kind, Layer layer)
    {
        switch (layer)
        {
            case Layer.Domain:
                return kind is CodeKind.AggregateRoot
                    or CodeKind.AggregateMethod
                    or CodeKind.DomainEvent
                    or CodeKind.RuleException;
            case Layer.Application:
            case Layer.ApplicationAbstractions:
                return kind is CodeKind.Handler or CodeKind.EventHandler or CodeKind.Job or CodeKind.Technical;
            case Layer.Infrastructure:
                return kind is CodeKind.Adapter or CodeKind.Job or CodeKind.Technical;
            case Layer.WebApi:
                return kind is CodeKind.Endpoint or CodeKind.Technical;
            case Layer.Worker:
                return kind is CodeKind.Job or CodeKind.Technical;
            case Layer.Test:
                return kind == CodeKind.Test;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Whether the symbol carries a mark in a verb its kind takes: a
    ///     <c>follows</c> mark never stands in for the <c>implements</c> or
    ///     <c>proves</c> mark the kind takes, except on a technical class, which
    ///     takes <c>implements</c> or <c>follows</c>.
    /// </summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <returns>True when the symbol has kind mark; otherwise false.</returns>
    private static bool HasKindMark(ISymbol symbol, CodeKind kind)
    {
        foreach (var mark in MarksOf(symbol))
        {
            if (MarkRules.TakesVerb(kind, mark.Verb))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the types.</summary>
    /// <param name="kind">The LiteSpec kind of the element under inspection.</param>
    /// <returns>The types as text.</returns>
    private static string ExpectedTypes(CodeKind kind)
    {
        if (kind != CodeKind.Technical)
        {
            return string.Join(" or ", MarkRules.RecordTypes(kind));
        }

        var types = new System.Collections.Generic.List<string>(MarkRules.RecordTypes(kind));
        types.AddRange(MarkRules.FollowsRecordTypes);

        return string.Join(" or ", types);
    }
}
