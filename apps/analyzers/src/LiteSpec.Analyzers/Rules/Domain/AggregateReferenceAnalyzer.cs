using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1016: a property of one aggregate is never typed as another aggregate's root or entity.</summary>
/// <follows>litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AggregateReferenceAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1016);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the aggregate reference analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSymbolAction(c => AnalyzeProperty(c, scope, map), SymbolKind.Property);
    }

    /// <summary>Analyzes the property.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeProperty(SymbolAnalysisContext context, FileScope scope, CodeMap map)
    {
        var property = (IPropertySymbol)context.Symbol;
        if (property.IsIndexer || !scope.AppliesTo(context.Symbol))
        {
            return;
        }

        if (FirstSourceFile(property) is not string file || map.AggregateOf(file) is not string aggregate)
        {
            return;
        }

        foreach (var candidate in CandidatesOf(property.Type))
        {
            if (CodeMap.SourceFileOf(candidate, context.Compilation) is not string declared
                || map.AggregateOf(declared) is not string other
                || other.Equals(aggregate, StringComparison.Ordinal))
            {
                continue;
            }

            if (Aggregates.IsRoot(candidate) || Aggregates.IsEntity(candidate))
            {
                Report(context, property, candidate, other);
                return;
            }
        }
    }

    /// <summary>Reports the object reference.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="property">The property under inspection.</param>
    /// <param name="candidate">The referenced root or entity.</param>
    /// <param name="other">The aggregate folder holding the referenced type.</param>
    private static void Report(SymbolAnalysisContext context, IPropertySymbol property, INamedTypeSymbol candidate, string other)
    {
        foreach (var location in property.Locations)
        {
            if (location.IsInSource)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS1016, location, property.Name, candidate.Name, CodeMap.FolderName(other)));
                return;
            }
        }
    }

    /// <summary>Returns the property type with its nullable, array and generic wrappers unwrapped.</summary>
    /// <param name="type">The property type.</param>
    /// <returns>The candidate named types, without duplicates.</returns>
    private static List<INamedTypeSymbol> CandidatesOf(ITypeSymbol type)
    {
        var candidates = new List<INamedTypeSymbol>();
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        Collect(type, candidates, seen);
        return candidates;
    }

    /// <summary>Collects the candidate.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="candidates">The candidates collected so far, in discovery order.</param>
    /// <param name="seen">The candidates already collected.</param>
    private static void Collect(ITypeSymbol type, List<INamedTypeSymbol> candidates, HashSet<INamedTypeSymbol> seen)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                Collect(array.ElementType, candidates, seen);
                return;
            case INamedTypeSymbol named:
                if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    && named.TypeArguments.Length == 1)
                {
                    Collect(named.TypeArguments[0], candidates, seen);
                    return;
                }

                if (seen.Add(named.OriginalDefinition))
                {
                    candidates.Add(named);
                }

                foreach (var argument in named.TypeArguments)
                {
                    Collect(argument, candidates, seen);
                }

                return;
            default:
                return;
        }
    }

    /// <summary>Returns the first source file declaring the property, or null for metadata.</summary>
    /// <param name="property">The property under inspection.</param>
    /// <returns>The source file path, or null when the property comes from metadata.</returns>
    private static string? FirstSourceFile(IPropertySymbol property)
    {
        foreach (var reference in property.DeclaringSyntaxReferences)
        {
            if (!string.IsNullOrEmpty(reference.SyntaxTree.FilePath))
            {
                return reference.SyntaxTree.FilePath;
            }
        }

        return null;
    }
}
