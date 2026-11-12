using System;
using System.Collections.Generic;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     Maps every source file of one compilation to its architectural home: its
///     layer, its module, and its use case or reaction folder. Built once
///     per compilation; the boundary rules read it for every reference.
/// </summary>
/// <follows>litespec/backend/architecture#point-dependencies-inward</follows>
internal sealed class CodeMap
{
    /// <summary>Folder names that never hold a use case or reaction of their own, and never name a module.</summary>
    private static readonly HashSet<string> KnownFolders = new(StringComparer.Ordinal)
    {
        "Domain",
        "Application",
        "Infrastructure",
        "WebApi",
        "Contracts",
        "Composition",
        "UseCases",
        "Reactions",
        "Jobs",
        "Events",
        "Exceptions",
    };

    /// <summary>Initializes a new instance of the <see cref="CodeMap"/> class.</summary>
    /// <param name="assemblyName">The assembly name that names the modules project.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="declared">The declared layer name, or null when the project sets none.</param>
    /// <param name="modules">The module folders, as forward-slash directory paths.</param>
    /// <param name="slices">The use case and reaction folders, as forward-slash directory paths.</param>
    private CodeMap(string assemblyName, BackendLayout layout, string? declared, HashSet<string> modules, HashSet<string> slices)
    {
        AssemblyName = assemblyName;
        Layout = layout;
        Declared = declared;
        Modules = modules;
        Slices = slices;
    }

    /// <summary>Gets the assembly name that names the modules project.</summary>
    public string AssemblyName { get; }

    /// <summary>Gets the backend layout the consumer builds.</summary>
    public BackendLayout Layout { get; }

    /// <summary>Gets the declared layer name, or null when the project sets none.</summary>
    public string? Declared { get; }

    /// <summary>Gets the module folders, as forward-slash directory paths.</summary>
    private HashSet<string> Modules { get; }

    /// <summary>Gets the use case and reaction folders, as forward-slash directory paths.</summary>
    private HashSet<string> Slices { get; }

    /// <summary>Builds the map for one compilation.</summary>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <param name="options">The analyzer options carrying the additional files.</param>
    /// <returns>The map.</returns>
    public static CodeMap For(Compilation compilation, AnalyzerOptions options)
    {
        var paths = new List<string>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (!string.IsNullOrEmpty(tree.FilePath))
            {
                paths.Add(tree.FilePath);
            }
        }

