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

    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        var collected = new List<MarkedAct>();
        var collectedLock = new object();

        context.RegisterSymbolAction(
            c => Collect(c, state, collected, collectedLock), SymbolKind.Method);
        context.RegisterCompilationEndAction(c => Analyze(c, state, collected));
    }

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
    private static IReadOnlyList<string> UseCaseMarks(TraceabilityContext state, IMethodSymbol method)
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
    private static IReadOnlyList<string> RaisedEventIds(TraceabilityContext state, IMethodSymbol method)
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
    private static IReadOnlyList<string> Raises(TraceabilityContext state, string useCase) =>
        state.Store.Index.ById(useCase)?.Ids("raises") ?? Array.Empty<string>();

    /// <summary>The file holding the method, or empty when it sits outside source.</summary>
    private static string LocationPath(MarkedAct act) =>
        act.Method.Locations[0].SourceTree?.FilePath ?? string.Empty;

    /// <summary>The 0-based line holding the method, or 0 when it sits outside source.</summary>
    private static int LocationLine(MarkedAct act)
    {
        var location = act.Method.Locations[0];

        return location.SourceTree is null
            ? 0
            : location.SourceTree.GetLineSpan(location.SourceSpan).StartLinePosition.Line;
    }

    private sealed class MarkedAct
    {
        public MarkedAct(IMethodSymbol method, IReadOnlyList<string> useCases, IReadOnlyList<string> raised)
        {
            Method = method;
            UseCases = useCases;
            Raised = raised;
        }

        public IMethodSymbol Method { get; }

        public IReadOnlyList<string> UseCases { get; }

        public IReadOnlyList<string> Raised { get; }
    }
}
