using System.Collections.Generic;
using System.Collections.Immutable;
using LiteSpec.Analyzers.Layers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Style;

/// <summary>LS3004: production C# seals every concrete class that nothing derives from.</summary>
/// <follows>litespec/backend/dotnet/naming#keep-implementation-style-consistent</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SealedClassAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS3004);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The application layer the compilation belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(Layer layer) => layer != Layer.None && layer != Layer.Test;

    /// <summary>Registers the analysis actions for the sealed class analyzer rule with the compilation context.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context) =>
        context.RegisterCompilationEndAction(Analyze);

    /// <summary>Analyzes the void.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private static void Analyze(CompilationAnalysisContext context)
    {
        var types = new List<INamedTypeSymbol>();
        CollectTypes(context.Compilation.Assembly.GlobalNamespace, types);

        var derived = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var type in types)
        {
            if (type.TypeKind == TypeKind.Class && type.BaseType is not null)
            {
                derived.Add(type.BaseType.OriginalDefinition);
            }
        }

        foreach (var type in types)
        {
            if (IsSealableLeaf(type) && !derived.Contains(type.OriginalDefinition))
            {
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS3004, type.Locations[0], type.Name));
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

    /// <summary>Returns true when the type is sealable leaf; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the type is sealable leaf; otherwise false.</returns>
    private static bool IsSealableLeaf(INamedTypeSymbol type)
    {
        if (type.DeclaringSyntaxReferences.Length == 0
            || type.IsImplicitlyDeclared
            || type.TypeKind != TypeKind.Class
            || type.IsRecord
            || type.IsAbstract
            || type.IsStatic
            || type.IsSealed)
        {
            return false;
        }

        // The WebApi composition marker carries no behavior to extend.
        if (type.Name == "Program")
        {
            return false;
        }

        return !DesignedForExtension(type);
    }

    /// <summary>Returns true when the designed-for-extension condition holds; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the designed-for-extension condition holds; otherwise false.</returns>
    private static bool DesignedForExtension(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsVirtual || (member.IsOverride && !member.IsSealed) || IsProtected(member))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the member is protected; otherwise false.</summary>
    /// <param name="member">The member declaration under inspection.</param>
    /// <returns>True when the member is protected; otherwise false.</returns>
    private static bool IsProtected(ISymbol member) =>
        member.DeclaredAccessibility == Accessibility.Protected
        || member.DeclaredAccessibility == Accessibility.ProtectedOrInternal
        || member.DeclaredAccessibility == Accessibility.ProtectedAndInternal;
}
