using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5007: an aggregate method marked with a use case raises an event the use
///     case does not list in <c>raises</c>. LS5008: a use case lists an event in
///     <c>raises</c> that no method marked with it raises.
/// </summary>
/// <follows>litespec/backend/domain#raise-the-declared-events-from-the-marked-act</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ActEventAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5007, Descriptors.LS5008);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>Registers the analysis actions for the act event analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        var collected = new List<MarkedAct>();
        var collectedLock = new object();

        context.RegisterSymbolAction(
            c => Collect(c, state, collected, collectedLock), SymbolKind.Method);
        context.RegisterCompilationEndAction(c => Analyze(c, state, collected));
    }

    /// <summary>Collects the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="collected">The marked acts collected so far, in discovery order.</param>
    /// <param name="collectedLock">The object that guards the collected list while symbols are visited.</param>
    private static void Collect(
        SymbolAnalysisContext context,
        TraceabilityContext state,
        List<MarkedAct> collected,
        object collectedLock)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (method.IsImplicitlyDeclared || method.MethodKind != MethodKind.Ordinary)
        {
            return;
        }

        if (KindReader.KindOf(method.ContainingType, state.Kinds, state.Project) != CodeKind.AggregateRoot)
        {
            return;
        }

        var useCases = UseCaseMarks(state, method);

        if (useCases.Count == 0)
        {
            return;
        }

        var raised = RaisedEventIds(state, method);

        lock (collectedLock)
        {
            collected.Add(new MarkedAct(method, useCases, raised));
        }
    }

    /// <summary>Every use-case mark on the method that resolves to a use-case record.</summary>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="method">The method under inspection.</param>
    /// <returns>The case marks, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<string> UseCaseMarks(TraceabilityContext state, IMethodSymbol method)
    {
        var useCases = new List<string>();

        foreach (var mark in MarksOf(method))
        {
            if (mark.HasRecordId
                && mark.RecordId.StartsWith("use-case/", System.StringComparison.Ordinal)
                && state.Store.Index.ById(mark.RecordId) is IRecordFacts record
                && record.Type == "use-case"
                && !useCases.Contains(mark.RecordId))
            {
                useCases.Add(mark.RecordId);
            }
        }

        return useCases;
    }

    /// <summary>
    ///     The event ids the method raises through the shared extraction, resolved
    ///     through each event type's <c>event/</c> mark.
    /// </summary>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="method">The method under inspection.</param>
    /// <returns>The event ids, in the order this method builds them, or an empty collection when none apply.</returns>
    private static List<string> RaisedEventIds(TraceabilityContext state, IMethodSymbol method)
    {
        var raised = new List<string>();

        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is MethodDeclarationSyntax declaration
                && declaration.Parent is TypeDeclarationSyntax parent)
            {
                foreach (var name in RaisedEvents.Of(declaration, parent))
                {
                    if (state.EventIds.TryGetValue(name, out var id) && !raised.Contains(id))
                    {
                        raised.Add(id);
                    }
                }
            }
        }

        return raised;
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="collected">The marked acts collected so far, in discovery order.</param>
    private static void Analyze(
        CompilationAnalysisContext context, TraceabilityContext state, List<MarkedAct> collected)
    {
        var useCases = collected
            .SelectMany(act => act.UseCases)
            .Distinct()
            .OrderBy(id => id, StringComparer.Ordinal);

        foreach (var useCase in useCases)
        {
            var methods = collected
                .Where(act => act.UseCases.Contains(useCase))
                .OrderBy(act => LocationPath(act))
                .ThenBy(act => LocationLine(act))
                .ThenBy(act => act.Method.Name, StringComparer.Ordinal)
                .ToList();
            var inputs = methods
                .Select(act => new ActMethodRaises(act.Raised))
                .ToList();

            foreach (var decided in ActEvents.Check(Raises(state, useCase), inputs))
            {
                var act = methods[decided.Method];

                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    decided.Code == "act-event-undeclared" ? Descriptors.LS5007 : Descriptors.LS5008,
                    decided.Code,
                    useCase,
                    act.Method.Locations[0],
                    act.Method.Name,
                    useCase,
                    decided.EventId);
            }
        }
    }

    /// <summary>The event ids the use case lists in raises, in record order.</summary>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="useCase">The use-case identifier to resolve.</param>
    /// <returns>The declared event ids for the use case, or an empty list when the use case names no record.</returns>
    private static IReadOnlyList<string> Raises(TraceabilityContext state, string useCase) =>
        state.Store.Index.ById(useCase)?.Ids("raises") ?? Array.Empty<string>();

    /// <summary>The file holding the method, or empty when it sits outside source.</summary>
    /// <param name="act">The marked act collected from the aggregate method.</param>
    /// <returns>The path as text.</returns>
    private static string LocationPath(MarkedAct act) =>
        act.Method.Locations[0].SourceTree?.FilePath ?? string.Empty;

    /// <summary>The 0-based line holding the method, or 0 when it sits outside source.</summary>
    /// <param name="act">The marked act collected from the aggregate method.</param>
    /// <returns>The line.</returns>
    private static int LocationLine(MarkedAct act)
    {
        var location = act.Method.Locations[0];

        return location.SourceTree is null
            ? 0
            : location.SourceTree.GetLineSpan(location.SourceSpan).StartLinePosition.Line;
    }

    /// <summary>Holds the marked act data the act event analyzer code works with.</summary>
    private sealed class MarkedAct
    {
        /// <summary>Initializes a new instance of the <see cref="MarkedAct"/> class.</summary>
        /// <param name="method">The method under inspection.</param>
        /// <param name="useCases">The use-case identifiers collected so far.</param>
        /// <param name="raised">The raised event identifiers collected so far.</param>
        public MarkedAct(IMethodSymbol method, IReadOnlyList<string> useCases, IReadOnlyList<string> raised)
        {
            Method = method;
            UseCases = useCases;
            Raised = raised;
        }

        /// <summary>Gets the method for the marked act.</summary>
        public IMethodSymbol Method { get; }

        /// <summary>Gets the use cases for the marked act.</summary>
        public IReadOnlyList<string> UseCases { get; }

        /// <summary>Gets the raised for the marked act.</summary>
        public IReadOnlyList<string> Raised { get; }
    }
}
