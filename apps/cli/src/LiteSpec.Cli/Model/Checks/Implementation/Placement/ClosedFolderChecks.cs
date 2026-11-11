using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check: the shared kernel, the modules root, each module, aggregate and use case folder, and each frontend modules root hold only their fixed folders and the folders a declared record names.</summary>
/// <follows>litespec/backend/domain#organize-a-module-by-aggregate-and-concept</follows>
/// <follows>litespec/workspace/naming#group-event-handlers-by-the-fact-they-handle</follows>
internal static class ClosedFolderChecks
{
    /// <summary>Fixed folders at the modules root, whatever its modules declare.</summary>
    static readonly HashSet<string> ModulesRootFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Composition",
    };

    /// <summary>Fixed folders at the shared kernel root: one layer folder per layer.</summary>
    static readonly HashSet<string> SharedKernelFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi",
    };

    /// <summary>Fixed folders at a module, whatever its aggregates declare.</summary>
    static readonly HashSet<string> ModuleFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contracts", "Domain", "Application", "Infrastructure", "WebApi",
        "UseCases", "Jobs",
    };

    /// <summary>The kind folders a module Contracts folder holds: the sorted public surface.</summary>
    static readonly HashSet<string> ContractsKindFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identifiers", "Exceptions", "IntegrationEvents", "Commands", "Ports",
    };

    /// <summary>Fixed folders at an aggregate, whatever its use cases declare.</summary>
    static readonly HashSet<string> AggregateFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi", "UseCases",
    };

    /// <summary>
    ///     Reports every backend and frontend folder outside the closed folder sets, and every module and
    ///     aggregate folder that names no declared record. Inside a layer folder subfolders are free, a host
    ///     holds its endpoints and plumbing without a folder rule, and a project with no declared modules,
    ///     aggregates or use cases states no names, so those folders are left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every declared module, aggregate and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var modules = DeclaredModules(model);
        var aggregates = DeclaredAggregates(model);
        var useCases = DeclaredUseCases(model);
        var reactions = DeclaredReactionFolders(model);

        if (model.Config.ModulesRoot.Length > 0 && Directory.Exists(model.Config.ModulesRoot))
        {
            if (SourceRoots.IsProductRoot(model.Config.ModulesRoot))
            {
                CheckModulesRoot(model.Config, modules, aggregates, useCases, reactions, findings);
            }
            else
            {
                CheckLayerProject(model.Config, model.Config.ModulesRoot, modules, findings);
            }
        }

        if (model.Config.SharedKernelRoot.Length > 0 && Directory.Exists(model.Config.SharedKernelRoot))
        {
            CheckSharedKernelRoot(model.Config, findings);
        }

        foreach (var root in new[] { model.Config.DomainRoot, model.Config.InfrastructureRoot })
        {
            if (root.Length > 0 && Directory.Exists(root))
            {
                CheckLayerProject(model.Config, root, modules, findings);
            }
        }

        // Hosts hold their endpoints and plumbing without a folder rule in the module
        // layout; under layer-projects their module folders name declared modules.
        if (!SourceRoots.IsProductRoot(model.Config.ModulesRoot))
        {
            foreach (var root in model.Config.WebApiRoots.Concat(model.Config.WorkerRoots))
            {
                if (root.Length > 0 && Directory.Exists(root))
                {
                    CheckLayerProject(model.Config, root, modules, findings);
                }
            }
        }

        foreach (var frontend in model.Config.Frontends)
        {
            CheckFrontendRoot(model.Config, frontend, modules, findings);
        }
    }

    /// <summary>Reports every folder under the shared kernel root that breaks its closed set of layer folders.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSharedKernelRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.SharedKernelRoot))
        {
            var name = Path.GetFileName(child);

            if (SharedKernelFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no shared kernel layer folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the modules root that breaks its level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModulesRoot(ConsumerConfig config, List<string> modules, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        foreach (var module in ChildFolders(config.ModulesRoot))
        {
            var name = Path.GetFileName(module);

            if (ModulesRootFolders.Contains(name))
            {
                continue;
            }

            if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no Composition folder or declared module.", module, 1, null));
                continue;
            }

            CheckModuleLevel(config, module, name, aggregates, useCases, reactions, findings);
        }
    }

    /// <summary>Reports every folder under one module that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="module">The absolute module folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ConsumerConfig config, string module, string moduleName, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        foreach (var child in ChildFolders(module))
        {
            var name = Path.GetFileName(child);

            if (ModuleFolders.Contains(name))
            {
                if (string.Equals(name, "Contracts", StringComparison.OrdinalIgnoreCase))
                {
                    CheckContractsLevel(config, child, findings);
                }
                else if (string.Equals(name, "UseCases", StringComparison.OrdinalIgnoreCase))
                {
                    CheckUseCasesLevel(config, child, moduleName, null, useCases, reactions, findings);
                }

                continue;
            }

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameName(name, aggregate.Aggregate)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no layer folder or declared aggregate.", child, 1, null));
                continue;
            }

            CheckAggregateLevel(config, child, moduleName, name, useCases, findings);
        }
    }

    /// <summary>Reports every folder under a module Contracts folder that names no kind folder, and every file beside them.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts folder of one module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractsLevel(ConsumerConfig config, string contracts, List<Finding> findings)
    {
        foreach (var child in ChildFolders(contracts))
        {
            var name = Path.GetFileName(child);

            if (!ContractsKindFolders.Contains(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Contracts kind folder.", child, 1, null));
            }
        }

        foreach (var file in ChildFiles(contracts))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Contracts/, which holds only its kind folders.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under one aggregate that breaks the aggregate level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregateName">The aggregate folder name as written on disk.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateLevel(ConsumerConfig config, string aggregate, string moduleName, string aggregateName, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        foreach (var child in ChildFolders(aggregate))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "UseCases", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCasesLevel(config, child, moduleName, aggregateName, useCases, [], findings);
                continue;
            }

            if (AggregateFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no layer folder or UseCases folder.", child, 1, null));
        }
    }

    /// <summary>Reports every subfolder under a use case or reaction folder, which holds files only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The absolute use case or reaction folder.</param>
    /// <param name="kind">The folder kind as printed: use case or reaction.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseFilesOnly(ConsumerConfig config, string useCase, string kind, List<Finding> findings)
    {
        foreach (var child in ChildFolders(useCase))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in {kind} '{Path.GetFileName(useCase)}', which holds files only.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under a UseCases folder that names no declared use case of its home, or no declared reaction at module level.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCases">The absolute UseCases folder of one module or aggregate.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregateName">The aggregate folder name as written on disk, or null at module level.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="reactions">The declared reaction folders by module and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCasesLevel(ConsumerConfig config, string useCases, string moduleName, string? aggregateName, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, IReadOnlyList<(string Module, string Folder)> reactions, List<Finding> findings)
    {
        // A module with one aggregate of its own name stays flat, so the module
        // UseCases folder also holds the use cases of that aggregate.
        var owned = aggregateName is null
            ? declared
                .Where(useCase => SameName(moduleName, useCase.Module)
                    && (useCase.Aggregate is null || SameName(useCase.Aggregate, moduleName)))
                .ToList()
            : declared
                .Where(useCase => SameName(moduleName, useCase.Module)
                    && useCase.Aggregate is not null
                    && SameName(aggregateName, useCase.Aggregate))
                .ToList();
        var facts = aggregateName is null
            ? reactions.Where(reaction => SameName(moduleName, reaction.Module)).ToList()
            : [];

        foreach (var child in ChildFolders(useCases))
        {
            var name = Path.GetFileName(child);

            if (owned.Any(useCase => SameName(name, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            if (facts.Any(fact => SameName(name, fact.Folder)))
            {
                CheckUseCaseFilesOnly(config, child, "reaction", findings);
                continue;
            }

            if (owned.Count == 0 && facts.Count == 0)
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            findings.Add(new Finding(
                "code-placement-drift",
                aggregateName is null
                    ? $"Folder '{PathText.Relative(config.Root, child)}' names no declared use case or reaction folder."
                    : $"Folder '{PathText.Relative(config.Root, child)}' names no declared use case.",
                child,
                1,
                null));
        }
    }

    /// <summary>Reports every module folder under one layer project that names no declared module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="root">The absolute layer project root.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckLayerProject(ConsumerConfig config, string root, List<string> modules, List<Finding> findings)
    {
        if (modules.Count == 0)
        {
            return;
        }

        foreach (var module in ChildFolders(root))
        {
            var name = Path.GetFileName(module);

            if (name.Equals("Endpoints", StringComparison.OrdinalIgnoreCase) || name.Equals("Persistence", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
            }
        }
    }

    /// <summary>Reports every module folder under one frontend modules root that names no declared module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose modules root is checked.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFrontendRoot(ConsumerConfig config, FrontendConfig frontend, List<string> modules, List<Finding> findings)
    {
        if (modules.Count == 0 || !Directory.Exists(frontend.ModulesRoot))
        {
            return;
        }

        foreach (var module in ChildFolders(frontend.ModulesRoot))
        {
            var name = Path.GetFileName(module);

            if (!modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
            }
        }
    }

    /// <summary>The declared module names.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module names in model order.</returns>
    internal static List<string> DeclaredModules(ProjectModel model)
    {
        var modules = new List<string>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Module)
            {
                continue;
            }

            modules.Add(NameOf(record));
        }

        return modules;
    }

    /// <summary>The declared aggregates by module and aggregate name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module and aggregate names of each aggregate record placed in a module folder.</returns>
    private static List<(string Module, string Aggregate)> DeclaredAggregates(ProjectModel model)
    {
        var aggregates = new List<(string Module, string Aggregate)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is not null && path.Aggregate is not null)
            {
                aggregates.Add((path.Module, path.Aggregate));
            }
        }

        return aggregates;
    }

    /// <summary>The declared use cases by module, aggregate and name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate and name of each use case record placed in a module folder.</returns>
    private static List<(string Module, string? Aggregate, string Name)> DeclaredUseCases(ProjectModel model)
    {
        var useCases = new List<(string Module, string? Aggregate, string Name)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.UseCase)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is not null && path.Name is not null)
            {
                useCases.Add((path.Module, path.Aggregate, path.Name));
            }
        }

        return useCases;
    }

    /// <summary>
    ///     The declared reaction folders by module and folder name, one On{Fact} folder per trigger of each reaction.
    ///     The fact is the name of the event class that carries the trigger's mark, without its Event suffix. A
    ///     trigger with no marked class allows its name alone and its home and name, since either can name the class.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module and folder name of each reaction trigger placed in a module folder.</returns>
    private static List<(string Module, string Folder)> DeclaredReactionFolders(ProjectModel model)
    {
        var folders = new List<(string Module, string Folder)>();
        var eventClasses = model.Code.Elements
            .Where(element => element.Kind == CodeKind.DomainEvent)
            .SelectMany(element => element.Marks.Select(mark => (mark.RecordId, element.Class)))
            .ToLookup(pair => pair.RecordId, pair => pair.Class, StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Reaction)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is null)
            {
                continue;
            }

            foreach (var trigger in record.Ids(RecordFields.TriggeredBy))
            {
                var classes = eventClasses[trigger].ToList();

                if (classes.Count > 0)
                {
                    folders.AddRange(classes.Select(type => (path.Module, "On" + (type.EndsWith("Event", StringComparison.Ordinal) ? type[..^"Event".Length] : type))));
                }
                else if (Identifiers.TrySplit(trigger, out _, out var home, out var name) && name.Length > 0)
                {
                    folders.Add((path.Module, "On" + Naming.Pascal(name)));

                    if (home is not null && !name.StartsWith(home + "-", StringComparison.Ordinal))
                    {
                        folders.Add((path.Module, "On" + Naming.Pascal(home + "-" + name)));
                    }
                }
            }
        }

        return folders;
    }

    /// <summary>The child folders of one folder in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="folder">The absolute folder to read.</param>
    /// <returns>The child folders in ordinal order, without build output and tool folders.</returns>
    private static List<string> ChildFolders(string folder)
    {
        try
        {
            return Directory.EnumerateDirectories(folder)
                .Where(child => !SourceTree.SkippedFolders.Contains(Path.GetFileName(child)))
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>The files directly in one folder in ordinal order, or an empty list when the folder cannot be read.</summary>
    /// <param name="folder">The absolute folder to read.</param>
    /// <returns>The files directly in the folder in ordinal order.</returns>
    private static List<string> ChildFiles(string folder)
    {
        try
        {
            return Directory.EnumerateFiles(folder)
                .OrderBy(child => child, StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
