using System;
using System.Collections.Generic;
using LiteSpec.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace LiteSpec.Analyzers.LayerMap;

/// <summary>
///     Maps every source file of one compilation to its architectural home: its
///     layer, its module, and its use case. Built once
///     per compilation; the boundary rules read it for every reference.
/// </summary>
/// <follows>litespec/backend/architecture#point-dependencies-inward</follows>
internal sealed class CodeMap
{
    /// <summary>Folder names that never hold a use case of their own, and never name a module.</summary>
    private static readonly HashSet<string> KnownFolders = new(StringComparer.Ordinal)
    {
        "Domain",
        "Application",
        "Ports",
        "Infrastructure",
        "WebApi",
        "Contracts",
        "Composition",
        "Notifications",
        "ExternalSystems",
        "ReadModels",
        "Settings",
        "Events",
        "Exceptions",
        "Persistence",
    };

    /// <summary>Application folders shared by every use case of the module, which name no use case.</summary>
    private static readonly HashSet<string> SharedApplicationFolders = new(StringComparer.Ordinal)
    {
        "Notifications",
        "ExternalSystems",
        "ReadModels",
        "Settings",
    };

    /// <summary>Initializes a new instance of the <see cref="CodeMap"/> class.</summary>
    /// <param name="assemblyName">The assembly name that names the module or modules project.</param>
    /// <param name="modules">The module folders, as forward-slash directory paths.</param>
    /// <param name="root">The modules or module project root, or null when the compilation is neither.</param>
    /// <param name="folders">The folders holding source files directly, as forward-slash directory paths.</param>
    private CodeMap(string assemblyName, HashSet<string> modules, string? root, HashSet<string> folders)
    {
        AssemblyName = assemblyName;
        Modules = modules;
        Root = root;
        FileFolders = folders;
    }

    /// <summary>Gets the assembly name that names the module or modules project.</summary>
    public string AssemblyName { get; }

    /// <summary>Gets the module folders, as forward-slash directory paths.</summary>
    private HashSet<string> Modules { get; }

    /// <summary>Gets the modules or module project root, or null when the compilation is neither.</summary>
    private string? Root { get; }

    /// <summary>Gets the folders holding source files directly, as forward-slash directory paths.</summary>
    private HashSet<string> FileFolders { get; }

    /// <summary>Gets a value indicating whether the compilation is one module project: the project root itself is the module.</summary>
    public bool IsModuleProject => Root is not null && Modules.Contains(Root);

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

        var assemblyName = compilation.AssemblyName ?? string.Empty;
        var root = ModuleProjectRootOf(paths, assemblyName)
            ?? ApplicationRootOf(paths, assemblyName);

