using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace LiteSpec.Analyzers.Layers;

/// <summary>
///     Whether a rule applies to one file: the rule's layer predicate over the
///     file's layer. Built once per compilation; every analysis action gates through it.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class FileScope
{
    /// <summary>Initializes a new instance of the <see cref="FileScope"/> class.</summary>
    /// <param name="assemblyName">The assembly name that names the modules project.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="declared">The declared layer name, or null when the project sets none.</param>
    /// <param name="applies">The rule's layer predicate.</param>
    private FileScope(string assemblyName, BackendLayout layout, string? declared, Func<ArchitectureLayer, bool> applies)
    {
        AssemblyName = assemblyName;
        Layout = layout;
        Declared = declared;
        Applies = applies;
    }

    /// <summary>Gets the assembly name that names the modules project.</summary>
    public string AssemblyName { get; }

    /// <summary>Gets the backend layout the consumer builds.</summary>
    public BackendLayout Layout { get; }

    /// <summary>Gets the declared layer name, or null when the project sets none.</summary>
    public string? Declared { get; }

    /// <summary>Gets the rule's layer predicate.</summary>
    private Func<ArchitectureLayer, bool> Applies { get; }

    /// <summary>Builds the scope for one compilation and one rule.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <param name="applies">The rule's layer predicate.</param>
    /// <returns>The scope.</returns>
    public static FileScope For(Compilation compilation, AnalyzerOptions options, Func<ArchitectureLayer, bool> applies) =>
        For(compilation.AssemblyName ?? string.Empty, DeclaredLayout(options), DeclaredLayer(options), applies);

    /// <summary>Builds the scope from its inputs.</summary>
    /// <param name="assemblyName">The assembly name that names the modules project.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="declared">The declared layer name, or null when the project sets none.</param>
    /// <param name="applies">The rule's layer predicate.</param>
    /// <returns>The scope.</returns>
    public static FileScope For(string assemblyName, BackendLayout layout, string? declared, Func<ArchitectureLayer, bool> applies) =>
        new(assemblyName, layout, declared, applies);

    /// <summary>Returns true when the rule applies to the tree's file; otherwise false.</summary>
    /// <param name="tree">The syntax tree, or null when the element has none.</param>
    /// <returns>True when the rule applies to the tree's file; otherwise false.</returns>
    public bool AppliesTo(SyntaxTree? tree) => Applies(LayerOf(tree));

    /// <summary>Returns true when the rule applies to the symbol's first source file; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>True when the rule applies to the symbol's first source file; otherwise false.</returns>
    public bool AppliesTo(ISymbol symbol) => Applies(LayerOf(symbol));

    /// <summary>Returns the layer of the tree's file.</summary>
    /// <param name="tree">The syntax tree, or null when the element has none.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer LayerOf(SyntaxTree? tree) =>
        ArchitectureLayerResolver.FileLayer(tree?.FilePath, AssemblyName, Layout, Declared);

    /// <summary>Returns the layer of the symbol's first source file, or None when it has none.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer LayerOf(ISymbol symbol)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            return LayerOf(reference.SyntaxTree);
        }

        return ArchitectureLayer.None;
    }

    /// <summary>Reads the declared layer property, or null when the project sets none.</summary>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <returns>The declared layer, or null.</returns>
    public static string? DeclaredLayer(AnalyzerOptions options)
    {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ArchitectureLayerResolver.PropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return declared.Trim();
        }

        return null;
    }

    /// <summary>Reads the declared layout property, or the module layout when the project sets none.</summary>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <returns>The layout.</returns>
    public static BackendLayout DeclaredLayout(AnalyzerOptions options)
    {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ArchitectureLayerResolver.LayoutPropertyKey, out var declared)
            && !string.IsNullOrWhiteSpace(declared))
        {
            return ArchitectureLayerResolver.ParseLayout(declared);
        }

        return BackendLayout.Modules;
    }
}
