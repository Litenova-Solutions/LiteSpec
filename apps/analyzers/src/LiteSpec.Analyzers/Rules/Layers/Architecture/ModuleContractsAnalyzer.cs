using System;
using System.Collections.Immutable;
using LiteSpec.Analyzers.LayerMap;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.Rules.Layers.Architecture;

/// <summary>LS2029: a module file outside a provider adapter names no public query of another module, and a Platform file names no module identifier. LS2030: a Contracts project declares no interface.</summary>
/// <follows>litespec/backend/dotnet/architecture#follow-layer-direction-in-every-file</follows>
/// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleContractsAnalyzer : LayerAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.LS2029, Descriptors.LS2030);

    /// <summary>Returns true when this analyzer applies to the layer; otherwise false.</summary>
    /// <param name="layer">The architecture layer the file belongs to.</param>
    /// <returns>True when this analyzer applies to the layer; otherwise false.</returns>
    private protected override bool AppliesTo(ArchitectureLayer layer) => layer != ArchitectureLayer.Test;

    /// <summary>
    ///     Registers the analysis actions for the module contracts analyzer rule with the compilation
    ///     context.
    /// </summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    private protected override void Register(CompilationStartAnalysisContext context)
    {
        var map = CodeMap.For(context.Compilation, context.Options);
        var scope = FileScope.For(context.Compilation, context.Options, AppliesTo);
        var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        context.RegisterSyntaxNodeAction(c => AnalyzeName(c, scope, map, assemblyName), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        context.RegisterSymbolAction(c => AnalyzeType(c, scope, assemblyName), SymbolKind.NamedType);
    }

    /// <summary>Analyzes the name.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    private static void AnalyzeName(SyntaxNodeAnalysisContext context, FileScope scope, CodeMap map, string assemblyName)
    {
        var node = (SimpleNameSyntax)context.Node;
        if (!scope.AppliesTo(node.SyntaxTree))
        {
            return;
        }

        if (CodeMap.IsPlatformFile(node.SyntaxTree.FilePath, assemblyName))
        {
            AnalyzePlatformName(context, node, map);
            return;
        }

        if (map.ModuleOf(node.SyntaxTree.FilePath) is not string module || CodeMap.IsAdapter(node.SyntaxTree.FilePath))
        {
            return;
        }

        if (!References.TryResolve(context, node, type => IsPublicQueryOf(type, context.Compilation), out var name, out var type)
            || type is null
            || CodeMap.SourceFileOf(type, context.Compilation) is not string file
            || map.ModuleOf(file) is not string other
            || other.Equals(module, StringComparison.Ordinal))
        {
            return;
        }

        var provider = CodeMap.FolderName(other);
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2029,
            node.Identifier.GetLocation(),
            Where(scope, node, module),
            name,
            "which is a public query or result of module '" + provider + "'; only the adapter in 'Infrastructure/" + provider + "/' may name it"));
    }

    /// <summary>Analyzes the name in a Platform file.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="node">The name syntax under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    private static void AnalyzePlatformName(SyntaxNodeAnalysisContext context, SimpleNameSyntax node, CodeMap map)
    {
        if (!References.TryResolve(context, node, type => IsModuleIdentifier(type, map, context.Compilation), out var name, out _))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.LS2029,
            node.Identifier.GetLocation(),
            "Platform",
            name,
            "which is a module identifier; the Platform names no business identifier"));
    }

    /// <summary>Analyzes the type.</summary>
    /// <param name="context">The analysis context supplied by the analyzer host for the current element or compilation.</param>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    private static void AnalyzeType(SymbolAnalysisContext context, FileScope scope, string assemblyName)
    {
        if (!scope.AppliesTo(context.Symbol))
        {
            return;
        }

        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Interface)
        {
            return;
        }

        if (CodeMap.SourceFileOf(type, context.Compilation) is not string file)
        {
            return;
        }

        if (!CodeMap.IsContracts(file) && !CodeMap.IsContractsAssembly(assemblyName))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.LS2030, type.Locations[0], type.Name));
    }

    /// <summary>Returns where the diagnostic sits: the file's layer, or its module when no layer folder holds the file.</summary>
    /// <param name="scope">Whether the rule applies to each file.</param>
    /// <param name="node">The name syntax under inspection.</param>
    /// <param name="module">The module folder holding the file under inspection.</param>
    /// <returns>The layer name, or the module name when the file takes no layer.</returns>
    private static string Where(FileScope scope, SimpleNameSyntax node, string module)
    {
        var layer = scope.LayerOf(node.SyntaxTree);
        return layer == ArchitectureLayer.None ? "Module '" + CodeMap.FolderName(module) + "'" : layer.ToString();
    }

    /// <summary>Returns true when the type is a public query or result declared in a Contracts project; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when a Contracts use case folder declares the type.</returns>
    private static bool IsPublicQueryOf(INamedTypeSymbol type, Compilation compilation) =>
        CodeMap.SourceFileOf(type, compilation) is string file && Contracts.IsPublicQuery(file, type.Name);

    /// <summary>Returns true when the type is a module identifier: a type ending in Id declared in a module; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="map">The architectural home of every source file.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when a module file declares the identifier.</returns>
    private static bool IsModuleIdentifier(INamedTypeSymbol type, CodeMap map, Compilation compilation) =>
        type.Name.EndsWith("Id", StringComparison.Ordinal)
        && CodeMap.SourceFileOf(type, compilation) is string file
        && map.ModuleOf(file) is not null;
}