        var layout = FileScope.DeclaredLayout(options);
        var modules = FindModules(paths, layout, compilation.AssemblyName ?? string.Empty);
        return new CodeMap(
            compilation.AssemblyName ?? string.Empty,
            layout,
            FileScope.DeclaredLayer(options),
            modules,
            FindSlices(paths, layout, ApplicationRootOf(paths, compilation.AssemblyName ?? string.Empty)));
    }

    /// <summary>Returns the layer of the file.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer LayerOf(string? filePath) =>
        ArchitectureLayerResolver.FileLayer(filePath, AssemblyName, Layout, Declared);

    /// <summary>Returns the module folder holding the file, or null for shared and host files.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The module folder, or null when no module holds the file.</returns>
    public string? ModuleOf(string? filePath)
    {
        var folder = FolderOf(filePath);
        if (folder is null)
        {
            return null;
        }

        string? longest = null;
        foreach (var module in Modules)
        {
            if ((folder.Equals(module, StringComparison.Ordinal) || folder.StartsWith(module + "/", StringComparison.Ordinal))
                && (longest is null || module.Length > longest.Length))
            {
                longest = module;
            }
        }

        return longest;
    }

    /// <summary>Returns the use case or reaction folder holding the file, or null when none does.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The slice folder, or null when the file sits outside every slice.</returns>
    public string? SliceOf(string? filePath)
    {
        var folder = FolderOf(filePath);
        return folder is not null && Slices.Contains(folder) ? folder : null;
    }

    /// <summary>Returns true when the file sits under a module Contracts folder; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when the file sits under a module Contracts folder; otherwise false.</returns>
    public static bool IsContracts(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return false;
        }

        foreach (var segment in filePath.Split('/', '\\'))
        {
            if (segment.Equals("Contracts", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the source file declaring the symbol in this compilation, or null for metadata.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The source file path, or null when the symbol comes from metadata.</returns>
    public static string? SourceFileOf(ISymbol symbol, Compilation compilation)
    {
        foreach (var reference in symbol.DeclaringSyntaxReferences)
        {
            if (compilation.ContainsSyntaxTree(reference.SyntaxTree))
            {
                return reference.SyntaxTree.FilePath;
            }
        }

        if (symbol is not INamedTypeSymbol && symbol.ContainingType is INamedTypeSymbol containing)
        {
            foreach (var reference in containing.DeclaringSyntaxReferences)
            {
                if (compilation.ContainsSyntaxTree(reference.SyntaxTree))
                {
                    return reference.SyntaxTree.FilePath;
                }
            }
        }

        return null;
    }

    /// <summary>Returns the layer of the file declaring the symbol, or its assembly suffix for metadata.</summary>
    /// <param name="symbol">The symbol declared by the element under inspection.</param>
    /// <param name="compilation">The compilation whose types and references the check reads.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer ReferencedLayer(ISymbol symbol, Compilation compilation)
    {
        if (SourceFileOf(symbol, compilation) is string file)
        {
            return LayerOf(file);
        }

        var assembly = symbol.ContainingAssembly?.Name;
        if (string.IsNullOrEmpty(assembly))
        {
            assembly = symbol.ContainingType?.ContainingAssembly?.Name;
        }

        return ArchitectureLayerResolver.FromAssemblyName(assembly ?? string.Empty);
    }

    /// <summary>Returns the display name of a module or slice folder: its last segment.</summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <returns>The last segment.</returns>
    public static string FolderName(string folder)
    {
        var slash = folder.LastIndexOf('/');
        return slash < 0 ? folder : folder.Substring(slash + 1);
    }

    /// <summary>Finds the module folders: parents of Contracts folders, or project children under layer-projects.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="assemblyName">The modules assembly name, which names the modules project folder.</param>
    /// <returns>The module folders, as forward-slash directory paths.</returns>
    private static HashSet<string> FindModules(List<string> paths, BackendLayout layout, string assemblyName)
    {
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var folder = FolderOf(path);
            if (folder is null)
            {
                continue;
            }

            if (layout == BackendLayout.LayerProjects)
            {
                if (ProjectModuleOf(folder) is string module)
                {
                    modules.Add(module);
                }

                continue;
            }

            var marker = folder.IndexOf("/Contracts/", StringComparison.Ordinal);
            if (marker > 0)
            {
                modules.Add(folder.Substring(0, marker));
            }
            else if (folder.EndsWith("/Contracts", StringComparison.Ordinal))
            {
                modules.Add(folder.Substring(0, folder.Length - "/Contracts".Length));
            }
        }

        // Under the module layout every top-level folder of the modules project outside the
        // known folders is a module, whether or not it publishes a Contracts folder yet.
        // Composition/ belongs to no module: it names several modules at once.
        if (layout != BackendLayout.LayerProjects && ApplicationRootOf(paths, assemblyName) is string root)
        {
            foreach (var path in paths)
            {
                if (FolderOf(path) is not string folder || !folder.StartsWith(root + "/", StringComparison.Ordinal))
                {
                    continue;
                }

                var first = folder.Substring(root.Length + 1).Split('/')[0];
                if (!KnownFolders.Contains(first) && !first.Equals("obj", StringComparison.Ordinal) && !first.Equals("bin", StringComparison.Ordinal))
                {
                    modules.Add(root + "/" + first);
                }
            }
        }

        return modules;
    }

    /// <summary>Returns the modules project root, or null when the compilation is no modules project.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>The root as a forward-slash directory path, or null.</returns>
    private static string? ApplicationRootOf(List<string> paths, string assemblyName)
    {
        if (IsSharedKernelAssembly(assemblyName) || ProjectRootOf(paths, assemblyName) is not string root)
        {
            return null;
        }

        // The modules project holds Composition/ beside its modules and needs no marker;
        // any other project proves itself with product layer folders at its root.
        return IsModulesAssembly(assemblyName) || HoldsProductLayerFolder(paths, root) ? root : null;
    }

    /// <summary>Returns true when the assembly is the modules project; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly name ends with the Modules segment; otherwise false.</returns>
    private static bool IsModulesAssembly(string assemblyName) =>
        assemblyName.Equals("Modules", StringComparison.Ordinal)
        || assemblyName.EndsWith(".Modules", StringComparison.Ordinal);

    /// <summary>Returns true when the assembly is the shared kernel; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly name ends with the SharedKernel segment; otherwise false.</returns>
    private static bool IsSharedKernelAssembly(string assemblyName) =>
        assemblyName.Equals("SharedKernel", StringComparison.Ordinal)
        || assemblyName.EndsWith(".SharedKernel", StringComparison.Ordinal);

    /// <summary>
    ///     Returns true when a source file sits in a product layer folder directly under the root:
    ///     <c>Domain/</c>, <c>Application/</c> or <c>Infrastructure/</c>. The modules project,
    ///     a host, a tool or a test project holds none there.
    /// </summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="root">The project root as a forward-slash directory path.</param>
    /// <returns>True when the compilation holds product layer folders at its root.</returns>
    private static bool HoldsProductLayerFolder(List<string> paths, string root)
    {
        foreach (var path in paths)
        {
            var unified = path.Replace('\\', '/');
            if (!unified.StartsWith(root + "/", StringComparison.Ordinal))
            {
                continue;
            }

            var rest = unified.Substring(root.Length + 1);
            var slash = rest.IndexOf('/');
            if (slash > 0)
            {
                var first = rest.Substring(0, slash);
                if (first is "Domain" or "Application" or "Infrastructure")
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    ///     Returns the project folder: the folder named after the assembly, or else the
    ///     deepest folder holding every rooted, non-generated source file of the compilation.
    /// </summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>The project root as a forward-slash directory path, or null when none is found.</returns>
    private static string? ProjectRootOf(List<string> paths, string assemblyName)
    {
        // The modules project folder carries the assembly name: apps/api/src/{Product}.Modules/.
        if (assemblyName.Length > 0)
        {
            foreach (var path in paths)
            {
                var segments = path.Replace('\\', '/').Split('/');
                for (var index = segments.Length - 2; index >= 0; index--)
                {
                    if (segments[index].Equals(assemblyName, StringComparison.Ordinal))
                    {
                        return string.Join("/", segments, 0, index + 1);
                    }
                }
            }
        }

        string? root = null;
        foreach (var path in paths)
        {
            var unified = path.Replace('\\', '/');
            if (!System.IO.Path.IsPathRooted(unified) || unified.Contains("/obj/"))
            {
                continue;
            }

            var folder = FolderOf(unified)!;
            if (root is null)
            {
                root = folder;
                continue;
            }

            while (!(folder + "/").StartsWith(root + "/", StringComparison.Ordinal))
            {
                var slash = root.LastIndexOf('/');
                if (slash <= 0)
                {
                    return null;
                }

                root = root.Substring(0, slash);
            }
        }

        return root;
    }

    /// <summary>Finds the slice folders: folders directly under a UseCases/ folder, or folders of role-suffixed files under layer-projects.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="layout">The backend layout the consumer builds.</param>
    /// <param name="root">The modules project root, or null when the compilation is no modules project; under modules a slice sits below it, directly under a UseCases/ folder.</param>
    /// <returns>The slice folders, as forward-slash directory paths.</returns>
    private static HashSet<string> FindSlices(List<string> paths, BackendLayout layout, string? root)
    {
        var slices = new HashSet<string>(StringComparer.Ordinal);
        // Under layer-projects the use case folders sit in the layer projects; under modules only
        // the modules project holds them.
        if (root is null && layout != BackendLayout.LayerProjects)
        {
            return slices;
        }

        foreach (var path in paths)
        {
            var folder = FolderOf(path);
            if (folder is null)
            {
                continue;
            }

            // A slice is a folder directly under a UseCases/ folder, at module or
            // aggregate level, including On{Fact}/ reaction folders.
            if (layout != BackendLayout.LayerProjects)
            {
                if (ParentNameIs(folder, "UseCases"))
                {
                    slices.Add(folder);
                }

                continue;
            }

            if (KnownFolders.Contains(FolderName(folder)) || UnderLayerFolder(folder, root))
            {
                continue;
            }

            if (ArchitectureLayerResolver.RoleLayer(FileNameOf(path)) is not null)
            {
                slices.Add(folder);
            }
        }

        return slices;
    }

    /// <summary>Returns true when the folder sits directly under a folder of that name; otherwise false.</summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <param name="parent">The parent folder name.</param>
    /// <returns>True when the folder sits directly under a folder of that name; otherwise false.</returns>
    private static bool ParentNameIs(string folder, string parent)
    {
        var slash = folder.LastIndexOf('/');
        if (slash <= 0)
        {
            return false;
        }

        var before = folder.Substring(0, slash);
        var parentSlash = before.LastIndexOf('/');
        var name = parentSlash < 0 ? before : before.Substring(parentSlash + 1);
        return name.Equals(parent, StringComparison.Ordinal);
    }

    /// <summary>Returns the module folder of a layer-projects file: the folder under its project folder.</summary>
    /// <param name="folder">The file's folder as a forward-slash directory path.</param>
    /// <returns>The module folder, or null when the file sits at its project root.</returns>
    private static string? ProjectModuleOf(string folder)
    {
        var segments = folder.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (IsProjectFolder(segments[index]) && index + 1 < segments.Length)
            {
                return string.Join("/", segments, 0, index + 2);
            }
        }

        return null;
    }

    /// <summary>Returns true when the folder is a layer-projects project folder; otherwise false.</summary>
    /// <param name="segment">The path segment under inspection.</param>
    /// <returns>True when the folder is a layer-projects project folder; otherwise false.</returns>
    private static bool IsProjectFolder(string segment) =>
        segment.EndsWith(".Domain", StringComparison.Ordinal)
        || segment.EndsWith(".Application", StringComparison.Ordinal)
        || segment.EndsWith(".Infrastructure", StringComparison.Ordinal)
        || segment.EndsWith(".WebApi", StringComparison.Ordinal)
        || segment.EndsWith(".Worker", StringComparison.Ordinal);

    /// <summary>
    ///     Returns true when a layer folder lies between the project root and the folder: a use case
    ///     lives in its module or aggregate, never inside a product, module or aggregate layer folder
    ///     such as <c>Application/</c> or <c>WebApi/</c>.
    /// </summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <param name="root">The project root, or null when none is found.</param>
    /// <returns>True when the folder sits inside a layer folder.</returns>
    private static bool UnderLayerFolder(string folder, string? root)
    {
        var rest = root is not null && folder.StartsWith(root + "/", StringComparison.Ordinal)
            ? folder.Substring(root.Length + 1)
            : folder;

        foreach (var segment in rest.Split('/'))
        {
            if (KnownFolders.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the folder holding the file, as a forward-slash directory path.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The folder, or null when the path is null or empty.</returns>
    private static string? FolderOf(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return null;
        }

        var unified = filePath.Replace('\\', '/');
        var slash = unified.LastIndexOf('/');
        return slash < 0 ? string.Empty : unified.Substring(0, slash);
    }

    /// <summary>Returns the file name the path ends with.</summary>
    /// <param name="filePath">The file path as the host reports it.</param>
    /// <returns>The file name.</returns>
    private static string FileNameOf(string filePath)
    {
        var slash = Math.Max(filePath.LastIndexOf('/'), filePath.LastIndexOf('\\'));
        return slash < 0 ? filePath : filePath.Substring(slash + 1);
    }
}
