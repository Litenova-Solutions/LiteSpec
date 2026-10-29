using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers;

/// <summary>
///     LS2009: a result part is named after the use-case result it belongs to. A result part is a type the result
///     reaches through its properties, directly or through another part, declared beside it; a helper the handler
///     uses is not a part.
/// </summary>
/// <follows>litespec/workspace/naming#name-result-parts-after-their-result</follows>
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
        var result = (INamedTypeSymbol)context.Symbol;
        if (result.ContainingType is not null || !IsResult(result.Name))
        {
            return;
        }

        foreach (var part in PartsOf(result))
        {
            if (!part.Name.StartsWith(result.Name, StringComparison.Ordinal) && part.Locations.Length > 0 && part.Locations[0].IsInSource)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS2009, part.Locations[0], part.Name, result.Name));
            }
        }
    }

    // Every type declared in the result's namespace that the result reaches through public properties,
    // following collections, nullables and arrays, and following each part's own properties in turn.
    private static IEnumerable<INamedTypeSymbol> PartsOf(INamedTypeSymbol result)
    {
        var space = result.ContainingNamespace;
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default) { result };
        var pending = new Stack<INamedTypeSymbol>();
        pending.Push(result);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                foreach (var reached in TypesIn(property.Type))
                {
                    if (reached.ContainingType is null
                        && SymbolEqualityComparer.Default.Equals(reached.ContainingNamespace, space)
                        && seen.Add(reached))
                    {
                        pending.Push(reached);
                        yield return reached;
                    }
                }
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> TypesIn(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                foreach (var element in TypesIn(array.ElementType))
                {
                    yield return element;
                }

                break;
            case INamedTypeSymbol named:
                yield return named.OriginalDefinition;
                foreach (var argument in named.TypeArguments)
                {
                    foreach (var inner in TypesIn(argument))
                    {
                        yield return inner;
                    }
                }

                break;
        }
    }

    private static bool IsResult(string name) =>
        name.EndsWith("QueryResult", StringComparison.Ordinal)
        || name.EndsWith("CommandResult", StringComparison.Ordinal);
}
