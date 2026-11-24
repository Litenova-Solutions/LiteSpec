using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     The knowledge projects of one compilation: the knowledge model holding the rule types,
///     and the knowledge sets holding one variant's values each. A model project sits under a
///     <c>Knowledge/</c> folder named <c>&lt;Product&gt;.&lt;Area&gt;</c>; a set project sits
///     beside it named <c>&lt;Product&gt;.&lt;Area&gt;.&lt;Variant&gt;</c> and references its model.
///     Nothing outside <c>Knowledge/</c> is ever a model or a set.
/// </summary>
/// <follows>litespec/backend/knowledge#read-knowledge-through-its-model</follows>
internal static class Knowledge
{
    /// <summary>Returns the project folder below Knowledge holding the file, or null when no Knowledge folder does.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The project folder name, or null when the file sits outside every Knowledge folder.</returns>
    public static string? ProjectOf(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return null;
        }

        var segments = filePath!.Split('/', '\\');
        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (segments[index].Equals("Knowledge", StringComparison.Ordinal))
            {
                return segments[index + 1];
            }
        }

        return null;
    }

    /// <summary>Returns true when the project is a knowledge model: a two-segment product and area name; otherwise false.</summary>
    /// <param name="project">The project folder name.</param>
    /// <returns>True when the name holds a product and an area segment.</returns>
    public static bool IsModelProject(string project) => SegmentsOf(project) == 2;

    /// <summary>Returns true when the project is a knowledge set: a three-segment product, area and variant name; otherwise false.</summary>
    /// <param name="project">The project folder name.</param>
    /// <returns>True when the name holds a product, an area and a variant segment.</returns>
    public static bool IsSetProject(string project) =>
        SegmentsOf(project) == 3 && !project.EndsWith(".Contracts", StringComparison.Ordinal);

    /// <summary>Returns true when the file sits in a knowledge model project; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when a Knowledge folder holds the file in a model project.</returns>
    public static bool IsModelFile(string? filePath) =>
        ProjectOf(filePath) is string project && IsModelProject(project);

    /// <summary>Returns true when the file sits in a knowledge set project; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when a Knowledge folder holds the file in a set project.</returns>
    public static bool IsSetFile(string? filePath) =>
        ProjectOf(filePath) is string project && IsSetProject(project);

    /// <summary>Returns true when the compilation is a knowledge model project; otherwise false.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the assembly name is a model project holding source files below Knowledge.</returns>
    public static bool IsModelCompilation(Compilation compilation) =>
        IsModelProject(compilation.AssemblyName ?? string.Empty) && HoldsProject(compilation);

    /// <summary>Returns true when the compilation is a knowledge set project; otherwise false.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the assembly name is a set project holding source files below Knowledge and referencing its model.</returns>
    public static bool IsSetCompilation(Compilation compilation) =>
        IsSetProject(compilation.AssemblyName ?? string.Empty) && HoldsProject(compilation) && ReferencesModel(compilation);

    /// <summary>Returns the knowledge set holding the type, or null when no set does.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The set project or assembly name, or null when the type lives outside every set.</returns>
    public static string? SetOf(INamedTypeSymbol type, Compilation compilation)
    {
        if (CodeMap.SourceFileOf(type, compilation) is string file)
        {
            return IsSetFile(file) ? ProjectOf(file) : null;
        }

        var assembly = type.ContainingAssembly?.Name;
        if (string.IsNullOrEmpty(assembly) || !IsSetProject(assembly!))
        {
            return null;
        }

        // A referenced assembly counts as a set only within the compiling product (its first name segment)
        // and only when it references its parent model, so a vendor's Vendor.Area.Part assemblies never count.
        var product = ProductOf(compilation.AssemblyName);
        if (product.Length == 0 || !string.Equals(ProductOf(assembly), product, StringComparison.Ordinal))
        {
            return null;
        }

        var parent = assembly!.Substring(0, assembly.LastIndexOf('.'));
        return ReferencesParent(type, parent) ? assembly : null;
    }

    /// <summary>Returns the product segment of an assembly name: the text before the first dot, or empty when there is none.</summary>
    /// <param name="assembly">The assembly name, or null.</param>
    /// <returns>The product segment, or empty.</returns>
    private static string ProductOf(string? assembly)
    {
        var dot = assembly?.IndexOf('.') ?? -1;
        return dot > 0 ? assembly!.Substring(0, dot) : string.Empty;
    }

    /// <summary>Returns true when the compilation references its knowledge model: the parent of its set assembly name; otherwise false.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the compilation references its parent model assembly.</returns>
    private static bool ReferencesModel(Compilation compilation)
    {
        var assembly = compilation.AssemblyName ?? string.Empty;
        var dot = assembly.LastIndexOf('.');
        if (dot <= 0)
        {
            return false;
        }

        var parent = assembly.Substring(0, dot);
        return compilation.ReferencedAssemblyNames.Any(reference => reference.Name.Equals(parent, StringComparison.Ordinal));
    }

    /// <summary>Returns true when the candidate set assembly references its knowledge model; otherwise false.</summary>
    /// <param name="type">The type under inspection.</param>
    /// <param name="parent">The parent model assembly name.</param>
    /// <returns>True when the type's assembly references the parent model assembly.</returns>
    private static bool ReferencesParent(INamedTypeSymbol type, string parent)
    {
        var assembly = type.ContainingAssembly;
        if (assembly is null)
        {
            return false;
        }

        foreach (var module in assembly.Modules)
        {
            foreach (var referenced in module.ReferencedAssemblySymbols)
            {
                if (referenced.Name.Equals(parent, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>Returns true when a source file sits in the compilation's project folder below Knowledge; otherwise false.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>True when the compilation holds its own project folder below Knowledge.</returns>
    private static bool HoldsProject(Compilation compilation)
    {
        var assemblyName = compilation.AssemblyName ?? string.Empty;
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (assemblyName.Equals(ProjectOf(tree.FilePath), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the dot-separated segment count of the project name, or zero when a segment is empty.</summary>
    /// <param name="project">The project folder name.</param>
    /// <returns>The segment count, or zero when the name is malformed.</returns>
    private static int SegmentsOf(string project)
    {
        var parts = project.Split('.');
        foreach (var part in parts)
        {
            if (part.Length == 0)
            {
                return 0;
            }
        }

        return parts.Length;
    }
}
