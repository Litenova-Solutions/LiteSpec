using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5004: a mark is written in the style the project did not declare.
/// </summary>
/// <follows>litespec/backend/dotnet/stack#write-each-mark-in-the-declared-style</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MarkStyleAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5004);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.None;

    /// <summary>Registers the analysis actions for the mark style analyzer rule with the compilation context.</summary>
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

        Analyze(context, state, type);
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

        Analyze(context, state, method);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    private static void Analyze(SymbolAnalysisContext context, TraceabilityContext state, ISymbol symbol)
    {
        foreach (var member in Declarations(symbol))
        {
            foreach (var mark in MarkReader.Read(member))
            {
                AnalyzeMark(context, state, symbol, member, mark);
            }
        }
    }

    /// <summary>Reports LS5004 when the mark uses a style other than the declared one.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="state">The traceability context that carries the records and shared lookups for the compilation.</param>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="member">The member declaration under inspection.</param>
    /// <param name="mark">The mark read from the element.</param>
    private static void AnalyzeMark(
        SymbolAnalysisContext context,
        TraceabilityContext state,
        ISymbol symbol,
        MemberDeclarationSyntax member,
        Mark mark)
    {
        if (mark.Style != state.Records.Settings.Marks)
        {
            TraceabilityReporter.Report(
                state,
                context.ReportDiagnostic,
                Descriptors.LS5004,
                "mark-style-invalid",
                mark.HasRecordId ? mark.RecordId : null,
                MarkLocation(member, mark),
                symbol.Name,
                mark.RecordId,
                mark.Style.ToProjectValue(),
                state.Records.Settings.Marks.ToProjectValue());
        }
    }
}
