using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2024: a module holds no store, serializer, guard or converter code the platform provides.</summary>
/// <follows>litespec/packs/dotnet/architecture#plug-modules-into-the-platform-through-contributions</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PlatformOwnershipAnalyzer : LayerAnalyzer
{
    /// <summary>The store and serializer setup types the platform owns, by simple name.</summary>
    private static readonly string[] StoreSetupTypes =
    [
        "StoreOptions",
        "DocumentStore",
        "DbContextOptionsBuilder",
        "NpgsqlDataSourceBuilder",
    ];

    /// <summary>The guard contracts the platform guard implements, by simple name.</summary>
    private static readonly string[] GuardContracts =
    [
        "DeclarationGuard",
        "ICommandGuard",
        "IQueryGuard",
    ];

    /// <summary>The converter base the platform converters derive from, by simple name.</summary>
    private const string ConverterBase = "JsonConverter";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2024);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) =>
        layer != ArchitectureLayer.None && layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the platform ownership analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var map = CodeMap.For(context.Compilation, context.Options);
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope, map), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree) || map.ModuleOf(node.SyntaxTree.FilePath) is null)
        {
            return;
        }

        if (!References.TryResolve(context, node, IsStoreSetup, out var name, out _))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2024,
            node.Identifier.GetLocation(),
            name,
            "is store setup the platform owns; contribute stored types through the module entry type instead"));
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope, CodeMap map)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        if (FirstSourceFile(type) is not string file || map.ModuleOf(file) is null)
        {
            return;
        }

        if (ImplementsGuard(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2024,
                type.Locations[0],
                type.Name,
                "is a guard the platform owns; declare a policy in the use case AuthorizationDefinition instead"));
        }
        else if (DerivesConverter(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LS2024,
                type.Locations[0],
                type.Name,
                "is a converter the platform owns; use the platform converter instead"));
        }
    }

    /// <summary>Returns true when the type is platform-owned store setup; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when the simple name matches a store setup type.</returns>
    private static bool IsStoreSetup(INamedTypeSymbol type)
    {
        foreach (var setup in StoreSetupTypes)
        {
            if (type.Name.Equals(setup, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type implements a platform guard contract; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when a base or interface matches a guard contract.</returns>
    private static bool ImplementsGuard(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (IsGuardContract(current.Name))
            {
                return true;
            }
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (IsGuardContract(contract.Name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the type derives from the converter base; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>True when a base type matches the converter base.</returns>
    private static bool DerivesConverter(INamedTypeSymbol type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name.Equals(ConverterBase, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the name matches a guard contract; otherwise false.</summary>
    /// <param name="name">The name to convert or match.</param>
    /// <returns>True when the name matches a guard contract.</returns>
    private static bool IsGuardContract(string name)
    {
        foreach (var contract in GuardContracts)
        {
            if (name.Equals(contract, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the first source file declaring the type, or null for metadata.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <returns>The source file path, or null when the type comes from metadata.</returns>
    private static string? FirstSourceFile(INamedTypeSymbol type)
    {
        foreach (var reference in type.DeclaringSyntaxReferences)
        {
            if (!string.IsNullOrEmpty(reference.SyntaxTree.FilePath))
            {
                return reference.SyntaxTree.FilePath;
            }
        }

        return null;
    }
}