        return new CodeMap(
            assemblyName,
            FindModules(paths, assemblyName),
            root,
            FileFoldersOf(paths));
    }

    /// <summary>Returns the layer of the file: Application for a Contracts file, otherwise the resolver's layer.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The layer.</returns>
    public ArchitectureLayer LayerOf(string? filePath)
    {
        if (IsContracts(filePath) || IsContractsAssembly(AssemblyName))
        {
            return ArchitectureLayer.Application;
        }

        return ArchitectureLayerResolver.FileLayer(filePath, AssemblyName, declared: null);
    }

    /// <summary>Returns the module folder holding the file, or null for shared and host files. A file in a sibling Contracts project maps to its owner module.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The module folder, or null when no module holds the file.</returns>
    public string? ModuleOf(string? filePath)
    {
        var folder = FolderOf(filePath);
        if (folder is null)
        {
            return null;
        }

        folder = OwnerFolder(folder);

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

    /// <summary>Returns the aggregate folder holding the file, or null when no aggregate does.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The aggregate folder, or null when the file sits outside every aggregate.</returns>
    public string? AggregateOf(string? filePath) =>
        Aggregates.AggregateOf(filePath, Modules);

    /// <summary>
    ///     Returns the use case key holding the file: its module folder and use case folder,
    ///     or null when the file sits in no use case folder. A file's use case is the first
    ///     folder below Application or WebApi in its module that holds files,
    ///     shared folders aside: a module-level use case sits directly below the layer folder,
    ///     while a use case of an aggregate sits below its aggregate folder, which holds none.
    ///     Ports and Infrastructure hold shared folders only, so their files sit in no use case.
    ///     One key reads across the layers that hold the use case.
    /// </summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>The use case key, or null when the file sits outside every use case folder.</returns>
    public string? UseCaseOf(string? filePath)
    {
        var folder = FolderOf(filePath);
        if (folder is null || ModuleOf(filePath) is not string module)
        {
            return null;
        }

        if (!folder.StartsWith(module + "/", StringComparison.Ordinal))
        {
            return null;
        }

        var rest = folder.Substring(module.Length + 1).Split('/');
        if (rest.Length < 2 || !IsUseCaseLayer(rest[0], rest[1]))
        {
            return null;
        }

        var key = module;
        var path = module + "/" + rest[0];
        for (var index = 1; index < rest.Length; index++)
        {
            path += "/" + rest[index];
            key += "/" + rest[index];
            if (FileFolders.Contains(path))
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>Returns true when the layer folder holds use cases and the child folder names no shared concern; otherwise false.</summary>
    /// <param name="layer">The layer folder name.</param>
    /// <param name="child">The child folder name.</param>
    /// <returns>True when a use case may sit below the child folder.</returns>
    private static bool IsUseCaseLayer(string layer, string child)
    {
        if (layer.Equals("Application", StringComparison.Ordinal))
        {
            return !SharedApplicationFolders.Contains(child);
        }

        return layer.Equals("WebApi", StringComparison.Ordinal);
    }

    /// <summary>Returns true when the file sits in an Infrastructure adapter folder: below Infrastructure but outside Persistence; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when an adapter folder holds the file; otherwise false.</returns>
    public static bool IsAdapter(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        var segments = filePath!.Replace('\\', '/').Split('/');
        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (segments[index].Equals("Infrastructure", StringComparison.Ordinal))
            {
                return !segments[index + 1].Equals("Persistence", StringComparison.Ordinal);
            }
        }

        return false;
    }

    /// <summary>Returns the folders holding source files directly, as forward-slash directory paths.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <returns>The folders holding files directly.</returns>
    private static HashSet<string> FileFoldersOf(List<string> paths)
    {
        var folders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            if (FolderOf(path) is string folder)
            {
                folders.Add(folder);
            }
        }

        return folders;
    }

    /// <summary>Returns true when the file sits under a module Contracts folder or a sibling Contracts project; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <returns>True when the file sits under a module Contracts folder or a sibling Contracts project; otherwise false.</returns>
    public static bool IsContracts(string? filePath)
    {
        if (filePath is null || filePath.Length == 0)
        {
            return false;
        }

        foreach (var segment in filePath.Split('/', '\\'))
        {
            if (segment.Equals("Contracts", StringComparison.Ordinal) || IsContractsProjectName(segment))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns true when the assembly is a sibling Contracts project; otherwise false.</summary>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly name ends with .Contracts on a dotted owner name; otherwise false.</returns>
    internal static bool IsContractsAssembly(string assemblyName) =>
        IsContractsProjectName(assemblyName);

    /// <summary>Returns true when the folder names a sibling Contracts project; otherwise false.</summary>
    /// <param name="folder">The project folder name.</param>
    /// <returns>True when the folder ends with .Contracts on a dotted owner name, so a module named Contracts stays a module; otherwise false.</returns>
    internal static bool IsContractsProjectName(string folder)
    {
        const string suffix = ".Contracts";

        return folder.Length > suffix.Length
            && folder.EndsWith(suffix, StringComparison.Ordinal)
            && folder.Substring(0, folder.Length - suffix.Length).Contains(".");
    }

    /// <summary>Returns true when the file sits in the Platform project: the compilation builds it, or a path segment names it; otherwise false.</summary>
    /// <param name="filePath">The file path as the host reports it, or null when the document has none.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>True when the assembly or a path segment names the Platform project.</returns>
    internal static bool IsPlatformFile(string? filePath, string assemblyName)
    {
        if (assemblyName.Equals("Platform", StringComparison.Ordinal)
            || assemblyName.EndsWith(".Platform", StringComparison.Ordinal))
        {
            return true;
        }

        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        foreach (var segment in filePath!.Split('/', '\\'))
        {
            if (segment.Equals("Platform", StringComparison.Ordinal)
                || segment.EndsWith(".Platform", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Returns the folder with each sibling Contracts project segment replaced by its owner module segment.</summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <returns>The folder with .Contracts suffixes stripped from their segments.</returns>
    private static string OwnerFolder(string folder)
    {
        const string suffix = ".Contracts";
        var segments = folder.Split('/');

        for (var index = 0; index < segments.Length; index++)
        {
            if (IsContractsProjectName(segments[index]))
            {
                segments[index] = segments[index].Substring(0, segments[index].Length - suffix.Length);
            }
        }

        return string.Join("/", segments);
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

    /// <summary>Returns the display name of a module folder or use case key: its last segment.</summary>
    /// <param name="folder">The folder as a forward-slash directory path.</param>
    /// <returns>The last segment.</returns>
    public static string FolderName(string folder)
    {
        var slash = folder.LastIndexOf('/');
        return slash < 0 ? folder : folder.Substring(slash + 1);
    }

    /// <summary>Finds the module folders: parents of Contracts folders, owner modules of sibling Contracts projects, the module project root itself, or the layer-holding children of the modules project root.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="assemblyName">The compilation assembly name, which names the project folder.</param>
    /// <returns>The module folders, as forward-slash directory paths.</returns>
    private static HashSet<string> FindModules(List<string> paths, string assemblyName)
    {
        var modules = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var folder = FolderOf(path);
            if (folder is null)
            {
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

            // A sibling Contracts project belongs to its owner module: the project
            // folder with the .Contracts suffix stripped.
            var segments = folder.Split('/');
            for (var index = 0; index < segments.Length; index++)
            {
                if (IsContractsProjectName(segments[index]))
                {
                    segments[index] = segments[index].Substring(0, segments[index].Length - ".Contracts".Length);
                    modules.Add(string.Join("/", segments, 0, index + 1));
                    break;
                }
            }
        }

        // A module project is its own module, whether or not it publishes a Contracts folder yet.
        if (ModuleProjectRootOf(paths, assemblyName) is string moduleRoot)
        {
            modules.Add(moduleRoot);
            return modules;
        }

        // Every top-level folder of the modules project holding layer folders is a module,
        // whether or not it publishes a Contracts folder yet. Composition/ belongs to no
        // module: it names several modules at once.
        if (ApplicationRootOf(paths, assemblyName) is string root)
        {
            foreach (var path in paths)
            {
                if (FolderOf(path) is not string folder || !folder.StartsWith(root + "/", StringComparison.Ordinal))
                {
                    continue;
                }

                var relative = folder.Substring(root.Length + 1).Split('/');
                if (relative.Length < 2 || KnownFolders.Contains(relative[0]))
                {
                    continue;
                }

                if (relative[1] is "Contracts" or "Domain" or "Application" or "Ports" or "Infrastructure" or "WebApi")
                {
                    modules.Add(root + "/" + relative[0]);
                }
            }
        }

        return modules;
    }

    /// <summary>
    ///     Returns the module project root: the project folder of a compilation that holds
    ///     layer folders at its root, or null when the compilation is no module project.
    ///     The map records such a root as its own module, which opens module, use case and
    ///     aggregate detection for modules that publish no Contracts folder yet, and tells
    ///     a module project from the modules project.
    /// </summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="assemblyName">The compilation assembly name.</param>
    /// <returns>The root as a forward-slash directory path, or null.</returns>
    private static string? ModuleProjectRootOf(List<string> paths, string assemblyName)
    {
        if (IsModulesAssembly(assemblyName) || IsSharedKernelAssembly(assemblyName))
        {
            return null;
        }

        if (ProjectRootOf(paths, assemblyName) is not string root)
        {
            return null;
        }

        return HasChildFolder(paths, root, "Domain")
            || HasChildFolder(paths, root, "Application")
            || HasChildFolder(paths, root, "Ports")
            || HasChildFolder(paths, root, "Infrastructure")
            || HasChildFolder(paths, root, "WebApi")
            ? root
            : null;
    }

    /// <summary>Returns true when a source file sits in the child folder of the root; otherwise false.</summary>
    /// <param name="paths">The source file paths of the compilation.</param>
    /// <param name="root">The project root as a forward-slash directory path.</param>
    /// <param name="child">The child folder name.</param>
    /// <returns>True when a source file sits in the child folder; otherwise false.</returns>
    private static bool HasChildFolder(List<string> paths, string root, string child)
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
            if (slash > 0 && rest.Substring(0, slash).Equals(child, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
    ///     <c>Domain/</c>, <c>Application/</c>, <c>Ports/</c>, <c>Infrastructure/</c> or <c>WebApi/</c>.
    ///     A nested <c>Contracts/</c> folder marks a module only in the modules project.
    ///     The modules project, a host, a tool or a test project holds none there.
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
                if (first is "Domain" or "Application" or "Ports" or "Infrastructure" or "WebApi")
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
}
