using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Domain;

/// <summary>LS1007: Domain raises each fact as an immutable record named {Aggregate}{PastFact}Event.</summary>
/// <follows>litespec/backend/dotnet/domain#raise-immutable-domain-facts</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DomainEventAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS1007);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer == ArchitectureLayer.Domain;

    /// <summary>Registers the analysis actions for the domain event analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        context.RegisterSymbolAction(c => Analyze(c, scope), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    private static void Analyze(SymbolAnalysisContext context, FileScope scope)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind == TypeKind.Interface || !ImplementsDomainEvent(type))
        {
            return;
        }

        if (IsImmutableEventRecord(type))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS1007, type.Locations[0], type.Name));
    }

    /// <summary>Returns true when the implements-domain-event condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the implements-domain-event condition holds; otherwise false.</returns>
    private static bool ImplementsDomainEvent(INamedTypeSymbol type)
    {
        foreach (var contract in type.AllInterfaces)
        {
            if (contract.Name == "IDomainEvent")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type is immutable event record; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is immutable event record; otherwise false.</returns>
    private static bool IsImmutableEventRecord(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class || !type.IsRecord)
        {
            return false;
        }

        if (!type.Name.EndsWith("Event", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var member in type.GetMembers())
        {
            if (member is IPropertySymbol property
                && property.SetMethod is not null
                && !property.SetMethod.IsInitOnly)
            {
                return false;
            }

            if (member is IFieldSymbol field
                && !field.IsImplicitlyDeclared
                && !field.IsStatic
                && !field.IsReadOnly
                && !field.IsConst)
            {
                return false;
            }
        }

        return true;
    }
}
