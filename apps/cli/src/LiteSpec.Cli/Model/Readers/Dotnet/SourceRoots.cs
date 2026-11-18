using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Readers.Dotnet;

/// <summary>The backend source roots of a consumer, as the reader walks them.</summary>
/// <follows>litespec/core/system#point-code-and-tests-up-to-records</follows>
static class SourceRoots
{
    /// <summary>Layer folders that state the layer of the files under them, in either layout.</summary>
    static readonly HashSet<string> LayerFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi",
    };

    /// <summary>Module-level folders that state no layer: the public surface and the record homes they mirror. A notification, external system or view folder keeps its child, which names the record.</summary>
    static readonly HashSet<string> UnlayeredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contracts", "UseCases", "Notifications", "ExternalSystems", "ReadModels",
    };

    /// <summary>The short project name holding the file, or null when it sits under no backend root.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file path.</param>
    /// <returns>The folder name of the matching backend root, or null when the file sits under none of them.</returns>
    public static string? ProjectOf(ConsumerConfig config, string file)
    {
        foreach (var modules in config.ModulesRoots)
        {
            var abstractions = modules + ".Abstractions";

            if (abstractions.Length > ".Abstractions".Length && IsUnder(abstractions, file))
            {
                return FolderName(abstractions);
            }

            if (IsUnder(modules, file))
            {
                return FolderName(modules);
            }
        }

        if (IsUnder(config.KernelRoot, file))
        {
            return FolderName(config.KernelRoot);
        }

        if (IsUnder(config.PlatformRoot, file))
        {
            return FolderName(config.PlatformRoot);
        }

        if (IsUnder(config.DomainRoot, file))
        {
            return FolderName(config.DomainRoot);
        }

        foreach (var root in config.WebApiRoots)
        {
            if (IsUnder(root, file))
            {
                return FolderName(root);
            }
        }

        if (IsUnder(config.InfrastructureRoot, file))
        {
            return FolderName(config.InfrastructureRoot);
        }

        foreach (var root in config.WorkerRoots)
        {
            if (IsUnder(root, file))
            {
                return FolderName(root);
            }
        }

        return null;
    }

    /// <summary>Every backend root the readers walk: the module, Kernel and Platform roots with their hosts, plus the layer projects they name.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The non-empty backend roots in read order, with the cross-module contracts part after each modules root.</returns>
    public static IReadOnlyList<string> BackendRoots(ConsumerConfig config)
    {
        var roots = new List<string>();
        var candidates = new List<string>();

        foreach (var modules in config.ModulesRoots)
        {
            candidates.Add(modules);
            candidates.Add(modules + ".Abstractions");
        }

        candidates.Add(config.KernelRoot);
        candidates.Add(config.PlatformRoot);
        candidates.Add(config.DomainRoot);
        candidates.AddRange(config.WebApiRoots);
        candidates.Add(config.InfrastructureRoot);
        candidates.AddRange(config.WorkerRoots);

        foreach (var root in candidates)
        {
            if (root.Length > 0 && root != ".Abstractions"
                && !roots.Contains(root, StringComparer.OrdinalIgnoreCase))
            {
                roots.Add(root);
            }
        }

        return roots;
    }

    /// <summary>Whether the file sits under any WebApi or worker host root.</summary>
    /// <param name="config">The consumer configuration naming the host roots.</param>
    /// <param name="file">The file path.</param>
    /// <returns>True when the file sits under a WebApi or worker host root.</returns>
    public static bool IsUnderHost(ConsumerConfig config, string file) =>
        config.WebApiRoots.Any(root => IsUnder(root, file))
        || config.WorkerRoots.Any(root => IsUnder(root, file));

    /// <summary>Whether the file sits under the Kernel or Platform root.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file path.</param>
    /// <returns>True when the file sits under the Kernel or Platform root.</returns>
    public static bool IsUnderKernelOrPlatform(ConsumerConfig config, string file) =>
        IsUnder(config.KernelRoot, file) || IsUnder(config.PlatformRoot, file);

    /// <summary>
    ///     The layer of one backend file, read from the shared resolver: under layer-projects from its
    ///     project suffix, otherwise from its nearest layer folder or its role suffix, with Contracts as
    ///     Application. Null outside every backend root or when no folder, suffix or project states one.
    /// </summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <param name="file">The file path.</param>
    /// <returns>The layer name, or null when no folder, suffix or project states one.</returns>
    public static string? LayerOf(ConsumerConfig config, string file)
    {
        string? root = null;
        var candidates = new List<string>();

        foreach (var modules in config.ModulesRoots)
        {
            candidates.Add(modules + ".Abstractions");
            candidates.Add(modules);
        }

        candidates.Add(config.KernelRoot);
        candidates.Add(config.PlatformRoot);
        candidates.Add(config.DomainRoot);
        candidates.AddRange(config.WebApiRoots);
        candidates.Add(config.InfrastructureRoot);
        candidates.AddRange(config.WorkerRoots);

        foreach (var candidate in candidates)
        {
            if (candidate.Length > 0 && candidate != ".Abstractions" && IsUnder(candidate, file))
            {
                root = candidate;
                break;
            }
        }

        if (root is null)
        {
            return null;
        }

        var layer = ArchitectureLayerResolver.FileLayer(
            PathText.Relative(root, file),
            FolderName(root),
            config.IsLayerProjects ? BackendLayout.LayerProjects : BackendLayout.Modules,
            declared: null);

        return layer switch
        {
            ArchitectureLayer.Domain => "Domain",
            ArchitectureLayer.ApplicationAbstractions => "ApplicationAbstractions",
            ArchitectureLayer.Application => "Application",
            ArchitectureLayer.Infrastructure => "Infrastructure",
            ArchitectureLayer.WebApi => "WebApi",
            ArchitectureLayer.Worker => "Worker",
            ArchitectureLayer.Test => "Test",
            _ => null,
        };
    }

    /// <summary>
    ///     The path segments of the file under its module layout, with the module first and the file name last,
    ///     or null when the file sits under no backend root, under the cross-module contracts part, under provider
    ///     plumbing, under the Kernel or Platform, under the modules composition folder, in a host under the module
    ///     layout, or in a layer root itself. Layer folders and module-level record folders state no module
    ///     segment, so they are left out and the first kept segment is the module folder. A leading WebApi
    ///     Endpoints folder is skipped, so the first segment is the module folder and the last is the file name.
    /// </summary>
    /// <param name="config">The consumer configuration that holds each layer root, the Kernel root and the Platform root.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <returns>The path segments with the module first, or null when the file sits outside the module layout.</returns>
    public static string[]? ModulePath(ConsumerConfig config, string file)
    {
        if (IsUnderKernelOrPlatform(config, file))
        {
            return null;
        }

        foreach (var modules in config.ModulesRoots)
        {
            var abstractions = modules + ".Abstractions";

            if (abstractions.Length > ".Abstractions".Length && IsUnder(abstractions, file))
            {
                return null;
            }
        }

        // Under the modules roots the WebApi and Worker roots are hosts, which
        // hold their endpoints and plumbing without a folder rule.
        var productLayout = IsProductLayout(config);

        foreach (var modules in config.ModulesRoots)
        {
            if (IsUnder(modules, file))
            {
                if (HasLayerSuffix(FolderName(modules)))
                {
                    return UnderLayerRoot(modules, file, skipEndpoints: false, skipPersistence: false);
                }

                return IsSingleModulesProject(modules)
                    ? UnderProductRoot(modules, file)
                    : UnderModuleProject(modules, file);
            }
        }

        if (IsUnder(config.DomainRoot, file))
        {
            return UnderLayerRoot(config.DomainRoot, file, skipEndpoints: false, skipPersistence: false);
        }

        foreach (var host in config.WebApiRoots)
        {
            if (IsUnder(host, file))
            {
                return productLayout || !HasLayerSuffix(FolderName(host))
                    ? null
                    : UnderLayerRoot(host, file, skipEndpoints: true, skipPersistence: false);
            }
        }

        if (IsUnder(config.InfrastructureRoot, file))
        {
            return UnderLayerRoot(config.InfrastructureRoot, file, skipEndpoints: false, skipPersistence: true);
        }

        foreach (var host in config.WorkerRoots)
        {
            if (IsUnder(host, file))
            {
                return productLayout || !HasLayerSuffix(FolderName(host))
                    ? null
                    : UnderLayerRoot(host, file, skipEndpoints: false, skipPersistence: false);
            }
        }

        return null;
    }

    /// <summary>The module project holding the file, or null when the file sits under no module project.</summary>
    /// <param name="config">The consumer configuration naming the module projects.</param>
    /// <param name="file">The file path.</param>
    /// <returns>The module project root holding the file, or null.</returns>
    public static string? ModuleProjectOf(ConsumerConfig config, string file)
    {
        foreach (var modules in config.ModulesRoots)
        {
            if (IsUnder(modules, file))
            {
                return modules;
            }
        }

        return null;
    }

    /// <summary>The module name a module project folder states: the part after the last dot, or null for the single modules project and layer projects.</summary>
    /// <param name="project">The module project root.</param>
    /// <returns>The module name, or null when the project holds every module or names a layer.</returns>
    public static string? ModuleNameOf(string project)
    {
        var folder = FolderName(project);

        if (folder.Length == 0 || HasLayerSuffix(folder) || IsSingleModulesProject(project))
        {
            return null;
        }

        var dot = folder.LastIndexOf('.');

        if (dot < 0 || dot + 1 >= folder.Length)
        {
            return folder;
        }

        return folder[(dot + 1)..];
    }

    /// <summary>Whether the project is the single modules project holding every module, the allowed deviation.</summary>
    /// <param name="project">The module project root.</param>
    /// <returns>True when the folder name ends with .Modules.</returns>
    public static bool IsSingleModulesProject(string project) =>
        FolderName(project).EndsWith(".Modules", StringComparison.Ordinal);

    /// <summary>Whether any module project is the one product project rather than a layer project.</summary>
    /// <param name="config">The consumer configuration naming the module projects.</param>
    /// <returns>True when any module project folder carries no layer suffix.</returns>
    public static bool IsProductLayout(ConsumerConfig config) =>
        config.ModulesRoots.Any(root => IsProductRoot(root));

    /// <summary>The product name: the Kernel, Platform, or module project folder without its suffix, or empty when no root is named.</summary>
    /// <param name="config">The consumer configuration naming the source roots.</param>
    /// <returns>The product name, or an empty string when no root names one.</returns>
    public static string ProductName(ConsumerConfig config)
    {
        var folders = new List<string>();

        void Add(string root)
        {
            if (root.Length == 0)
            {
                return;
            }

            var folder = FolderName(root);

            if (folder.Length == 0 || folder.StartsWith('.'))
            {
                return;
            }

            folders.Add(folder);
        }

        Add(config.KernelRoot);
        Add(config.PlatformRoot);

        foreach (var modules in config.ModulesRoots)
        {
            Add(modules);
        }

        Add(config.DomainRoot);

        foreach (var folder in folders)
        {
            foreach (var suffix in new[] { ".Application.Abstractions", ".Application", ".Domain", ".Infrastructure", ".WebApi", ".Worker", ".Modules", ".Kernel", ".Platform" })
            {
                if (folder.EndsWith(suffix, StringComparison.Ordinal))
                {
                    var product = folder[..^suffix.Length];

                    if (product.Length > 0)
                    {
                        return product;
                    }
                }
            }

            var dot = folder.LastIndexOf('.');

            if (dot > 0)
            {
                return folder[..dot];
            }

            if (folder.Length > 0)
            {
                return folder;
            }
        }

        return "";
    }

    /// <summary>Whether the file sits under the root.</summary>
    /// <param name="root">The source root.</param>
    /// <param name="file">The file path.</param>
    /// <returns>True when the file sits under the root.</returns>
    public static bool IsUnder(string root, string file)
    {
        if (root.Length == 0)
        {
            return false;
        }

        var relative = PathText.Relative(root, file);

        return !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    /// <summary>The folder name of a source root: the short project name.</summary>
    /// <param name="root">The source root.</param>
    /// <returns>The folder name of a source root.</returns>
    public static string FolderName(string root) =>
        Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>The path segments under one layer-project root, or null when the file sits in the root itself or under skipped plumbing.</summary>
    /// <param name="root">The layer-project root.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <param name="skipEndpoints">Whether a leading WebApi Endpoints folder is skipped.</param>
    /// <param name="skipPersistence">Whether files under provider plumbing are left out.</param>
    /// <returns>The path segments with the module first, or null when the file sits outside the module layout.</returns>
    static string[]? UnderLayerRoot(string root, string file, bool skipEndpoints, bool skipPersistence)
    {
        var segments = PathText.Relative(root, file).Split('/');

        if (segments.Length < 2)
        {
            return null;
        }

        var first = 0;

        if (skipEndpoints && segments[0].Equals("Endpoints", StringComparison.OrdinalIgnoreCase))
        {
            first = 1;
        }

        if (skipPersistence && segments[first].Equals("Persistence", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = segments.Skip(first).ToArray();

        return rest.Length >= 2 ? rest : null;
    }

    /// <summary>The path segments under the single modules root, with layer and record folders left out, or null outside any module.</summary>
    /// <param name="root">The single modules root holding every module.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <returns>The path segments with the module first and the file name last, or null when the file sits outside any module.</returns>
    static string[]? UnderProductRoot(string root, string file)
    {
        var segments = PathText.Relative(root, file).Split('/');

        if (segments.Length < 2)
        {
            return null;
        }

        // The composition folder belongs to no module: what names several
        // modules at once sits there without a folder rule.
        if (segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var kept = new List<string>();

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (LayerFolders.Contains(segments[index]) || UnlayeredFolders.Contains(segments[index]))
            {
                continue;
            }

            kept.Add(segments[index]);
        }

        kept.Add(segments[^1]);

        return kept.Count >= 2 ? [.. kept] : null;
    }

    /// <summary>The path segments under one module project, with the module first and the file name last, or null outside the module.</summary>
    /// <param name="root">The module project root holding one module.</param>
    /// <param name="file">The full path of the file whose segments are wanted.</param>
    /// <returns>The path segments with the module first and the file name last, or null when the file sits in the project root itself.</returns>
    static string[]? UnderModuleProject(string root, string file)
    {
        var segments = PathText.Relative(root, file).Split('/');

        if (segments.Length < 2)
        {
            return null;
        }

        if (segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var module = ModuleNameOf(root);

        if (module is null)
        {
            return null;
        }

        var kept = new List<string> { module };

        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (LayerFolders.Contains(segments[index]) || UnlayeredFolders.Contains(segments[index]))
            {
                continue;
            }

            kept.Add(segments[index]);
        }

        kept.Add(segments[^1]);

        return kept.Count >= 2 ? [.. kept] : null;
    }

    /// <summary>Whether the root is the one product project rather than a layer project.</summary>
    /// <param name="root">The source root path.</param>
    /// <returns>True when the root folder carries no layer suffix.</returns>
    public static bool IsProductRoot(string root) =>
        root.Length > 0 && !HasLayerSuffix(FolderName(root));

    /// <summary>Whether the project folder carries a layer suffix, so its files follow the layer-projects layout.</summary>
    /// <param name="project">The short project name.</param>
    /// <returns>True when the name is a layer name or ends in one.</returns>
    static bool HasLayerSuffix(string project) =>
        project.Length > 0 && ArchitectureLayerResolver.FromAssemblyName(project) != ArchitectureLayer.None;
}
