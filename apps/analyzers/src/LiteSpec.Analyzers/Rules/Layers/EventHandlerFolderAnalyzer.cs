using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>LS2008: an event handler lives in the On{Fact} folder of the fact it handles.</summary>
/// <follows>litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EventHandlerFolderAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2008);

    private protected override bool AppliesTo(Layer layer) =>
        layer == Layer.Application || layer == Layer.Infrastructure || layer == Layer.Worker;

    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (!TryGetSingleFact(type, out var handled, out var fact))
        {
            return;
        }

        var space = type.ContainingNamespace;
        if (space is null
            || space.IsGlobalNamespace
            || !space.Name.Equals("On" + fact, StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2008, type.Locations[0], type.Name, handled.Name, fact));
        }
    }

    private static bool TryGetSingleFact(INamedTypeSymbol type, out INamedTypeSymbol handled, out string fact)
    {
        handled = null!;
        fact = string.Empty;
        INamedTypeSymbol? single = null;
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name != "IEventHandler" || contract.TypeArguments.Length != 1)
            {
                continue;
            }

            if (contract.TypeArguments[0] is not INamedTypeSymbol argument)
            {
                return false;
            }

            if (single is null)
            {
                single = argument;
            }
            else if (!SymbolEqualityComparer.Default.Equals(single, argument))
            {
                return false;
            }
        }

        if (single is null)
        {
            return false;
        }

        // A handler of a base interface fact handles every fact, which is stronger
        // than the two-fact exemption, so it takes no On{Fact} folder demand.
        if (single.TypeKind == TypeKind.Interface)
        {
            return false;
        }

        handled = single;
        fact = single.Name.EndsWith("Event", StringComparison.Ordinal)
            ? single.Name.Substring(0, single.Name.Length - "Event".Length)
            : single.Name;
        return true;
    }
}
