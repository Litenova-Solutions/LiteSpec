using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>LS5001: a code element of a recognized kind carries no mark naming the record it serves.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkPresenceAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5001);

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

        var kind = KindReader.KindOf(type, state.Kinds, state.Project);

        if (kind is null || !CheckedIn(kind.Value, state.Layer))
        {
            return;
        }

        if (MarksOf(type).Count > 0)
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

        if (MarksOf(method).Count > 0)
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
    ///     checks handlers, event handlers and jobs; Infrastructure checks adapters
    ///     and jobs; WebApi checks endpoints; test projects check tests. Ports and
    ///     domain services are recognized only by their mark, so no layer checks
    ///     their absence.
    /// </summary>
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
                return kind is CodeKind.Handler or CodeKind.EventHandler or CodeKind.Job;
            case Layer.Infrastructure:
                return kind is CodeKind.Adapter or CodeKind.Job;
            case Layer.WebApi:
                return kind == CodeKind.Endpoint;
            case Layer.Test:
                return kind == CodeKind.Test;
            default:
                return false;
        }
    }

    private static string ExpectedTypes(CodeKind kind) =>
        string.Join(" or ", MarkRules.RecordTypes(kind));
}
