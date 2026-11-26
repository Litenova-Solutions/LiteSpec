using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check: the Kernel, the Platform, each module project, each Contracts project, each aggregate and use case folder, each port folder with its adapter folder, and each frontend modules root hold only their fixed folders and the folders a declared record names; each kind of place groups past 20 files into its closed grouping folders.</summary>
/// <follows>litespec/packs/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
/// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
/// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
/// <follows>litespec/core/backend-application#use-this-use-case-layout</follows>
/// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
internal static class ClosedFolderChecks
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

    /// <summary>Reports every folder under the Kernel root outside its grouping folders, and every file directly at the root, at any count: the root holds no files.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckKernelRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.KernelRoot))
        {
            var name = Path.GetFileName(child);

            if (KernelFolders.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Kernel folder; the Kernel holds Domain/, Values/ and Auditing/.", child, 1, null));
        }

        foreach (var file in ChildFiles(config.KernelRoot))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Kernel root, which holds no files; move it into Domain/, Values/ or Auditing/.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under the Platform root that breaks its closed set of capability folders, every file directly at the root, at any count, and each capability past 20 files with no sub-area.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPlatformRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.PlatformRoot))
        {
            var name = Path.GetFileName(child);

            if (PlatformFolders.Contains(name))
            {
                CheckCapabilityLevel(config, child, name, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Platform folder.", child, 1, null));
        }

        foreach (var file in ChildFiles(config.PlatformRoot))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Platform root, which holds no files; move it into its capability folder.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under one Platform capability outside its sub-areas, and the capability past 20 files with none. CountryPacks takes one folder per named pack area; any other capability groups into one folder per sub-area of its own naming.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="capability">The absolute capability folder under the Platform root.</param>
    /// <param name="name">The capability folder name as written on disk.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCapabilityLevel(ConsumerConfig config, string capability, string name, List<Finding> findings)
    {
        var children = ChildFolders(capability);
        var packs = string.Equals(name, "CountryPacks", StringComparison.OrdinalIgnoreCase);

        foreach (var child in children)
        {
            var area = Path.GetFileName(child);

            if (packs && !CountryPackAreas.Contains(area))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no CountryPacks area; CountryPacks holds Vat/, Charts/, Banking/, Filing/, Documents/, Country/ and Retention/.", child, 1, null));
                continue;
            }

            CheckUseCaseFilesOnly(config, child, "grouping", findings);
        }

        var remedy = packs
            ? "group them into Vat/, Charts/, Banking/, Filing/ or Documents/"
            : "group them into one folder per sub-area";

        CheckUngrouped(config, capability, children, _ => true, remedy, findings, [".cs"]);
    }

    /// <summary>Reports every folder under the Knowledge root that is neither a knowledge model nor a set of a model the folder holds, and every file directly at the root: the root holds one model project per area and one set project per variant.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="knowledge">The absolute Knowledge folder beside Modules.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#compile-each-knowledge-area-as-a-model-project-with-its-set-projects</follows>
    private static void CheckKnowledgeRoot(ConsumerConfig config, string knowledge, List<Finding> findings)
    {
        var models = ChildFolders(knowledge)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(SourceRoots.IsKnowledgeModelName)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var child in ChildFolders(knowledge))
        {
            var name = Path.GetFileName(child);

            if (SourceRoots.IsKnowledgeModelName(name))
            {
                continue;
            }

            if (SourceRoots.KnowledgeModelName(child) is { } own && models.Contains(own))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no knowledge model or set of a known model; Knowledge holds one '<Product>.<Area>/' model project per area and one '<Product>.<Area>.<Variant>/' set project per variant.", child, 1, null));
        }

        foreach (var file in ChildFiles(knowledge))
        {
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the Knowledge root, which holds only model and set projects.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under the single modules root that breaks its level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The single modules project holding every module.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="aggregateMarked">The expected aggregate folder per file whose marks name an aggregate-homed aggregate, event or rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSingleModulesRoot(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, IReadOnlyDictionary<string, string> aggregateMarked, List<Finding> findings)
    {
        foreach (var module in ChildFolders(modulesRoot))
        {
            var name = Path.GetFileName(module);

            if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
                continue;
            }

            CheckModuleLevel(config, module, name, modules, aggregates, useCases, publicUseCases, homes, facts, portMarks, externalNames, aggregateMarked, nestedContracts: true, findings);
        }
    }

    /// <summary>Reports the module project that names no declared module, and every folder under it that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding one module.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="aggregateMarked">The expected aggregate folder per file whose marks name an aggregate-homed aggregate, event or rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleProject(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, IReadOnlyDictionary<string, string> aggregateMarked, List<Finding> findings)
    {
        if (SourceRoots.IsContractsProject(modulesRoot))
        {
            CheckContractsProject(config, modulesRoot, modules, aggregates, useCases, publicUseCases, facts, findings);
            return;
        }

        var name = SourceRoots.ModuleNameOf(modulesRoot) ?? Path.GetFileName(modulesRoot);

        if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, modulesRoot)}' names no declared module.", modulesRoot, 1, null));
            return;
        }

        CheckModuleLevel(config, modulesRoot, name, modules, aggregates, useCases, publicUseCases, homes, facts, portMarks, externalNames, aggregateMarked, nestedContracts: false, findings);
    }

    /// <summary>Reports the Contracts project whose owner names no declared module, and every folder under it that breaks the Contracts closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The Contracts project holding one module's contracts.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractsProject(ConsumerConfig config, string contracts, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, List<Finding> findings)
    {
        var owner = SourceRoots.ModuleNameOf(contracts) ?? Path.GetFileName(contracts);

        if (modules.Count > 0 && !modules.Any(declared => SameName(owner, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, contracts)}' names no declared module.", contracts, 1, null));
            return;
        }

        CheckContractsLevel(config, contracts, owner, aggregates, useCases, publicUseCases, facts, findings);
    }

    /// <summary>Reports every folder under one module that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="module">The absolute module folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="aggregateMarked">The expected aggregate folder per file whose marks name an aggregate-homed aggregate, event or rule.</param>
    /// <param name="nestedContracts">Whether a nested Contracts folder is allowed: only in the one-modules-project deviation.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ConsumerConfig config, string module, string moduleName, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, IReadOnlyDictionary<string, string> aggregateMarked, bool nestedContracts, List<Finding> findings)
    {
        // Each port pairs with one adapter folder of the same name, so both folder
        // names are read before either level is checked.
        string? ports = null;
        string? infrastructure = null;

        foreach (var child in ChildFolders(module))
        {
            if (string.Equals(Path.GetFileName(child), "Ports", StringComparison.OrdinalIgnoreCase))
            {
                ports = child;
            }
            else if (string.Equals(Path.GetFileName(child), "Infrastructure", StringComparison.OrdinalIgnoreCase))
            {
                infrastructure = child;
            }
        }

        var portNames = ports is null
            ? []
            : ChildFolders(ports).Select(child => Path.GetFileName(child)).ToList();
        var adapterNames = infrastructure is null
            ? []
            : ChildFolders(infrastructure).Select(child => Path.GetFileName(child)).Where(name => !InfrastructureFolders.Contains(name)).ToList();

        foreach (var child in ChildFolders(module))
        {
            var name = Path.GetFileName(child);

            if (ModuleFolders.Contains(name))
            {
                if (string.Equals(name, "Domain", StringComparison.OrdinalIgnoreCase))
                {
                    CheckDomainLevel(config, child, moduleName, aggregates, aggregateMarked, findings);
                }
                else if (string.Equals(name, "Application", StringComparison.OrdinalIgnoreCase))
                {
                    CheckApplicationLevel(config, child, moduleName, aggregates, useCases, homes, findings);
                }
                else if (string.Equals(name, "Ports", StringComparison.OrdinalIgnoreCase))
                {
                    CheckPortsLevel(config, child, adapterNames, modules, portMarks, externalNames, findings);
                }
                else if (string.Equals(name, "Infrastructure", StringComparison.OrdinalIgnoreCase))
                {
                    CheckInfrastructureLevel(config, child, moduleName, aggregates, portNames, modules, findings);
                }
                else if (string.Equals(name, "WebApi", StringComparison.OrdinalIgnoreCase))
                {
                    CheckWebApiLevel(config, child, moduleName, aggregates, useCases, findings);
                }

                continue;
            }

            if (nestedContracts && string.Equals(name, "Contracts", StringComparison.OrdinalIgnoreCase))
            {
                CheckContractsLevel(config, child, moduleName, aggregates, useCases, publicUseCases, facts, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no module folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the module contracts that names no declared aggregate or public module-level use case of the owner, every file directly beside them, every interface, every fact outside its On fact folder, and every command of no public use case.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsLevel(ConsumerConfig config, string contracts, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, List<Finding> findings)
    {
        foreach (var child in ChildFolders(contracts))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Any(aggregate => SameName(owner, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                CheckContractsAggregate(config, child, owner, name, aggregates, useCases, publicUseCases, facts, findings);
                continue;
            }

            if (publicUseCases.Any(useCase => SameName(owner, useCase.Module) && useCase.Aggregate is null && SameName(name, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            var singular = aggregates.FirstOrDefault(aggregate => SameName(owner, aggregate.Module) && SameAggregate(name, aggregate.Aggregate));

            if (singular.Module is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate; the aggregate folder is '{singular.Expected}/'.", child, 1, null));
                continue;
            }

            var flat = useCases.FirstOrDefault(useCase => SameName(owner, useCase.Module) && SameName(name, useCase.Name));

            if (flat.Module is not null)
            {
                if (flat.Aggregate is not null
                    && publicUseCases.Any(useCase => SameName(owner, useCase.Module) && SameName(name, useCase.Name)))
                {
                    var expected = ExpectedOf(aggregates, owner, flat.Aggregate);
                    findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names the public use case '{flat.Name}', which sits in its aggregate folder '{expected}/'.", child, 1, null));
                    continue;
                }

                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names '{flat.Name}', which is no public use case; only public use cases sit in Contracts.", child, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate or public module-level use case of module '{owner}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(contracts))
        {
            // The Contracts project file lives at the project root.
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Contracts/, which holds only one folder per aggregate and public module-level use case.", file, 1, null));
        }

        CheckContractsTypes(config, contracts, owner, aggregates, publicUseCases, findings);
    }

    /// <summary>Reports every folder under one Contracts aggregate folder that names no public use case of that aggregate and no On fact folder, every fact directly beside them, every query or command message or result directly beside them, and every declared fact with no On fact folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Contracts.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsAggregate(ConsumerConfig config, string aggregate, string owner, string name, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, List<Finding> findings)
    {
        var owned = publicUseCases
            .Where(useCase => SameName(owner, useCase.Module)
                && useCase.Aggregate is not null
                && SameName(name, ExpectedOf(aggregates, owner, useCase.Aggregate)))
            .ToList();
        var children = ChildFolders(aggregate);

        foreach (var child in children)
        {
            var childName = Path.GetFileName(child);

            if (FactOf(owner, name, aggregates, facts, childName) is { } fact)
            {
                CheckContractsFact(config, child, owner, name, aggregates, facts, fact, findings);
                continue;
            }

            var elsewhere = facts.FirstOrDefault(candidate =>
                SameName(owner, candidate.Module)
                && candidate.Aggregate is not null
                && !SameAggregate(name, candidate.Aggregate)
                && string.Equals(childName, OnFolderOf(candidate.Name), StringComparison.OrdinalIgnoreCase));

            if (elsewhere.Id is not null)
            {
                var home = ExpectedOf(aggregates, owner, elsewhere.Aggregate!);
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' holds the fact '{elsewhere.Id}'; it belongs in '{home}/{OnFolderOf(elsewhere.Name)}/'.", child, 1, elsewhere.Id));
                continue;
            }

            if (owned.Any(useCase => SameName(childName, useCase.Name)))
            {
                CheckUseCaseFilesOnly(config, child, "use case", findings);
                continue;
            }

            var level = publicUseCases.FirstOrDefault(useCase =>
                SameName(owner, useCase.Module) && useCase.Aggregate is null && SameName(childName, useCase.Name));

            if (level.Id is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names the public use case '{level.Name}', which sits directly in Contracts/.", child, 1, null));
                continue;
            }

            var flat = useCases.FirstOrDefault(useCase =>
                SameName(owner, useCase.Module)
                && useCase.Aggregate is not null
                && SameName(name, ExpectedOf(aggregates, owner, useCase.Aggregate))
                && SameName(childName, useCase.Name));

            if (flat.Module is not null)
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names '{flat.Name}', which is no public use case; only public use cases sit in Contracts.", child, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no public use case or On fact folder of aggregate '{name}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(aggregate))
        {
            var fileName = Path.GetFileName(file);
            var factSuffix = FactSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (factSuffix is not null)
            {
                var factKind = factSuffix.StartsWith("Module", StringComparison.Ordinal) ? "module" : "integration";
                var factPrefix = fileName[..^factSuffix.Length];
                var factMatch = facts.FirstOrDefault(fact =>
                    SameName(owner, fact.Module)
                    && fact.Kind == factKind
                    && SameName(factPrefix, fact.Name));
                var expected = factMatch.Id is not null && factMatch.Aggregate is not null
                    ? ExpectedOf(aggregates, owner, factMatch.Aggregate)
                    : name;
                var folder = factMatch.Id is not null ? OnFolderOf(factMatch.Name) : "On" + factPrefix;

                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a fact outside its On fact folder; it belongs in '{expected}/{folder}/'.", file, 1, null));
                continue;
            }

            var querySuffix = QuerySuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));
            var commandSuffix = querySuffix is null
                ? CommandSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal))
                : null;

            if (querySuffix is null && commandSuffix is null)
            {
                continue;
            }

            var kind = querySuffix is not null ? "query" : "command";
            var prefix = fileName[..^(querySuffix ?? commandSuffix!).Length];
            var match = owned.FirstOrDefault(useCase => useCase.Kind == kind && SameName(prefix, useCase.Name));

            if (match.Id is null)
            {
                var shape = kind == "query" ? "a query message" : "a command message";
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds {shape} outside a use case folder.", file, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the message of public {kind} '{match.Id}'; it belongs in '{Naming.Pascal(match.Name)}/'.", file, 1, null));
        }

        var held = children
            .Select(child => Path.GetFileName(child))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var fact in facts
            .Where(fact => SameName(owner, fact.Module)
                && fact.Aggregate is not null
                && SameAggregate(name, fact.Aggregate))
            .OrderBy(fact => fact.Id, StringComparer.Ordinal))
        {
            var folder = OnFolderOf(fact.Name);

            if (!held.Contains(folder))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, aggregate)}' holds no '{folder}/' folder for declared {fact.Kind} event '{fact.Id}'.", aggregate, 1, fact.Id));
            }
        }
    }

    /// <summary>The On fact folder a declared fact takes under its Contracts aggregate folder: On with the PascalCase record name.</summary>
    /// <param name="name">The fact name segment of its identifier, such as session-ended.</param>
    /// <returns>The folder name, such as OnSessionEnded.</returns>
    private static string OnFolderOf(string name) => "On" + Naming.Pascal(name);

    /// <summary>The declared fact an On fact folder names, or null when it names none: a fact of the owner module whose On folder the child spells and whose aggregate the folder sits under.</summary>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="childName">The child folder name as written on disk.</param>
    /// <returns>The declared fact the folder names, or null.</returns>
    private static (string Module, string? Aggregate, string Name, string Id, string Kind)? FactOf(string owner, string name, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, string childName)
    {
        foreach (var fact in facts)
        {
            if (!SameName(owner, fact.Module)
                || !string.Equals(childName, OnFolderOf(fact.Name), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (fact.Aggregate is null || SameAggregate(name, fact.Aggregate))
            {
                return fact;
            }
        }

        return null;
    }

    /// <summary>Reports every subfolder under one Contracts On fact folder, which holds files only, every fact file in it that names no declared fact, and every fact outside the aggregate of the domain event it translates. The payload types only the fact carries sit beside it.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="folder">The absolute On fact folder under Contracts.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregate">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="facts">The declared facts by module, aggregate, name, identifier and kind.</param>
    /// <param name="fact">The declared fact the folder names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsFact(ConsumerConfig config, string folder, string owner, string aggregate, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> facts, (string Module, string? Aggregate, string Name, string Id, string Kind) fact, List<Finding> findings)
    {
        var folderName = Path.GetFileName(folder);

        foreach (var child in ChildFolders(folder))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in '{aggregate}/{folderName}/', which holds files only.", child, 1, null));
        }

        foreach (var file in ChildFiles(folder))
        {
            var fileName = Path.GetFileName(file);
            var suffix = FactSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (suffix is null)
            {
                continue;
            }

            var kind = suffix.StartsWith("Module", StringComparison.Ordinal) ? "module" : "integration";
            var prefix = fileName[..^suffix.Length];
            var match = facts.FirstOrDefault(candidate =>
                SameName(owner, candidate.Module)
                && candidate.Kind == kind
                && SameName(prefix, candidate.Name));

            if (match.Id is null)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a {kind} event naming no declared {kind} event of module '{owner}'.", file, 1, null));
                continue;
            }

            if (!string.Equals(match.Id, fact.Id, StringComparison.Ordinal))
            {
                var home = match.Aggregate is null ? aggregate : ExpectedOf(aggregates, owner, match.Aggregate);
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the fact '{match.Id}'; it belongs in '{home}/{OnFolderOf(match.Name)}/'.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every interface and every command file under the module contracts that is no public command's message in its use case folder, which publish data only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsTypes(ConsumerConfig config, string contracts, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        List<string> files;

        try
        {
            if (!Directory.Exists(contracts))
            {
                return;
            }

            files = Directory.EnumerateFiles(contracts, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(contracts, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (CommandSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal))
                && ContractsCommandFinding(config, contracts, file, fileName, owner, aggregates, publicUseCases) is { } message)
            {
                findings.Add(new Finding("code-placement-drift", message, file, 1, null));
            }

            foreach (var face in InterfacesOf(file))
            {
                findings.Add(new Finding("code-placement-drift", $"Interface '{face.Name}' sits in Contracts, which publishes data only.", face.File, face.Line, null));
            }
        }
    }

    /// <summary>The finding message for a command file under the module contracts, or null when the file is its public command's message in its use case folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="file">The absolute command file under Contracts.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <returns>The finding message, or null when the file is allowed or the folder checks own it.</returns>
    private static string? ContractsCommandFinding(ConsumerConfig config, string contracts, string file, string fileName, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases)
    {
        var segments = PathText.Relative(contracts, file).Split('/');
        var suffix = CommandSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

        if (suffix is null)
        {
            return null;
        }

        var prefix = fileName[..^suffix.Length];
        var relative = PathText.Relative(config.Root, file);

        // The folder checks own a file directly in Contracts/, a file directly in an
        // aggregate folder, and a file nested past a use case folder, so those stay
        // silent here.
        if (segments.Length == 1
            || (segments.Length == 2 && aggregates.Any(aggregate => SameName(owner, aggregate.Module) && SameName(segments[0], aggregate.Expected)))
            || segments.Length > 3)
        {
            return null;
        }

        var home = segments.Length == 2
            ? PublicCommandHome(publicUseCases, owner, aggregate: null, segments[0])
            : PublicCommandHome(publicUseCases, owner, segments[0], segments[1], aggregates);

        if (home is not null && SameName(prefix, home.Value.Name))
        {
            return null;
        }

        var named = publicUseCases.FirstOrDefault(useCase =>
            useCase.Kind == "command"
            && SameName(owner, useCase.Module)
            && SameName(prefix, useCase.Name));

        if (named.Id is not null)
        {
            var expected = named.Aggregate is null
                ? $"{PathText.Relative(config.Root, contracts)}/{Naming.Pascal(named.Name)}"
                : $"{PathText.Relative(config.Root, contracts)}/{ExpectedOf(aggregates, owner, named.Aggregate)}/{Naming.Pascal(named.Name)}";

            return $"File '{relative}' holds the message of public command '{named.Id}'; it belongs in '{expected}/'.";
        }

        return $"File '{relative}' holds a command of no public use case; only public use cases sit in Contracts.";
    }

    /// <summary>The public command whose use case folder the segments name, or null when they name none.</summary>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregate">The aggregate folder holding the use case folder, or null for a module-level folder.</param>
    /// <param name="folder">The use case folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder; read only for an aggregate folder.</param>
    /// <returns>The public command the folders name, or null.</returns>
    private static (string Module, string? Aggregate, string Name, string Id, string Kind)? PublicCommandHome(
        IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases,
        string owner,
        string? aggregate,
        string folder,
        IReadOnlyList<(string Module, string Aggregate, string Expected)>? aggregates = null)
    {
        foreach (var useCase in publicUseCases)
        {
            if (useCase.Kind != "command"
                || !SameName(owner, useCase.Module)
                || !SameName(folder, useCase.Name))
            {
                continue;
            }

            if (aggregate is null && useCase.Aggregate is null)
            {
                return useCase;
            }

            if (aggregate is not null
                && useCase.Aggregate is not null
                && aggregates is not null
                && SameName(aggregate, ExpectedOf(aggregates, owner, useCase.Aggregate)))
            {
                return useCase;
            }
        }

        return null;
    }

    /// <summary>One interface a C# file declares: its name, file and line.</summary>
    /// <param name="Name">The simple interface name as declared.</param>
    /// <param name="File">The file declaring the interface.</param>
    /// <param name="Line">The 1-based line of the interface name.</param>
    sealed record ContractsInterface(string Name, string File, int Line);

    /// <summary>The interfaces one C# file declares, in file order.</summary>
    /// <param name="file">The absolute C# file to read.</param>
    /// <returns>Each declared interface in file order; a file that cannot be read reads as empty.</returns>
    private static List<ContractsInterface> InterfacesOf(string file)
    {
        var faces = new List<ContractsInterface>();
        SyntaxTree tree;

        try
        {
            if (!File.Exists(file))
            {
                return faces;
            }

            tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return faces;
        }

        foreach (var declared in tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>())
        {
            // A C# 14 extension block is a type declaration with no name, so it names no type.
            if (declared.Identifier.IsMissing || declared.Identifier.Span.Length == 0)
            {
                continue;
            }

            var line = tree.GetLineSpan(declared.Identifier.Span).StartLinePosition.Line + 1;
            faces.Add(new ContractsInterface(declared.Identifier.ValueText, file, line));
        }

        return faces;
    }

    /// <summary>
    ///     Reports each query or command message or result of a public use case that sits in the module project instead of
    ///     the Contracts project. A public use case keeps its message and result at its use case path in
    ///     Contracts, so a file named for one outside Contracts is drift, and the finding names the expected path.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project or single modules project holding the files.</param>
    /// <param name="allRoots">Every module root, holding the sibling Contracts project of each module.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckPublicUseCaseFiles(ConsumerConfig config, string modulesRoot, IReadOnlyList<string> allRoots, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, List<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        if (publicUseCases.Count == 0)
        {
            return;
        }

        if (SourceRoots.IsSingleModulesProject(modulesRoot))
        {
            foreach (var module in ChildFolders(modulesRoot))
            {
                var name = Path.GetFileName(module);
                var contracts = Path.Combine(module, "Contracts");

                CheckPublicUseCaseTree(config, module, contracts, name, $"{name}/Contracts", aggregates, publicUseCases, findings);
            }

            return;
        }

        if (SourceRoots.IsContractsProject(modulesRoot))
        {
            return;
        }

        var owner = SourceRoots.ModuleNameOf(modulesRoot) ?? Path.GetFileName(modulesRoot);
        var sibling = allRoots.FirstOrDefault(root => SourceRoots.IsContractsProject(root)
            && string.Equals(SourceRoots.ModuleNameOf(root), owner, StringComparison.OrdinalIgnoreCase));
        var display = sibling is null ? "Contracts" : Path.GetFileName(sibling.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        CheckPublicUseCaseTree(config, modulesRoot, contracts: null, owner, display, aggregates, publicUseCases, findings);
    }

    /// <summary>Reports each query or command message or result of a public use case under one module tree that sits outside its Contracts path.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="tree">The absolute module tree to scan.</param>
    /// <param name="contracts">The nested Contracts folder to leave out, or null when the tree holds none.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="display">The Contracts folder name as printed in the expected path.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPublicUseCaseTree(ConsumerConfig config, string tree, string? contracts, string owner, string display, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        List<string> files;

        try
        {
            files = Directory.EnumerateFiles(tree, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(tree, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Where(file => contracts is null || !file.StartsWith(contracts + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var owned = publicUseCases.Where(useCase => SameName(owner, useCase.Module)).ToList();

        if (owned.Count == 0)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var querySuffix = QuerySuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));
            var commandSuffix = querySuffix is null
                ? CommandSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal))
                : null;

            if (querySuffix is null && commandSuffix is null)
            {
                continue;
            }

            var kind = querySuffix is not null ? "query" : "command";
            var prefix = fileName[..^(querySuffix ?? commandSuffix!).Length];
            var match = owned.FirstOrDefault(useCase => useCase.Kind == kind && SameName(prefix, useCase.Name));

            if (match.Id is null)
            {
                continue;
            }

            var useCaseFolder = Naming.Pascal(match.Name);
            var home = match.Aggregate is null
                ? $"{display}/{useCaseFolder}"
                : $"{display}/{ExpectedOf(aggregates, owner, match.Aggregate)}/{useCaseFolder}";

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the message of public {kind} '{match.Id}'; it belongs in '{home}/'.", file, 1, null));
        }
    }

    /// <summary>The public use cases: query and command use cases whose reachedThrough names a consuming module, by module, aggregate, name, identifier and kind.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate, name, identifier and kind of each public use case placed in a module folder.</returns>
    private static List<(string Module, string? Aggregate, string Name, string Id, string Kind)> DeclaredPublicUseCases(ProjectModel model)
    {
        var useCases = new List<(string Module, string? Aggregate, string Name, string Id, string Kind)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.UseCase
                || record.Field(RecordFields.Kind) is not ("query" or "command")
                || !record.Ids(RecordFields.ReachedThrough).Any(IsModuleId))
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is not null && path.Name is not null)
            {
                useCases.Add((path.Module, path.Aggregate, path.Name, record.Id, record.Field(RecordFields.Kind)!));
            }
        }

        return useCases;
    }

    /// <summary>The declared facts by module, aggregate, name, identifier and kind: each module event and each integration event with its record folder's aggregate, or the aggregate of the domain event it translates when its folder states none.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate folder, name, identifier and kind of each fact placed in a module folder.</returns>
    private static List<(string Module, string? Aggregate, string Name, string Id, string Kind)> DeclaredFacts(ProjectModel model)
    {
        var facts = new List<(string Module, string? Aggregate, string Name, string Id, string Kind)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.ModuleEvent && record.Type != RecordTypes.IntegrationEvent)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is null || path.Name is null)
            {
                continue;
            }

            var aggregate = path.Aggregate;

            if (aggregate is null
                && record.Field(RecordFields.Translates) is { } translated
                && model.ById.TryGetValue(translated, out var domainEvent)
                && domainEvent.Type == RecordTypes.DomainEvent)
            {
                aggregate = PathOf(domainEvent).Aggregate;
            }

            var kind = record.Type == RecordTypes.ModuleEvent ? "module" : "integration";
            facts.Add((path.Module, aggregate, path.Name, record.Id, kind));
        }

        return facts;
    }

    /// <summary>Whether the identifier names a module.</summary>
    /// <param name="id">The identifier under test.</param>
    /// <returns>True when the identifier opens with the module prefix.</returns>
    private static bool IsModuleId(string id) =>
        Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix == RecordTypes.Module;

    /// <summary>Reports every folder under a module Domain folder that names no declared aggregate of the module, and every type directly in Domain/ that belongs to an aggregate: one plural folder per declared aggregate, so a singular folder reports too.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="domain">The absolute Domain folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="aggregateMarked">The expected aggregate folder per file whose marks name an aggregate-homed aggregate, event or rule.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDomainLevel(ConsumerConfig config, string domain, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyDictionary<string, string> aggregateMarked, List<Finding> findings)
    {
        foreach (var child in ChildFolders(domain))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate.", child, 1, null));
                continue;
            }

            CheckAggregateLevel(config, child, findings);
        }

        foreach (var file in ChildFiles(domain))
        {
            if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                && aggregateMarked.TryGetValue(file, out var expected))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Domain/, but its types belong to an aggregate; move them into '{expected}/'.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under one aggregate in Domain outside its grouping folders, every exception file beside the root instead of its Exceptions folder, every event file outside its Events folder, every state file beside the root instead of its Parts folder, a second store interface for the aggregate, and the aggregate past 20 files with no grouping folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateLevel(ConsumerConfig config, string aggregate, List<Finding> findings)
    {
        var children = ChildFolders(aggregate);

        foreach (var child in children)
        {
            var name = Path.GetFileName(child);

            if (AggregateSubFolders.Contains(name))
            {
                if (string.Equals(name, "Exceptions", StringComparison.OrdinalIgnoreCase))
                {
                    CheckExceptionsFiles(config, child, findings);
                }
                else if (string.Equals(name, "Events", StringComparison.OrdinalIgnoreCase))
                {
                    CheckEventsFiles(config, child, findings);
                }
                else
                {
                    CheckUseCaseFilesOnly(config, child, "grouping", findings);
                    CheckGroupingEventFiles(config, child, findings);
                }

                continue;
            }

            if (IsActFolder(child, name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Events, Exceptions, Parts, Facts or root-action folder.", child, 1, null));
        }

        foreach (var file in ChildFiles(aggregate))
        {
            var fileName = Path.GetFileName(file);

            if (fileName.EndsWith("Exception.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an exception beside its aggregate root; it belongs in 'Exceptions/'.", file, 1, null));
            }
            else if (fileName.EndsWith("Event.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an event outside its Events/ folder; it belongs in 'Events/'.", file, 1, null));
            }
            else if (fileName.EndsWith("State.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a state beside its aggregate root; it belongs in 'Parts/'.", file, 1, null));
            }
        }

        CheckStoreInterfaces(config, aggregate, findings);

        CheckUngrouped(config, aggregate, children, child => AggregateSubFolders.Contains(Path.GetFileName(child)) || IsActFolder(child, Path.GetFileName(child)), "group them into Events/, Exceptions/, Parts/, Facts/ or one folder per root action", findings, [".cs"]);
    }

    /// <summary>Reports a second store interface beside one aggregate root: the repository interface is the aggregate's only store interface.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckStoreInterfaces(ConsumerConfig config, string aggregate, List<Finding> findings)
    {
        var faces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in ChildFiles(aggregate))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var face in InterfacesOf(file))
            {
                if (face.Name.EndsWith("Repository", StringComparison.Ordinal))
                {
                    faces.Add(face.Name);
                }
            }
        }

        if (faces.Count > 1)
        {
            var held = string.Join(", ", faces.Order(StringComparer.Ordinal).Select(face => $"'{face}'"));
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, aggregate)}' declares {faces.Count} store interfaces ({held}); the repository interface is the aggregate's only store interface.", aggregate, 1, null));
        }
    }

    /// <summary>Reports every event or exception file under one Parts or Facts grouping, which hold entities, values, shared rules and passed-in values only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="grouping">The absolute Parts or Facts folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckGroupingEventFiles(ConsumerConfig config, string grouping, List<Finding> findings)
    {
        foreach (var file in ChildFiles(grouping))
        {
            var fileName = Path.GetFileName(file);

            if (fileName.EndsWith("Exception.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an exception outside its Exceptions/ folder; it belongs in 'Exceptions/'.", file, 1, null));
            }
            else if (fileName.EndsWith("Event.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an event outside its Events/ folder; it belongs in 'Events/'.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every exception file under one aggregate Events folder, which holds domain events only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="events">The absolute Events folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckEventsFiles(ConsumerConfig config, string events, List<Finding> findings)
    {
        foreach (var file in ChildFiles(events))
        {
            if (Path.GetFileName(file).EndsWith("Exception.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an exception outside its Exceptions/ folder; it belongs in 'Exceptions/'.", file, 1, null));
            }
        }
    }

    /// <summary>Whether a Domain aggregate child folder is one root action's folder: it holds the root partial named for the act.</summary>
    /// <param name="folder">The absolute child folder under the aggregate.</param>
    /// <param name="name">The child folder name as written on disk.</param>
    /// <returns>True when a file directly inside reads '{Root}.{Act}.cs' for the folder act.</returns>
    private static bool IsActFolder(string folder, string name)
    {
        foreach (var file in ChildFiles(folder))
        {
            var stem = Path.GetFileNameWithoutExtension(file);

            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dot = stem.LastIndexOf('.');

            if (dot > 0 && SameName(stem[(dot + 1)..], name))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Reports every event file under one aggregate Exceptions folder, which holds refusals only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="exceptions">The absolute Exceptions folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckExceptionsFiles(ConsumerConfig config, string exceptions, List<Finding> findings)
    {
        foreach (var file in ChildFiles(exceptions))
        {
            if (Path.GetFileName(file).EndsWith("Event.cs", StringComparison.Ordinal))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an event outside its Events/ folder; it belongs in 'Events/'.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a module Application folder that names no declared aggregate or module-level use case of the module and no fixed Application folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="application">The absolute Application folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckApplicationLevel(ConsumerConfig config, string application, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        // A use case whose record sits under an aggregate lives in its plural aggregate
        // folder; only a module-level use case lives directly under Application.
        var owned = declared
            .Where(useCase => SameName(moduleName, useCase.Module))
            .ToList();

        foreach (var child in ChildFolders(application))
        {
            var name = Path.GetFileName(child);

            if (ApplicationFolders.Contains(name))
            {
                if (string.Equals(name, "Settings", StringComparison.OrdinalIgnoreCase))
                {
                    CheckSettingsLevel(config, child, moduleName, findings);
                }
                else
                {
                    CheckHomeLevel(config, child, moduleName, name, homes, findings);
                }

                continue;
            }

            if (owned.Count == 0)
            {
                CheckUseCaseFolder(config, child, "Application", findings);
                continue;
            }

            if (CheckLayerChildFolder(config, child, "Application", moduleName, aggregates, owned, findings))
            {
                continue;
            }

            findings.Add(new Finding(
                "code-placement-drift",
                $"Folder '{PathText.Relative(config.Root, child)}' names no declared use case.",
                child,
                1,
                null));
        }

        foreach (var file in ChildFiles(application))
        {
            if (owned.Count > 0)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Application/, which holds only one folder per aggregate and use case, Notifications, ExternalSystems, ReadModels and Settings.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a module WebApi folder that names no declared aggregate or module-level use case of the module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="webApi">The absolute WebApi folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWebApiLevel(ConsumerConfig config, string webApi, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, List<Finding> findings)
    {
        var owned = declared
            .Where(useCase => SameName(moduleName, useCase.Module))
            .ToList();

        foreach (var child in ChildFolders(webApi))
        {
            if (owned.Count == 0)
            {
                CheckUseCaseFolder(config, child, "WebApi", findings);
                continue;
            }

            if (CheckLayerChildFolder(config, child, "WebApi", moduleName, aggregates, owned, findings))
            {
                continue;
            }

            findings.Add(new Finding(
                "code-placement-drift",
                $"Folder '{PathText.Relative(config.Root, child)}' names no declared use case.",
                child,
                1,
                null));
        }

        foreach (var file in ChildFiles(webApi))
        {
            if (owned.Count > 0)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in WebApi/, which holds only one folder per aggregate and use case.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a module Pipeline folder that holds folders of its own, and the Pipeline folder past 20 files with no protocol folder. The top keeps the module's HTTP pipeline pieces serving no single use case; past 20 files, one folder per protocol holds that protocol's wire types beside its pieces.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="pipeline">The absolute Pipeline folder of one module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPipelineLevel(ConsumerConfig config, string pipeline, List<Finding> findings)
    {
        var children = ChildFolders(pipeline);

        foreach (var child in children)
        {
            CheckUseCaseFilesOnly(config, child, "protocol", findings);
        }

        CheckUngrouped(config, pipeline, children, _ => true, "group them into one folder per protocol", findings, [".cs"]);
    }

    /// <summary>Reports every folder under a module Infrastructure folder that names no Persistence folder, no Messaging folder, no Pipeline folder and no port folder of the module: each adapter folder pairs with one port folder of the same name. An adapter folder naming a provider module is drift: a module reaches another module through its Contracts, not an adapter.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="infrastructure">The absolute Infrastructure folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="portNames">The port folder names under the module Ports folder.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckInfrastructureLevel(ConsumerConfig config, string infrastructure, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<string> portNames, List<string> modules, List<Finding> findings)
    {
        foreach (var child in ChildFolders(infrastructure))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Persistence", StringComparison.OrdinalIgnoreCase))
            {
                CheckPersistenceLevel(config, child, moduleName, aggregates, findings);
                continue;
            }

            if (string.Equals(name, "Messaging", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "folder", findings);
                continue;
            }

            if (string.Equals(name, "Pipeline", StringComparison.OrdinalIgnoreCase))
            {
                CheckPipelineLevel(config, child, findings);
                continue;
            }

            if (modules.Count > 0 && modules.FirstOrDefault(declared => SameName(name, declared)) is { } provider)
            {
                findings.Add(new Finding("code-placement-drift", $"Adapter folder '{PathText.Relative(config.Root, child)}' names the module '{provider}'; a module reaches another module through its Contracts, not an adapter.", child, 1, null));
            }

            CheckAdapterFolder(config, child, findings);

            if (portNames.Any(port => SameName(name, port)))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Adapter folder '{PathText.Relative(config.Root, child)}' names no port folder; its port sits in 'Ports/{name}/'.", child, 1, null));
        }
    }

    /// <summary>
    ///     Reports the shape of one adapter folder: a subfolder outside Models/ and Parts/, a type beside the
    ///     adapter class instead of Models/ or Parts/, and a Models/ or Parts/ type no adapter file names. The top
    ///     keeps the adapter class with its options class; the external system's wire types sit in Models/, and the
    ///     collaborators the adapter needs sit in Parts/. Several adapters of one port sit side by side.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckAdapterFolder(ConsumerConfig config, string adapter, List<Finding> findings)
    {
        foreach (var child in ChildFolders(adapter))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Models", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Parts", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in adapter '{Path.GetFileName(adapter)}', which holds only its Models/ and Parts/ groupings.", child, 1, null));
        }

        var adapterName = Path.GetFileName(adapter);
        var relative = PathText.Relative(config.Root, adapter);
        var types = AdapterTypesOf(adapter);

        // A named type keeps its own parts: the check follows the adapter files one level
        // into the types they name, so a wire part the adapter names only through its response stays.
        var named = AdapterFileIdentifiers(adapter);

        foreach (var type in types)
        {
            if (!IsDirectChild(adapter, type.File) && named.Contains(type.Name))
            {
                named.UnionWith(type.SignatureNames);
            }
        }

        foreach (var type in types.OrderBy(candidate => candidate.File, StringComparer.Ordinal).ThenBy(candidate => candidate.Line))
        {
            if (IsDirectChild(adapter, type.File))
            {
                if (!IsAdapterType(type, adapterName) && !Path.GetFileName(type.File).EndsWith("Options.cs", StringComparison.Ordinal))
                {
                    findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in adapter folder '{relative}' sits beside the adapter; it belongs in 'Models/' when the external system shapes it, or in 'Parts/' when the adapter calls it.", type.File, type.Line, null));
                }
            }
            else if (!named.Contains(type.Name))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in adapter folder '{relative}' is named by no adapter file; move it to Domain/{{Aggregates}}/ or the use case folder that uses it.", type.File, type.Line, null));
            }
        }
    }

    /// <summary>Whether a declared type is one of its adapter folder's adapters: a class implementing the port interface, or a class named for an adapter. Several adapters of one port sit side by side.</summary>
    /// <param name="type">The declared type under test.</param>
    /// <param name="port">The adapter folder name as written on disk.</param>
    /// <returns>True for a class implementing the port interface or named with the Adapter suffix; otherwise false.</returns>
    private static bool IsAdapterType(PortDeclaredType type, string port) =>
        !type.IsInterface
        && (type.Name.EndsWith("Adapter", StringComparison.Ordinal)
            || type.BaseNames.Any(baseName => string.Equals(baseName, "I" + port, StringComparison.OrdinalIgnoreCase)));

    /// <summary>The simple type names the C# files directly in one adapter folder name anywhere in their text: the names the adapter and options files state.</summary>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <returns>The identifier names; a file that cannot be read reads as empty.</returns>
    private static HashSet<string> AdapterFileIdentifiers(string adapter)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in ChildFiles(adapter))
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(file))
            {
                continue;
            }

            SyntaxTree tree;

            try
            {
                tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var identifier in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                if (!identifier.Identifier.IsMissing && identifier.Identifier.Span.Length > 0)
                {
                    names.Add(identifier.Identifier.ValueText);
                }
            }
        }

        return names;
    }

    /// <summary>Reports every folder under a module Persistence folder that names no declared aggregate of the module, every file directly at the root outside module-wide store setup, at any count, every use case message, result or transport model under it, and every adapter file under it. The Persistence root holds only module-wide store setup; each aggregate's repository implementation, stored documents and mapping sit in its own aggregate folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="persistence">The absolute Persistence folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPersistenceLevel(ConsumerConfig config, string persistence, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, List<Finding> findings)
    {
        foreach (var child in ChildFolders(persistence))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate of module '{moduleName}'.", child, 1, null));
            }
        }

        var setup = moduleName + "StoreContribution.cs";

        foreach (var file in ChildFiles(persistence))
        {
            var fileName = Path.GetFileName(file);

            // A use case model or adapter file at the root is reported by the scan
            // below with its own remedy, so the root stays silent for those shapes.
            if (string.Equals(fileName, setup, StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || UseCaseModelSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal))
                || AdapterSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Persistence/, which holds only module-wide store setup ('{setup}') and one folder per aggregate.", file, 1, null));
        }

        List<string> files;

        try
        {
            files = Directory.EnumerateFiles(persistence, "*.cs", SearchOption.AllDirectories)
                .Where(file => !PathText.Relative(persistence, file).Split('/').Any(segment => SourceTree.SkippedFolders.Contains(segment)))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);

            if (UseCaseModelSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds a message, result or transport model, which sits in its use case folder, not in Persistence/.", file, 1, null));
            }
            else if (AdapterSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal)))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds an adapter, which sits in its Infrastructure/{{Port}}/ folder, not in Persistence/.", file, 1, null));
            }
        }
    }

    /// <summary>
    ///     Checks one folder under a use case layer folder: a module-level use case folder holds its use case files, a plural
    ///     aggregate folder of an aggregate that has use cases holds only its use case folders, and anything else reports.
    ///     A use case folder flat under the layer that names an aggregate-owned use case names the aggregate folder it sits in.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="child">The absolute child folder under the layer folder.</param>
    /// <param name="layer">The layer folder holding the child: Application or WebApi.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="owned">The declared use cases of the module.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the child was checked, so the caller reports nothing more for it.</returns>
    private static bool CheckLayerChildFolder(ConsumerConfig config, string child, string layer, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> owned, List<Finding> findings)
    {
        var name = Path.GetFileName(child);

        if (owned.Any(useCase => useCase.Aggregate is null && SameName(name, useCase.Name)))
        {
            CheckUseCaseFolder(config, child, layer, findings);
            return true;
        }

        var under = owned
            .Where(useCase => useCase.Aggregate is not null && SameName(name, ExpectedOf(aggregates, moduleName, useCase.Aggregate)))
            .Select(useCase => useCase.Name)
            .ToList();

        if (under.Count > 0)
        {
            CheckAggregateUseCaseLevel(config, child, name, under, layer, findings);
            return true;
        }

        var flat = owned.FirstOrDefault(useCase => useCase.Aggregate is not null && SameName(name, useCase.Name));

        if (flat.Name is not null)
        {
            var expected = ExpectedOf(aggregates, moduleName, flat.Aggregate!);
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names the use case '{flat.Name}', which sits in its aggregate folder '{expected}/'.", child, 1, null));
            return true;
        }

        foreach (var aggregate in aggregates)
        {
            if (!SameName(moduleName, aggregate.Module) || !SameAggregate(name, aggregate.Aggregate))
            {
                continue;
            }

            // A plural folder of an aggregate that holds no use case stays with the caller,
            // which names the use case folders the level holds. A singular folder reports here.
            if (SameName(name, aggregate.Expected))
            {
                return false;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate.", child, 1, null));
            return true;
        }

        return false;
    }

    /// <summary>Reports every folder under a plural aggregate folder under a layer folder that names no use case of that aggregate, and every file directly beside them.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under the layer folder.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="under">The declared use case names under the aggregate.</param>
    /// <param name="layer">The layer folder holding the aggregate folder: Application or WebApi.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateUseCaseLevel(ConsumerConfig config, string aggregate, string name, IReadOnlyList<string> under, string layer, List<Finding> findings)
    {
        foreach (var child in ChildFolders(aggregate))
        {
            var childName = Path.GetFileName(child);

            if (under.Any(useCase => SameName(childName, useCase)))
            {
                CheckUseCaseFolder(config, child, layer, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no use case of aggregate '{name}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(aggregate))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in {name}/, which holds only one folder per use case.", file, 1, null));
        }
    }

    /// <summary>The expected plural folder of an aggregate folder: the declared aggregate's plural, or the folder itself when no declared aggregate matches.</summary>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="module">The module folder name as written on disk.</param>
    /// <param name="folder">The aggregate folder the use case sits under.</param>
    /// <returns>The expected plural folder.</returns>
    private static string ExpectedOf(IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, string module, string folder)
    {
        foreach (var aggregate in aggregates)
        {
            if (SameName(module, aggregate.Module) && SameAggregate(folder, aggregate.Aggregate))
            {
                return aggregate.Expected;
            }
        }

        return Naming.Pascal(folder);
    }

    /// <summary>
    ///     Reports every folder under a module Notifications, ExternalSystems or ReadModels folder that names no declared
    ///     record of its kind, every file directly beside them, and every file or folder under a named child, which
    ///     holds files only.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="container">The absolute Notifications, ExternalSystems or ReadModels folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="containerName">The container folder name: Notifications, ExternalSystems or ReadModels.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckHomeLevel(ConsumerConfig config, string container, string moduleName, string containerName, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        var kind = containerName switch
        {
            "Notifications" => "notification a reaction or job of the module sends",
            "ExternalSystems" => "external system a reaction or job of the module calls",
            _ => "read model of the module",
        };

        foreach (var child in ChildFolders(container))
        {
            var name = Path.GetFileName(child);

            if (homes.Any(home => SameName(moduleName, home.Module) && string.Equals(home.Container, containerName, StringComparison.OrdinalIgnoreCase) && SameName(name, home.Folder)))
            {
                CheckUseCaseFilesOnly(config, child, containerName, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no {kind}.", child, 1, null));
        }

        foreach (var file in ChildFiles(container))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in {containerName}/, which holds only one folder per record it names.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under a module Settings folder, which holds files only, and every file in it that is not the module options class.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="settings">The absolute Settings folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSettingsLevel(ConsumerConfig config, string settings, string moduleName, List<Finding> findings)
    {
        foreach (var child in ChildFolders(settings))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in 'Settings/', which holds files only.", child, 1, null));
        }

        var options = moduleName + "Options.cs";

        foreach (var file in ChildFiles(settings))
        {
            if (!string.Equals(Path.GetFileName(file), options, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits in 'Settings/', which holds only '{options}'.", file, 1, null));
            }
        }
    }

    /// <summary>
    ///     Reports every file directly in a module Ports folder, which holds only one folder
    ///     per port, every port folder with no adapter folder of the same name, every port
    ///     folder naming a provider module, and every port folder spelling its external-system
    ///     record under another name, and checks each port folder for its interface and named types.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="ports">The absolute Ports folder of one module.</param>
    /// <param name="adapterNames">The adapter folder names under the module Infrastructure folder, without Persistence.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortsLevel(ConsumerConfig config, string ports, IReadOnlyList<string> adapterNames, List<string> modules, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, List<Finding> findings)
    {
        foreach (var child in ChildFolders(ports))
        {
            var name = Path.GetFileName(child);

            if (modules.Count > 0 && modules.FirstOrDefault(declared => SameName(name, declared)) is { } provider)
            {
                findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, child)}' names the module '{provider}'; a module reaches another module through its Contracts, not a port.", child, 1, null));
            }

            CheckPortRecordName(config, child, name, portMarks, externalNames, findings);
            CheckPortFolder(config, child, findings);

            if (!adapterNames.Any(adapter => SameName(name, adapter)))
            {
                findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, child)}' names no adapter folder; its adapter sits in 'Infrastructure/{name}/'.", child, 1, null));
            }
        }

        foreach (var file in ChildFiles(ports))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Ports/, which holds only one folder per port.", file, 1, null));
        }
    }

    /// <summary>Reports the port folder whose interface marks an external-system record the folder spells under another name: the folder spells the record name in PascalCase. An in-process port carries no mark and keeps the name of its capability.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <param name="name">The port folder name as written on disk.</param>
    /// <param name="portMarks">The external-system marks each port interface carries, by file.</param>
    /// <param name="externalNames">The record name of each declared external system, by identifier.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPortRecordName(ConsumerConfig config, string port, string name, IReadOnlyList<(string File, string RecordId)> portMarks, IReadOnlyDictionary<string, string> externalNames, List<Finding> findings)
    {
        var marked = portMarks
            .Where(mark => SourceRoots.IsUnder(port, mark.File))
            .Select(mark => mark.RecordId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (marked.Count == 0)
        {
            return;
        }

        foreach (var id in marked)
        {
            if (externalNames.TryGetValue(id, out var record) && SameName(name, record))
            {
                return;
            }
        }

        if (!externalNames.TryGetValue(marked[0], out var first))
        {
            return;
        }

        findings.Add(new Finding("code-placement-drift", $"Port folder '{PathText.Relative(config.Root, port)}' carries the port for '{marked[0]}', so it belongs in 'Ports/{Naming.Pascal(first)}/'.", port, 1, null));
    }

    /// <summary>
    ///     Reports the shape of one port folder: a subfolder outside Models/, no interface or more than
    ///     one, an interface whose name is not the folder name with an I prefix, a type beside the
    ///     interface instead of Models/, and a Models/ type no member signature of the interface names,
    ///     directly or through one named type. The top keeps the interface; every type its signatures
    ///     name sits in Models/.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/packs/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortFolder(ConsumerConfig config, string port, List<Finding> findings)
    {
        foreach (var child in ChildFolders(port))
        {
            if (string.Equals(Path.GetFileName(child), "Models", StringComparison.OrdinalIgnoreCase))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in port '{Path.GetFileName(port)}', which holds only its Models/ grouping.", child, 1, null));
        }

        var name = Path.GetFileName(port);
        var relative = PathText.Relative(config.Root, port);
        var types = PortTypesOf(port);
        var faces = types.Where(type => type.IsInterface && IsDirectChild(port, type.File)).OrderBy(type => type.Name, StringComparer.Ordinal).ToList();

        if (faces.Count == 0)
        {
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds no interface; it holds the interface 'I{name}' with the types its signatures name in Models/.", port, 1, null));
            return;
        }

        if (faces.Count > 1)
        {
            var held = string.Join(", ", faces.Select(face => $"'{face.Name}'"));
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds {faces.Count} interfaces ({held}); it holds only the interface 'I{name}' with the types its signatures name in Models/.", port, 1, null));
            return;
        }

        var face = faces[0];

        if (!string.Equals(face.Name, "I" + name, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding("code-placement-drift", $"Interface '{face.Name}' sits in port folder '{relative}', whose interface is 'I{name}'.", face.File, face.Line, null));
        }

        // A named type keeps its own parts: the check follows the interface signatures one level
        // into the types they name, so a result part the interface names only through its result stays.
        // Only the interface's own names open the second level; names found there open no third.
        var direct = new HashSet<string>(face.SignatureNames, StringComparer.Ordinal);
        var named = new HashSet<string>(direct, StringComparer.Ordinal);

        foreach (var type in types)
        {
            if (!type.IsInterface && direct.Contains(type.Name))
            {
                named.UnionWith(type.SignatureNames);
            }
        }

        foreach (var type in types.OrderBy(candidate => candidate.File, StringComparer.Ordinal).ThenBy(candidate => candidate.Line))
        {
            if (string.Equals(type.Name, face.Name, StringComparison.Ordinal) && string.Equals(type.File, face.File, StringComparison.Ordinal))
            {
                continue;
            }

            if (IsDirectChild(port, type.File))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in port folder '{relative}' sits beside the interface '{face.Name}'; it belongs in 'Models/'.", type.File, type.Line, null));
            }
            else if (!named.Contains(type.Name))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in port folder '{relative}' is named by no signature of '{face.Name}'; move it to Domain/{{Aggregates}}/ or the use case folder that uses it.", type.File, type.Line, null));
            }
        }
    }

    /// <summary>Whether the file sits directly in the folder rather than in one of its groupings.</summary>
    /// <param name="folder">The absolute folder.</param>
    /// <param name="file">The absolute file path.</param>
    /// <returns>True when the file's folder is the folder itself.</returns>
    private static bool IsDirectChild(string folder, string file) =>
        string.Equals(Path.GetDirectoryName(file), folder, StringComparison.OrdinalIgnoreCase);

    /// <summary>One top-level type declared in a port or adapter folder: its name, file, line, whether it is an interface, the simple type names its member signatures name, and the simple names its base list states.</summary>
    /// <param name="Name">The simple type name as declared.</param>
    /// <param name="File">The file declaring the type.</param>
    /// <param name="Line">The 1-based line of the type name.</param>
    /// <param name="IsInterface">Whether the declaration is an interface.</param>
    /// <param name="SignatureNames">The simple type names its members name: parameter and return types with their type arguments, then property, field and event types.</param>
    /// <param name="BaseNames">The simple type names its base list states: base classes and implemented interfaces with their type arguments.</param>
    sealed record PortDeclaredType(string Name, string File, int Line, bool IsInterface, HashSet<string> SignatureNames, HashSet<string> BaseNames);

    /// <summary>The top-level types each C# file in a port folder declares, in file order, with the simple type names each member signature names: the interface beside its Models/ grouping.</summary>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> PortTypesOf(string port)
    {
        var files = ChildFiles(port);
        var models = ChildFolders(port).FirstOrDefault(child => string.Equals(Path.GetFileName(child), "Models", StringComparison.OrdinalIgnoreCase));

        if (models is not null)
        {
            files.AddRange(ChildFiles(models));
        }

        return DeclaredTypesOf(files);
    }

    /// <summary>The top-level types each C# file in an adapter folder declares, in file order: the adapter and options files beside the Models/ and Parts/ groupings.</summary>
    /// <param name="adapter">The absolute adapter folder under Infrastructure.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> AdapterTypesOf(string adapter)
    {
        var files = ChildFiles(adapter);

        foreach (var child in ChildFolders(adapter))
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, "Models", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Parts", StringComparison.OrdinalIgnoreCase))
            {
                files.AddRange(ChildFiles(child));
            }
        }

        return DeclaredTypesOf(files);
    }

    /// <summary>The top-level types the C# files declare, in file order, with the simple type names each member signature names.</summary>
    /// <param name="files">The absolute files to read.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> DeclaredTypesOf(List<string> files)
    {
        var types = new List<PortDeclaredType>();

        foreach (var file in files)
        {
            if (!file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SyntaxTree tree;

            try
            {
                if (!File.Exists(file))
                {
                    continue;
                }

                tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var root = tree.GetRoot();

            foreach (var declared in root.DescendantNodes().OfType<MemberDeclarationSyntax>())
            {
                if (declared is not TypeDeclarationSyntax and not EnumDeclarationSyntax and not DelegateDeclarationSyntax)
                {
                    continue;
                }

                if (declared.Parent is TypeDeclarationSyntax)
                {
                    continue;
                }

                // A C# 14 extension block is a type declaration with no name, so it names no type.
                if (declared is TypeDeclarationSyntax extension
                    && (extension.Identifier.IsMissing || extension.Identifier.Span.Length == 0))
                {
                    continue;
                }

                var names = new HashSet<string>(StringComparer.Ordinal);
                var bases = new HashSet<string>(StringComparer.Ordinal);

                if (declared is TypeDeclarationSyntax type)
                {
                    if (type.BaseList is not null)
                    {
                        foreach (var baseType in type.BaseList.Types)
                        {
                            AddSignatureNames(baseType.Type, bases);
                        }
                    }

                    if (type.ParameterList is not null)
                    {
                        foreach (var parameter in type.ParameterList.Parameters)
                        {
                            AddSignatureNames(parameter.Type, names);
                        }
                    }

                    foreach (var member in type.Members)
                    {
                        AddMemberNames(member, names);
                    }
                }
                else if (declared is DelegateDeclarationSyntax delegateType)
                {
                    AddSignatureNames(delegateType.ReturnType, names);

                    foreach (var parameter in delegateType.ParameterList.Parameters)
                    {
                        AddSignatureNames(parameter.Type, names);
                    }
                }

                var identifier = declared switch
                {
                    TypeDeclarationSyntax named => named.Identifier,
                    EnumDeclarationSyntax named => named.Identifier,
                    DelegateDeclarationSyntax named => named.Identifier,
                    _ => default,
                };
                var line = tree.GetLineSpan(identifier.Span).StartLinePosition.Line + 1;
                types.Add(new PortDeclaredType(identifier.ValueText, file, line, declared is InterfaceDeclarationSyntax, names, bases));
            }
        }

        return types;
    }

    /// <summary>Adds the simple type names one member signature names: a method's return and parameter types, a property, field or event type, or an indexer's type and parameter types.</summary>
    /// <param name="member">The member whose signature is read.</param>
    /// <param name="names">The names collected so far.</param>
    private static void AddMemberNames(MemberDeclarationSyntax member, HashSet<string> names)
    {
        switch (member)
        {
            case MethodDeclarationSyntax method:
                AddSignatureNames(method.ReturnType, names);

                foreach (var parameter in method.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            case PropertyDeclarationSyntax property:
                AddSignatureNames(property.Type, names);
                break;
            case FieldDeclarationSyntax field:
                AddSignatureNames(field.Declaration.Type, names);
                break;
            case EventFieldDeclarationSyntax fieldEvent:
                AddSignatureNames(fieldEvent.Declaration.Type, names);
                break;
            case EventDeclarationSyntax declaredEvent:
                AddSignatureNames(declaredEvent.Type, names);
                break;
            case IndexerDeclarationSyntax indexer:
                AddSignatureNames(indexer.Type, names);

                foreach (var parameter in indexer.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            default:
                break;
        }
    }

    /// <summary>Adds the simple names one type use states: its own name with each nested type argument name, unqualified and without nullability or array ranks.</summary>
    /// <param name="type">The type syntax as written, or null when the signature states none.</param>
    /// <param name="names">The names collected so far.</param>
    private static void AddSignatureNames(TypeSyntax? type, HashSet<string> names)
    {
        switch (type)
        {
            case null:
                break;
            case IdentifierNameSyntax plain:
                names.Add(plain.Identifier.ValueText);
                break;
            case GenericNameSyntax generic:
                names.Add(generic.Identifier.ValueText);

                foreach (var argument in generic.TypeArgumentList.Arguments)
                {
                    AddSignatureNames(argument, names);
                }

                break;
            case QualifiedNameSyntax qualified:
                AddSignatureNames(qualified.Right, names);
                break;
            case AliasQualifiedNameSyntax alias:
                AddSignatureNames(alias.Name, names);
                break;
            case NullableTypeSyntax nullable:
                AddSignatureNames(nullable.ElementType, names);
                break;
            case ArrayTypeSyntax array:
                AddSignatureNames(array.ElementType, names);
                break;
            case TupleTypeSyntax tuple:
                foreach (var element in tuple.Elements)
                {
                    AddSignatureNames(element.Type, names);
                }

                break;
            case PointerTypeSyntax pointer:
                AddSignatureNames(pointer.ElementType, names);
                break;
            case FunctionPointerTypeSyntax pointer:
                // The last function pointer parameter states the return type.
                foreach (var parameter in pointer.ParameterList.Parameters)
                {
                    AddSignatureNames(parameter.Type, names);
                }

                break;
            default:
                break;
        }
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

    /// <summary>Reports every folder under the shared UI package components folder outside its role groupings, and the folder past 20 files with none of them. The top keeps the package index.</summary>
    /// <param name="config">The project configuration that holds the consumer root and the shared package root.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSharedComponents(ConsumerConfig config, List<Finding> findings)
    {
        if (config.SharedUiPackageRoot.Length == 0)
        {
            return;
        }

        var components = Path.Combine(config.SharedUiPackageRoot, "src", "components");

        if (!Directory.Exists(components))
        {
            return;
        }

        var children = ChildFolders(components);

        foreach (var child in children)
        {
            var name = Path.GetFileName(child);

            if (UiComponentGroups.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no shared component role; components/ holds fields/, overlays/, layout/, data/, values/ and feedback/.", child, 1, null));
        }

        CheckUngrouped(config, components, children, child => UiComponentGroups.Contains(Path.GetFileName(child)), "group them into fields/, overlays/, layout/, data/, values/ or feedback/", findings);
    }

    /// <summary>
    ///     Reports the layout of one use case folder: any subfolder, each file that sits in the
    ///     wrong layer, and the folder past 20 files. A transport type, such as an endpoint or a
    ///     request or response model, belongs in WebApi, and every other file belongs in
    ///     Application. The folder decides what a model or row is: beside the endpoint it shapes
    ///     the wire contract, while in Application it is a result part the query projects. The
    ///     folder stays flat and small once fan-out lives in a job.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The absolute use case folder under Application or WebApi.</param>
    /// <param name="layer">The layer folder holding the use case folder: Application or WebApi.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/backend-application#use-this-use-case-layout</follows>
    private static void CheckUseCaseFolder(ConsumerConfig config, string useCase, string layer, List<Finding> findings)
    {
        CheckUseCaseFilesOnly(config, useCase, "use case", findings);
        CheckTransportFiles(config, useCase, layer, findings);
        CheckUngrouped(config, useCase, ChildFolders(useCase), _ => false, "a use case folder stays flat and small, so move fan-out into a job", findings, [".cs"]);
    }

    /// <summary>
    ///     Reports each file in a use case folder that sits in the wrong layer: a transport type, such as an
    ///     endpoint or a request or response model, belongs in WebApi, and every other file belongs in
    ///     Application. The folder decides what a model or row is: beside the endpoint it shapes the wire
    ///     contract, while in Application it is a result part the query projects.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The use case folder under Application or WebApi.</param>
    /// <param name="layer">The layer folder holding the use case folder: Application or WebApi.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/backend-application#use-this-use-case-layout</follows>
    private static void CheckTransportFiles(ConsumerConfig config, string useCase, string layer, List<Finding> findings)
    {
        var inWebApi = string.Equals(layer, "WebApi", StringComparison.OrdinalIgnoreCase);

        foreach (var file in ChildFiles(useCase))
        {
            var fileName = Path.GetFileName(file);
            var transport = inWebApi
                ? WebApiSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal))
                : TransportSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.Ordinal));

            if (transport && !inWebApi)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' is a transport type, which sits in WebApi/, not {layer}/.", file, 1, null));
            }
            else if (!transport && inWebApi)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' is not a transport type, which sits in Application/, not WebApi/.", file, 1, null));
            }
        }
    }

    /// <summary>Folder names no folder under an application's source tree carries: each accepts anything, so each file belongs in the folder that owns it instead.</summary>
    static readonly HashSet<string> RefusedFrontendFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "shared", "common", "utils", "util", "helpers", "misc", "components", "hooks", "types", "services",
    };

    /// <summary>Reports every module folder under one frontend modules root that names no declared module, every folder under it carrying a refused name, every folder segment outside kebab case, and every shared app folder outside its area groupings. An Android frontend keeps no module mirror, so only its module folders are checked.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose modules root is checked.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckFrontendRoot(ConsumerConfig config, FrontendConfig frontend, List<string> modules, List<Finding> findings)
    {
        if (!frontend.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            CheckAppShared(config, frontend, findings);
        }

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

        if (frontend.Platform.Equals("android", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var folder in WalkFolders(frontend.ModulesRoot))
        {
            var name = Path.GetFileName(folder);

            if (RefusedFrontendFolders.Contains(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, folder)}' carries the refused name '{name}'; move its files into the folder that owns them.", folder, 1, null));
            }
            else if (!IsKebabCase(name))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, folder)}' sits outside kebab case; spell it '{Naming.Kebab(name)}'.", folder, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a web frontend shared app folder outside its area groupings, and every file directly at its root outside the generated records file, at any count. The top keeps only the generated records file; shared session, messages, calls and layout sit in their areas.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="frontend">The frontend whose shared app folder is checked.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAppShared(ConsumerConfig config, FrontendConfig frontend, List<Finding> findings)
    {
        var app = frontend.FrameRoot;

        if (!Directory.Exists(app))
        {
            return;
        }

        foreach (var child in ChildFolders(app))
        {
            var name = Path.GetFileName(child);

            if (AppGroups.Contains(name))
            {
                CheckUseCaseFilesOnly(config, child, "grouping", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no shared app area; the shared app folder holds session/, i18n/, api/ and ui/.", child, 1, null));
        }

        foreach (var file in ChildFiles(app))
        {
            if (string.Equals(Path.GetFileName(file), "litespec-records.ts", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in the shared app folder, which holds only 'litespec-records.ts' and its area folders; move it into session/, i18n/, api/ or ui/.", file, 1, null));
        }
    }

    /// <summary>Whether a folder name is kebab case: lowercase letters and digits in hyphen-separated words.</summary>
    /// <param name="name">The folder name as written on disk.</param>
    /// <returns>True when the name holds lowercase words joined by single hyphens; otherwise false.</returns>
    private static bool IsKebabCase(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        var word = false;

        foreach (var character in name)
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                word = true;
            }
            else if (character == '-' && word)
            {
                word = false;
            }
            else
            {
                return false;
            }
        }

        return word;
    }

    /// <summary>Every folder under one root in ordinal order, without build output and tool folders.</summary>
    /// <param name="root">The absolute root to walk.</param>
    /// <returns>The folders under the root in ordinal order.</returns>
    private static List<string> WalkFolders(string root)
    {
        var found = new List<string>();
        var stack = new Stack<string>(ChildFolders(root).Reverse<string>());

        while (stack.Count > 0)
        {
            var folder = stack.Pop();
            found.Add(folder);

            foreach (var child in ChildFolders(folder).Reverse<string>())
            {
                stack.Push(child);
            }
        }

        found.Sort(StringComparer.Ordinal);

        return found;
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

    /// <summary>The external-system marks each port interface carries, by file.</summary>
    /// <param name="model">The loaded project model that holds each marked port.</param>
    /// <returns>The file and record identifier of each port mark naming an external system.</returns>
    private static List<(string File, string RecordId)> DeclaredPortMarks(ProjectModel model)
    {
        var marks = new List<(string File, string RecordId)>();

        foreach (var element in model.Code.Elements)
        {
            if (element.Kind != CodeKind.Port)
            {
                continue;
            }

            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Follows
                    && mark.RecordId.StartsWith(RecordTypes.ExternalSystem + "/", StringComparison.Ordinal))
                {
                    marks.Add((element.File, mark.RecordId));
                }
            }
        }

        return marks;
    }

    /// <summary>The expected aggregate folder per file whose marks name an aggregate-homed aggregate, domain event or rule.</summary>
    /// <param name="model">The loaded project model that holds every record and each marked element.</param>
    /// <returns>The expected plural aggregate folder per carrying file.</returns>
    private static Dictionary<string, string> AggregateMarkedFiles(ProjectModel model)
    {
        var marked = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var element in model.Code.Elements)
        {
            foreach (var mark in element.Marks)
            {
                if (mark.Verb != MarkVerb.Implements
                    || !model.ById.TryGetValue(mark.RecordId, out var record)
                    || record.Type is not (RecordTypes.Aggregate or RecordTypes.DomainEvent or RecordTypes.Rule))
                {
                    continue;
                }

                var path = PathOf(record);

                if (path.Module is null || path.Aggregate is null)
                {
                    continue;
                }

                marked.TryAdd(element.File, ExpectedAggregateFolder(model, path.Module, path.Aggregate));
            }
        }

        return marked;
    }

    /// <summary>The record name of each declared external system, by identifier.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The name segment of each external-system identifier.</returns>
    private static Dictionary<string, string> DeclaredExternalNames(ProjectModel model)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var record in model.Records)
        {
            if (record.Type == RecordTypes.ExternalSystem)
            {
                names.TryAdd(record.Id, NameOf(record));
            }
        }

        return names;
    }

    /// <summary>The declared aggregates by module, aggregate folder and expected plural folder.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate folder and expected plural folder of each aggregate record placed in a module folder.</returns>
    internal static List<(string Module, string Aggregate, string Expected)> DeclaredAggregates(ProjectModel model)
    {
        var aggregates = new List<(string Module, string Aggregate, string Expected)>();

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate)
            {
                continue;
            }

            var path = PathOf(record);

            if (path.Module is not null && path.Aggregate is not null)
            {
                aggregates.Add((path.Module, path.Aggregate, ExpectedAggregateFolder(model, path.Module, path.Aggregate)));
            }
        }

        return aggregates;
    }

    /// <summary>The declared use cases by module, aggregate and name.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate and name of each use case record placed in a module folder.</returns>
    internal static List<(string Module, string? Aggregate, string Name)> DeclaredUseCases(ProjectModel model)
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
    ///     The declared trigger and read model folders by module, container and folder name. A reaction or job that runs a use
    ///     case takes no folder here, since its code sits in that use case's folder under Application. A trigger that only sends
    ///     takes a Notifications folder, and one that only calls takes an ExternalSystems folder, each named for its first
    ///     effect. Each read model takes a ReadModels folder, named for its plural.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, container and folder name of each trigger and read model placed in a module folder.</returns>
    /// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
    /// <follows>litespec/packs/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</follows>
    private static List<(string Module, string Container, string Folder)> DeclaredTriggerHomes(ProjectModel model)
    {
        var homes = new List<(string Module, string Container, string Folder)>();

        foreach (var record in model.Records)
        {
            var path = PathOf(record);

            if (path.Module is null)
            {
                continue;
            }

            if (record.Type is RecordTypes.Reaction or RecordTypes.Job)
            {
                if (TriggerHomeOf(model, record) is { } home && home.Container != "Application")
                {
                    homes.Add((path.Module, home.Container, home.Name));
                }
            }
            else if (record.Type == "read-model" && path.Name is not null)
            {
                homes.Add((path.Module, "ReadModels", ReadModelFolderOf(path.Name)));
            }
        }

        return homes;
    }

    /// <summary>
    ///     Reports each message, result or transport model file in one module project that sits outside a use case
    ///     folder. A use case's own models live in its folder, so a file named for a use case outside one is drift. The
    ///     finding names the use case folder the file belongs in when the file name's prefix names a declared use case.
    ///     Files under Persistence stay with the persistence check, which names that folder.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding the files.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/core/backend-architecture#give-each-use-case-its-own-models</follows>
    private static void CheckMessageFiles(ConsumerConfig config, string modulesRoot, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        var single = SourceRoots.IsSingleModulesProject(modulesRoot);

        // A Contracts project holds public use case messages at its own use case paths, which the Contracts level owns.
        if (!single && SourceRoots.IsContractsProject(modulesRoot))
        {
            return;
        }

        var own = single ? null : SourceRoots.ModuleNameOf(modulesRoot);

        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories).OrderBy(child => child, StringComparer.Ordinal))
        {
            var segments = PathText.Relative(modulesRoot, file).Split('/');

            if (segments.Length < 2
                || segments.Any(segment => SourceTree.SkippedFolders.Contains(segment))
                || segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase)
                || segments.Any(segment => segment.Equals("Persistence", StringComparison.OrdinalIgnoreCase))
                || (single && segments[1].Equals("Contracts", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var fileName = segments[^1];
            var suffix = UseCaseModelSuffixes.FirstOrDefault(candidate => fileName.EndsWith(candidate, StringComparison.Ordinal));

            if (suffix is null || InsideUseCase(segments))
            {
                continue;
            }

            var prefix = fileName[..^suffix.Length];
            var match = single
                ? useCases.FirstOrDefault(useCase => SameName(segments[0], useCase.Module) && SameName(prefix, useCase.Name))
                : useCases.FirstOrDefault(useCase => own is not null && SameName(own, useCase.Module) && SameName(prefix, useCase.Name));
            var relative = PathText.Relative(config.Root, file);

            if (match.Module is null)
            {
                findings.Add(new Finding("code-placement-drift", $"File '{relative}' holds a message, result or transport model outside a use case folder.", file, 1, null));
                continue;
            }

            var layer = suffix is "RequestModel.cs" or "ResponseModel.cs" ? "WebApi" : "Application";
            var useCaseFolder = Naming.Pascal(match.Name);
            var home = match.Aggregate is null
                ? $"{layer}/{useCaseFolder}"
                : $"{layer}/{ExpectedOf(aggregates, match.Module, match.Aggregate)}/{useCaseFolder}";
            var belongs = single
                ? $"{segments[0]}/{home}"
                : home;

            findings.Add(new Finding("code-placement-drift", $"File '{relative}' holds a message, result or transport model outside a use case folder; it belongs in '{belongs}'.", file, 1, null));
        }
    }

    /// <summary>Whether a module-relative path sits in a use case folder under Application or WebApi.</summary>
    /// <param name="segments">The module-relative path segments, with the file name last.</param>
    /// <returns>True when a layer folder names a use case folder that holds the file.</returns>
    private static bool InsideUseCase(string[] segments)
    {
        for (var index = 0; index + 2 < segments.Length; index++)
        {
            if (UseCaseHomeLayers.Contains(segments[index]) && !FixedLayerFolders.Contains(segments[index + 1]))
            {
                return true;
            }
        }

        return false;
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
