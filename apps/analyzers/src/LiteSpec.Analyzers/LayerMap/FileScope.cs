using System;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     Whether a rule applies to one file: the rule's layer predicate over the
///     file's layer. Built once per compilation; every analysis action gates through it.
/// </summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
internal sealed class FileScope
{
    /// <summary>Initializes a new instance of the <see cref="FileScope"/> class.</summary>
    /// <param name="assemblyName">The assembly name that names the module or modules project.</param>
    /// <param name="applies">The rule's layer predicate.</param>
    private FileScope(string assemblyName, Func<ArchitectureLayer, bool> applies)
    {
        AssemblyName = assemblyName;
        Applies = applies;
    }

    /// <summary>Gets the assembly name that names the module or modules project.</summary>
    public string AssemblyName { get; }

    /// <summary>Gets the rule's layer predicate.</summary>
    private Func<ArchitectureLayer, bool> Applies { get; }

    /// <summary>Builds the scope for one compilation and one rule.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <param name="applies">The rule's layer predicate.</param>
    /// <returns>The scope.</returns>
    public static FileScope For(Compilation compilation, AnalyzerOptions options, Func<ArchitectureLayer, bool> applies) =>
        new(compilation.AssemblyName ?? string.Empty, applies);

    /// <summary>Returns true when the rule applies to the tree's file; otherwise false.</summary>
    /// <param name="tree">The syntax tree, or null when the element has none.</param>
    /// <returns>True when the rule applies to the tree's file; otherwise false.</returns>
    public bool AppliesTo(SyntaxTree? tree) => Applies(LayerOf(tree));

    /// <summary>Returns true when the rule applies to the symbol's first source file; otherwise false.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <returns>True when the rule applies to the symbol's first source file; otherwise false.</returns>
    public bool AppliesTo(ISymbol symbol) => Applies(LayerOf(symbol));

    /// <summary>Returns the layer of the tree's file: Application for a Contracts file, Domain for a knowledge model file, None for a knowledge set file, otherwise the resolver's layer.</summary>
    /// <param name="tree">The syntax tree, or null when the element has none.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer LayerOf(SyntaxTree? tree)
    {
        if (CodeMap.IsContracts(tree?.FilePath) || CodeMap.IsContractsAssembly(AssemblyName))
        {
            return ArchitectureLayer.Application;
        }

        if (Knowledge.IsModelFile(tree?.FilePath))
        {
            return ArchitectureLayer.Domain;
        }

        if (Knowledge.IsSetFile(tree?.FilePath))
        {
            return ArchitectureLayer.None;
        }

        return ArchitectureLayerResolver.FileLayer(tree?.FilePath, AssemblyName, declared: null);
    }

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
}
