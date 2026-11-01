using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5006: a marked domain event or rule exception is not a partial type, or is
///     nested in a type that is not partial, so the generator cannot write its runtime value.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#declare-marked-events-and-exceptions-as-partial-types</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialTypeAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5006);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>Registers the analysis actions for the partial type analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state) =>
        context.RegisterSymbolAction(c => Analyze(c, state), SymbolKind.NamedType);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private static void Analyze(SymbolAnalysisContext context, TraceabilityContext state)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (type.IsImplicitlyDeclared || type.TypeKind == TypeKind.Error)
        {
            return;
        }

        var kind = KindReader.KindOf(type, state.Kinds, state.Project);

        if (kind != CodeKind.DomainEvent && kind != CodeKind.RuleException)
        {
            return;
        }

        if (MarksOf(type).Count == 0)
        {
            return;
        }

        var subject = FirstRecordId(type);

        if (!PartialTypes.IsPartial(type))
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5006,
                "partial-missing",
                subject,
                type.Locations[0],
                type.Name);
        }

        for (var outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            if (!PartialTypes.IsPartial(outer))
            {
                TraceabilityReporter.Report(
                    state,
                    context.ReportDiagnostic,
                    Descriptors.LS5006,
                    "partial-missing",
                    subject,
                    outer.Locations[0],
                    outer.Name);
            }
        }
    }

    /// <summary>Returns the record id.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The record id as text, or null when none applies.</returns>
    private static string? FirstRecordId(INamedTypeSymbol type)
    {
        foreach (var mark in MarksOf(type))
        {
            if (mark.HasRecordId)
            {
                return mark.RecordId;
            }
        }

        return null;
    }
}
