using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using LiteSpec.Model;
using LiteSpec.Model.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Traceability;

/// <summary>
///     LS5006: a marked domain event or rule exception is not a partial type, so
///     the generator cannot write its runtime value.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PartialTypeAnalyzer : TraceabilityAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS5006);

    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    private protected override void RegisterTraceability(
        CompilationStartAnalysisContext context, TraceabilityContext state) =>
        context.RegisterSymbolAction(c => Analyze(c, state), SymbolKind.NamedType);

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

        if (MarksOf(type).Count == 0 || IsPartial(type))
        {
            return;
        }

        var subject = FirstRecordId(type);

        TraceabilityReporter.Report(
            state,
            context.ReportDiagnostic,
            Descriptors.LS5006,
            "partial-missing",
            subject,
            type.Locations[0],
            type.Name);
    }

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

    private static bool IsPartial(INamedTypeSymbol type)
    {
        foreach (var member in Declarations(type))
        {
            if (member is TypeDeclarationSyntax declared
                && declared.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
