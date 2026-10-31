using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1003 and LS1004: an aggregate exposes one State property over an abstract state record with sealed cases.</summary>
/// <follows>litespec/backend/dotnet/domain#model-every-aggregate-lifecycle-with-state-records</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AggregateStateAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1003, Descriptors.LS1004);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer == Layer.Domain;

    /// <summary>
    ///     Registers the analysis actions for the aggregate state analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterCompilationEndAction(Analyze);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(CompilationAnalysisContext context)
    {
        var types = new List<INamedTypeSymbol>();
        CollectTypes(context.Compilation.Assembly.GlobalNamespace, types);

        var aggregates = new List<INamedTypeSymbol>();
        foreach (var type in types)
        {
            if (IsAggregate(type))
            {
                aggregates.Add(type);
            }
        }

        if (aggregates.Count == 0)
        {
            return;
        }

        var stateBases = FindStateBases(types, aggregates);
        foreach (var aggregate in aggregates)
        {
            if (!HasAbstractRecordState(aggregate))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1003, aggregate.Locations[0], aggregate.Name));
            }
        }

        foreach (var type in types)
        {
            var stateBase = UnsealedCaseBase(type, stateBases);
            if (stateBase != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LS1004, type.Locations[0], type.Name, stateBase.Name));
            }
        }
    }

    /// <summary>Collects the types.</summary>
    /// <param name="space">The namespace that contains the types under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectTypes(INamespaceSymbol space, List<INamedTypeSymbol> types)
    {
        foreach (var member in space.GetTypeMembers())
        {
            CollectType(member, types);
        }

        foreach (var child in space.GetNamespaceMembers())
        {
            CollectTypes(child, types);
        }
    }

    /// <summary>Collects the type.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="types">The types collected so far, in discovery order.</param>
    private static void CollectType(INamedTypeSymbol type, List<INamedTypeSymbol> types)
    {
        types.Add(type);
        foreach (var nested in type.GetTypeMembers())
        {
            CollectType(nested, types);
        }
    }

    /// <summary>Returns true when the type is aggregate; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is aggregate; otherwise false.</returns>
    private static bool IsAggregate(INamedTypeSymbol type)
    {
        if (!IsDeclaredInSource(type) || type.TypeKind != TypeKind.Class || type.IsAbstract)
        {
            return false;
        }

        for (var current = type.BaseType; current != null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.TypeKind == TypeKind.Class && definition.Arity == 1 && definition.Name == "AggregateRoot")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the aggregate has abstract record state; otherwise false.</summary>
    /// <param name="aggregate">The aggregate type under inspection.</param>
    /// <returns>True when the aggregate has abstract record state; otherwise false.</returns>
    private static bool HasAbstractRecordState(INamedTypeSymbol aggregate)
    {
        var property = FindStateProperty(aggregate);
        return property != null
            && property.Type is INamedTypeSymbol state
            && state.IsRecord
            && state.TypeKind == TypeKind.Class
            && state.IsAbstract;
    }

    /// <summary>Returns the state property.</summary>
    /// <param name="aggregate">The aggregate type under inspection.</param>
    /// <returns>The state property, or null when none applies.</returns>
    private static IPropertySymbol? FindStateProperty(INamedTypeSymbol aggregate)
    {
        for (var current = aggregate; current != null; current = current.BaseType)
        {
            var definition = current.OriginalDefinition;
            if (definition.TypeKind == TypeKind.Class && definition.Arity == 1 && definition.Name == "AggregateRoot")
            {
                break;
            }

            foreach (var member in current.GetMembers("State"))
            {
                if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, current))
                {
                    continue;
                }

                if (member is IPropertySymbol property)
                {
                    return property;
                }
            }
        }

        return null;
    }

    /// <summary>Returns the state bases.</summary>
    /// <param name="types">The types collected so far, in discovery order.</param>
    /// <param name="aggregates">The aggregate types found in the compilation.</param>
    /// <returns>The state bases, in the order this method builds them, or an empty collection when none apply.</returns>
    private static HashSet<INamedTypeSymbol> FindStateBases(
        List<INamedTypeSymbol> types, List<INamedTypeSymbol> aggregates)
    {
        var expectedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var aggregate in aggregates)
        {
            expectedNames.Add(aggregate.Name + "State");
        }

        var propertyTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var aggregate in aggregates)
        {
            var property = FindStateProperty(aggregate);
            if (property != null
                && property.Type is INamedTypeSymbol state
                && state.IsRecord
                && state.TypeKind == TypeKind.Class
                && state.IsAbstract)
            {
                propertyTypes.Add(state);
            }
        }

        var bases = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var type in types)
        {
            if (type.IsRecord
                && type.TypeKind == TypeKind.Class
                && type.IsAbstract
                && type.Name.EndsWith("State", StringComparison.Ordinal)
                && (expectedNames.Contains(type.Name) || propertyTypes.Contains(type)))
            {
                bases.Add(type);
            }
        }

        return bases;
    }

    /// <summary>Returns the case base.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="stateBases">The state base types found for the aggregates.</param>
    /// <returns>The case base, or null when none applies.</returns>
    private static INamedTypeSymbol? UnsealedCaseBase(INamedTypeSymbol type, HashSet<INamedTypeSymbol> stateBases)
    {
        if (!IsDeclaredInSource(type) || type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsSealed)
        {
            return null;
        }

        var baseType = type.BaseType;
        return baseType != null && stateBases.Contains(baseType) ? baseType : null;
    }

    /// <summary>Returns true when the type is declared in source; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is declared in source; otherwise false.</returns>
    private static bool IsDeclaredInSource(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.Length != 0;
}
