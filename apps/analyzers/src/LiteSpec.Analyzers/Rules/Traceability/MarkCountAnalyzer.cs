using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5003: an element carries more marks than its kind takes, or names one
///     record twice.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkCountAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5003);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None;

    /// <summary>Registers the analysis actions for the mark count analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => AnalyzeType(c, state, scope), SymbolKind.NamedType);
        context.RegisterSymbolAction(c => AnalyzeMethod(c, state, scope), SymbolKind.Method);
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, TraceabilityContext state, FileScope scope)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        if (type.IsImplicitlyDeclared || type.TypeKind == TypeKind.Error)
        {
            return;
        }

        Analyze(context, state, type, KindReader.KindOf(type, state.Kinds, state.ProjectOf(type)));
    }

    /// <summary>Analyzes the method.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void AnalyzeMethod(SymbolAnalysisContext context, TraceabilityContext state, FileScope scope)
    {
        var method = (IMethodSymbol)context.Symbol;

        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

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
        if (kind is null)
        {
            return;
        }

        var counted = new List<string>();

        foreach (var member in Declarations(symbol))
        {
            foreach (var mark in MarkReader.Read(member))
            {
                if (kind.Value == CodeKind.Technical
                    ? mark.Verb == MarkVerb.Implements || mark.Verb == MarkVerb.Follows
                    : mark.Verb != MarkVerb.Follows)
                {
                    counted.Add(mark.RecordId);
                }
            }
        }

        if (MarkRules.CheckCount(kind.Value, counted).Violated)
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
}
