using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check: the Kernel, the Platform, each module project, each Contracts project, each aggregate and use case folder, each port folder with its adapter folder, and each frontend modules root hold only their fixed folders and the folders a declared record names; each kind of place groups past 20 files into its closed grouping folders.</summary>
/// <follows>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
/// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
/// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
/// <follows>litespec/core/backend-application#use-this-use-case-layout</follows>
/// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
internal static partial class ClosedFolderChecks
{
    /// <summary>Fixed folders at the Platform root: one folder per technical capability.</summary>
    static readonly HashSet<string> PlatformFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence", "Messaging", "Access", "Http", "CountryPacks", "Modules",
    };

    /// <summary>Grouping folders at the Kernel root: the aggregate base and domain vocabulary, the shared values, and the audit vocabulary.</summary>
    static readonly HashSet<string> KernelFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Values", "Auditing",
    };

    /// <summary>Grouping folders under a Platform CountryPacks folder: one folder per pack sub-area.</summary>
    static readonly HashSet<string> CountryPackAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vat", "Charts", "Banking", "Filing", "Documents", "Country", "Retention",
    };

    /// <summary>Grouping folders under the shared UI package components folder: one folder per component role.</summary>
    static readonly HashSet<string> UiComponentGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "fields", "overlays", "layout", "data", "values", "feedback",
    };

    /// <summary>Grouping folders under a web app shared app folder: one folder per shared area.</summary>
    static readonly HashSet<string> AppGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        "session", "i18n", "api", "ui",
    };

    /// <summary>The file count past which a folder groups into its allowed grouping folders.</summary>
    const int UngroupedFileLimit = 20;

    /// <summary>Fixed folders at a module project root, whatever its aggregates, use cases, ports, triggers and read models declare.</summary>
    static readonly HashSet<string> ModuleFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Ports", "Infrastructure", "WebApi",
    };

    /// <summary>Fixed folders under one aggregate in Domain: its events, refusals, parts and facts. Each root action takes one more folder named for the act.</summary>
    static readonly HashSet<string> AggregateSubFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Events", "Exceptions", "Parts", "Facts",
    };

    /// <summary>Fixed folders under a module Application folder besides its use case folders: its trigger homes and its settings.</summary>
    static readonly HashSet<string> ApplicationFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notifications", "ExternalSystems", "ReadModels", "Settings",
    };

    /// <summary>Fixed folders under a module Infrastructure folder besides its adapter folders: its persistence, its message pipeline pieces and its HTTP pipeline pieces.</summary>
    static readonly HashSet<string> InfrastructureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence", "Messaging", "Pipeline",
    };

    /// <summary>The layer folders a use case folder sits under.</summary>
    static readonly HashSet<string> UseCaseHomeLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application", "WebApi",
    };

    /// <summary>Fixed folders under a layer folder that hold no use case: the trigger homes, persistence and settings.</summary>
    static readonly HashSet<string> FixedLayerFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notifications", "ExternalSystems", "ReadModels", "Persistence", "Settings",
    };

    /// <summary>The file name suffixes of transport types, which sit in a use case WebApi folder and no other.</summary>
    static readonly string[] TransportSuffixes = ["Endpoint.cs", "RequestModel.cs", "ResponseModel.cs", "ResponseItemModel.cs", "PaginationModel.cs"];

    /// <summary>The file name suffixes a use case WebApi folder accepts: its endpoint and any model or row shaping the wire contract.</summary>
    static readonly string[] WebApiSuffixes = ["Endpoint.cs", "Model.cs", "Row.cs"];

    /// <summary>The file name suffixes of a use case's own message, result and transport models. The longer suffix comes first, so CommandResult is matched before Command.</summary>
    static readonly string[] UseCaseModelSuffixes = ["CommandResult.cs", "QueryResult.cs", "Command.cs", "Query.cs", "RequestModel.cs", "ResponseModel.cs"];

    /// <summary>The file name suffixes of a use case command message and result, which sit in the use case folder of their Contracts aggregate only for a public command.</summary>
    static readonly string[] CommandSuffixes = ["CommandResult.cs", "Command.cs"];

    /// <summary>The file name suffixes of a use case query message and result, which sit in the use case folder of their Contracts aggregate.</summary>
    static readonly string[] QuerySuffixes = ["QueryResult.cs", "Query.cs"];

    /// <summary>The file name suffixes of a fact: a module event or an integration event, which sit in the On fact folder of their Contracts aggregate.</summary>
    static readonly string[] FactSuffixes = ["ModuleEvent.cs", "IntegrationEvent.cs"];

    /// <summary>The file name suffixes of an adapter: an adapter class or client, which sit in their Infrastructure port folder and never in Persistence.</summary>
    static readonly string[] AdapterSuffixes = ["Adapter.cs", "Client.cs"];

    /// <summary>
    ///     Reports every backend and frontend folder outside the closed folder sets, and every module and
    ///     aggregate folder that names no declared record. A host holds its endpoints and plumbing without
    ///     a folder rule, and a project with no declared modules, aggregates or use cases states no names,
    ///     so those folders are left out.
    /// </summary>
    /// <param name="model">The loaded project model that holds every declared module, aggregate and use case.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        var modules = DeclaredModules(model);
        var aggregates = DeclaredAggregates(model);
        var useCases = DeclaredUseCases(model);
        var homes = DeclaredTriggerHomes(model);
        var publicUseCases = DeclaredPublicUseCases(model);
        var facts = DeclaredFacts(model);
        var portMarks = DeclaredPortMarks(model);
        var externalNames = DeclaredExternalNames(model);
        var aggregateMarked = AggregateMarkedFiles(model);

        foreach (var modulesRoot in model.Config.ModulesRoots)
        {
            if (modulesRoot.Length == 0 || !Directory.Exists(modulesRoot))
            {
                continue;
            }

            if (SourceRoots.IsSingleModulesProject(modulesRoot))
            {
                CheckSingleModulesRoot(model.Config, modulesRoot, modules, aggregates, useCases, publicUseCases, homes, facts, portMarks, externalNames, aggregateMarked, findings);
            }
            else
            {
                CheckModuleProject(model.Config, modulesRoot, modules, aggregates, useCases, publicUseCases, homes, facts, portMarks, externalNames, aggregateMarked, findings);
            }

            CheckMessageFiles(model.Config, modulesRoot, aggregates, useCases, findings);
            CheckPublicUseCaseFiles(model.Config, modulesRoot, model.Config.ModulesRoots, aggregates, publicUseCases, findings);
        }

        if (model.Config.KernelRoot.Length > 0 && Directory.Exists(model.Config.KernelRoot))
        {
            CheckKernelRoot(model.Config, findings);
        }

        if (model.Config.PlatformRoot.Length > 0 && Directory.Exists(model.Config.PlatformRoot))
        {
            CheckPlatformRoot(model.Config, findings);
        }

        if (SourceRoots.KnowledgeRoot(model.Config) is { Length: > 0 } knowledge)
        {
            CheckKnowledgeRoot(model.Config, knowledge, findings);
        }

        foreach (var frontend in model.Config.Frontends)
        {
            CheckFrontendRoot(model.Config, frontend, modules, findings);
        }

        CheckSharedComponents(model.Config, findings);
    }

    /// <summary>Reports every subfolder under a folder that holds files only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="folder">The absolute folder that holds files only.</param>
    /// <param name="kind">The folder kind as printed: use case, port, grouping, Notifications, ExternalSystems or ReadModels.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseFilesOnly(ConsumerConfig config, string folder, string kind, List<Finding> findings)
    {
        foreach (var child in ChildFolders(folder))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in {kind} '{Path.GetFileName(folder)}', which holds files only.", child, 1, null));
        }
    }

    /// <summary>Reports folder-ungrouped when the folder holds more than 20 files directly and none of its allowed grouping folders.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="folder">The absolute folder to judge.</param>
    /// <param name="children">The child folders of the folder.</param>
    /// <param name="isGrouping">Whether a child folder path is one of the folder's allowed groupings.</param>
    /// <param name="remedy">How the message tells the reader to group the files.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <param name="extensions">The file extensions counted, or null for every file.</param>
    private static void CheckUngrouped(ConsumerConfig config, string folder, List<string> children, Func<string, bool> isGrouping, string remedy, List<Finding> findings, string[]? extensions = null)
    {
        if (children.Any(isGrouping))
        {
            return;
        }

        var count = ChildFiles(folder)
            .Count(file => extensions is null || extensions.Any(extension => file.EndsWith(extension, StringComparison.OrdinalIgnoreCase)));

        if (count > UngroupedFileLimit)
        {
            findings.Add(new Finding("folder-ungrouped", $"Folder '{PathText.Relative(config.Root, folder)}' holds {count} files and none of its grouping folders; {remedy}.", folder, 1, null));
        }
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
