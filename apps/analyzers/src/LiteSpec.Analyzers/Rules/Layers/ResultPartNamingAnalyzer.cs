using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2009: a result part is named after the use-case result it belongs to.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ResultPartNamingAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2009);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.ContainingType is not null)
        {
            return;
        }

        if (type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
        {
            return;
        }

        if (HasOwnName(type.Name))
        {
            return;
        }

        var space = type.ContainingNamespace;
        if (space is null || space.IsGlobalNamespace)
        {
            return;
        }

        string? result = null;
        foreach (var sibling in space.GetTypeMembers())
        {
            if (!IsResult(sibling.Name))
            {
                continue;
            }

            if (type.Name.StartsWith(sibling.Name, StringComparison.Ordinal))
            {
                return;
            }

            if (result is null || string.CompareOrdinal(sibling.Name, result) < 0)
            {
                result = sibling.Name;
            }
        }

        if (result is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2009, type.Locations[0], type.Name, result));
        }
    }

    private static bool HasOwnName(string name) =>
        name.EndsWith("CommandResult", StringComparison.Ordinal)
        || name.EndsWith("Command", StringComparison.Ordinal)
        || name.EndsWith("QueryResult", StringComparison.Ordinal)
        || name.EndsWith("Query", StringComparison.Ordinal)
        || name.EndsWith("Handler", StringComparison.Ordinal)
        || name.EndsWith("Validator", StringComparison.Ordinal)
        || name.EndsWith("Definition", StringComparison.Ordinal);

    private static bool IsResult(string name) =>
        name.EndsWith("QueryResult", StringComparison.Ordinal)
        || name.EndsWith("CommandResult", StringComparison.Ordinal);
}
