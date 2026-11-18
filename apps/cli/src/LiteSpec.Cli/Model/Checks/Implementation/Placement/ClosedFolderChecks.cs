using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check: the Kernel, the Platform, each module project, each aggregate and use case folder, and each frontend modules root hold only their fixed folders and the folders a declared record names.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
/// <follows>litespec/backend/application#use-this-use-case-layout</follows>
/// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
internal static class ClosedFolderChecks
{
    /// <summary>Fixed folders at the Platform root: one folder per technical capability.</summary>
    static readonly HashSet<string> PlatformFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence", "Messaging", "Access", "Auditing", "Http", "CountryPacks", "Modules",
    };

    /// <summary>Fixed folders at a module project root, whatever its aggregates, use cases, triggers and read models declare.</summary>
    static readonly HashSet<string> ModuleFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Infrastructure", "WebApi",
    };

    /// <summary>The kind folders the module contracts hold: the sorted public surface.</summary>
    static readonly HashSet<string> ContractsKindFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Identifiers", "IntegrationEvents", "ModuleEvents", "Ports",
    };

    /// <summary>Fixed folders under one aggregate in Domain: its events and refusals.</summary>
    static readonly HashSet<string> AggregateSubFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Events", "Exceptions",
    };

    /// <summary>Fixed folders under a module Application folder besides its use case folders: its shared ports and trigger homes.</summary>
    static readonly HashSet<string> ApplicationFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ports", "Notifications", "ExternalSystems", "ReadModels",
    };

    /// <summary>Fixed folders under a module Infrastructure folder besides its use case folders: its shared adapters.</summary>
    static readonly HashSet<string> InfrastructureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence", "ExternalSystems",
    };

    /// <summary>The layer folders a use case folder sits under.</summary>
    static readonly HashSet<string> UseCaseHomeLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application", "WebApi", "Infrastructure",
    };

    /// <summary>Fixed folders under a layer folder that hold no use case: the shared ports, trigger homes and persistence.</summary>
    static readonly HashSet<string> FixedLayerFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Ports", "Notifications", "ExternalSystems", "ReadModels", "Persistence",
    };

    /// <summary>The file name suffixes of transport types, which sit in a use case WebApi folder and no other.</summary>
    static readonly string[] TransportSuffixes = ["Endpoint.cs", "RequestModel.cs", "ResponseModel.cs", "ResponseItemModel.cs", "PaginationModel.cs"];

    /// <summary>The file name suffixes a use case WebApi folder accepts: its endpoint and any model or row shaping the wire contract.</summary>
    static readonly string[] WebApiSuffixes = ["Endpoint.cs", "Model.cs", "Row.cs"];

    /// <summary>The file name suffixes of a use case's own message, result and transport models. The longer suffix comes first, so CommandResult is matched before Command.</summary>
    static readonly string[] UseCaseModelSuffixes = ["CommandResult.cs", "QueryResult.cs", "Command.cs", "Query.cs", "RequestModel.cs", "ResponseModel.cs"];

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

        foreach (var modulesRoot in model.Config.ModulesRoots)
        {
            if (modulesRoot.Length == 0 || !Directory.Exists(modulesRoot))
            {
                continue;
            }

            if (SourceRoots.IsSingleModulesProject(modulesRoot))
            {
                CheckSingleModulesRoot(model.Config, modulesRoot, modules, aggregates, useCases, homes, findings);
            }
            else
            {
                CheckModuleProject(model.Config, modulesRoot, modules, aggregates, useCases, homes, findings);
            }

            CheckMessageFiles(model.Config, modulesRoot, useCases, findings);
        }

        if (model.Config.KernelRoot.Length > 0 && Directory.Exists(model.Config.KernelRoot))
        {
            CheckKernelRoot(model.Config, findings);
        }

        if (model.Config.PlatformRoot.Length > 0 && Directory.Exists(model.Config.PlatformRoot))
        {
            CheckPlatformRoot(model.Config, findings);
        }

        foreach (var frontend in model.Config.Frontends)
        {
            CheckFrontendRoot(model.Config, frontend, modules, findings);
        }
    }

    /// <summary>Reports every folder under the Kernel root, which holds files only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckKernelRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.KernelRoot))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in the Kernel, which holds files only.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the Platform root that breaks its closed set of capability folders.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckPlatformRoot(ConsumerConfig config, List<Finding> findings)
    {
        foreach (var child in ChildFolders(config.PlatformRoot))
        {
            var name = Path.GetFileName(child);

            if (PlatformFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Platform folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the single modules root that breaks its level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The single modules project holding every module.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSingleModulesRoot(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        foreach (var module in ChildFolders(modulesRoot))
        {
            var name = Path.GetFileName(module);

            if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
                continue;
            }

            CheckModuleLevel(config, module, name, aggregates, useCases, homes, nestedContracts: true, findings);
        }
    }

    /// <summary>Reports the module project that names no declared module, and every folder under it that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding one module.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleProject(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        if (SourceRoots.IsContractsProject(modulesRoot))
        {
            CheckContractsProject(config, modulesRoot, modules, findings);
            return;
        }

        var name = SourceRoots.ModuleNameOf(modulesRoot) ?? Path.GetFileName(modulesRoot);

        if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, modulesRoot)}' names no declared module.", modulesRoot, 1, null));
            return;
        }

        CheckModuleLevel(config, modulesRoot, name, aggregates, useCases, homes, nestedContracts: false, findings);
    }

    /// <summary>Reports the Contracts project whose owner names no declared module, and every folder under it that breaks the kind folder set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The Contracts project holding one module's contracts.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractsProject(ConsumerConfig config, string contracts, List<string> modules, List<Finding> findings)
    {
        var owner = SourceRoots.ModuleNameOf(contracts) ?? Path.GetFileName(contracts);

        if (modules.Count > 0 && !modules.Any(declared => SameName(owner, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, contracts)}' names no declared module.", contracts, 1, null));
            return;
        }

        CheckContractsLevel(config, contracts, findings);
    }

    /// <summary>Reports every folder under one module that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="module">The absolute module folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="nestedContracts">Whether a nested Contracts folder is allowed: only in the one-modules-project deviation.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ConsumerConfig config, string module, string moduleName, IReadOnlyList<(string Module, string Aggregate)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, bool nestedContracts, List<Finding> findings)
    {
        foreach (var child in ChildFolders(module))
        {
            var name = Path.GetFileName(child);

            if (ModuleFolders.Contains(name))
            {
                if (string.Equals(name, "Domain", StringComparison.OrdinalIgnoreCase))
                {
                    CheckDomainLevel(config, child, moduleName, aggregates, findings);
                }
                else if (string.Equals(name, "Application", StringComparison.OrdinalIgnoreCase))
                {
                    CheckApplicationLevel(config, child, moduleName, useCases, homes, findings);
                }
                else if (string.Equals(name, "Infrastructure", StringComparison.OrdinalIgnoreCase))
                {
                    CheckInfrastructureLevel(config, child, moduleName, useCases, findings);
                }
                else if (string.Equals(name, "WebApi", StringComparison.OrdinalIgnoreCase))
                {
                    CheckWebApiLevel(config, child, moduleName, useCases, findings);
                }

                continue;
            }

            if (nestedContracts && string.Equals(name, "Contracts", StringComparison.OrdinalIgnoreCase))
            {
                CheckContractsLevel(config, child, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no module folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the module contracts that names no kind folder, and every file beside them.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
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
            // The Contracts project file lives at the project root.
            if (Path.GetFileName(file).EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Contracts/, which holds only its kind folders.", file, 1, null));
        }
    }

    /// <summary>Reports every folder under a module Domain folder that names no declared aggregate of the module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="domain">The absolute Domain folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module and aggregate name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDomainLevel(ConsumerConfig config, string domain, string moduleName, IReadOnlyList<(string Module, string Aggregate)> aggregates, List<Finding> findings)
    {
        foreach (var child in ChildFolders(domain))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Count > 0 && !aggregates.Any(aggregate => SameName(moduleName, aggregate.Module) && SameAggregate(name, aggregate.Aggregate)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no declared aggregate.", child, 1, null));
                continue;
            }

            CheckAggregateLevel(config, child, findings);
        }
    }

    /// <summary>Reports every folder under one aggregate in Domain that names no Events or Exceptions folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Domain.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckAggregateLevel(ConsumerConfig config, string aggregate, List<Finding> findings)
    {
        foreach (var child in ChildFolders(aggregate))
        {
            var name = Path.GetFileName(child);

            if (AggregateSubFolders.Contains(name))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Events or Exceptions folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under a module Application folder that names no declared use case of the module and no fixed Application folder.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="application">The absolute Application folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckApplicationLevel(ConsumerConfig config, string application, string moduleName, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        // Every use case lives directly under Application, whatever aggregate its
        // record sits under.
        var owned = declared
            .Where(useCase => SameName(moduleName, useCase.Module))
            .ToList();

        foreach (var child in ChildFolders(application))
        {
            var name = Path.GetFileName(child);

            if (ApplicationFolders.Contains(name))
            {
                if (string.Equals(name, "Ports", StringComparison.OrdinalIgnoreCase))
                {
                    CheckUseCaseFilesOnly(config, child, "Ports", findings);
                }
                else
                {
                    CheckHomeLevel(config, child, moduleName, name, homes, findings);
                }

                continue;
            }

            if (owned.Any(useCase => SameName(name, useCase.Name)))
            {
                CheckUseCaseFolder(config, child, "Application", findings);
                continue;
            }

            if (owned.Count == 0)
            {
                CheckUseCaseFolder(config, child, "Application", findings);
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
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Application/, which holds only one folder per use case, Ports, Notifications, ExternalSystems and ReadModels.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a module WebApi folder that names no declared use case of the module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="webApi">The absolute WebApi folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckWebApiLevel(ConsumerConfig config, string webApi, string moduleName, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, List<Finding> findings)
    {
        var owned = declared
            .Where(useCase => SameName(moduleName, useCase.Module))
            .ToList();

        foreach (var child in ChildFolders(webApi))
        {
            var name = Path.GetFileName(child);

            if (owned.Any(useCase => SameName(name, useCase.Name)))
            {
                CheckUseCaseFolder(config, child, "WebApi", findings);
                continue;
            }

            if (owned.Count == 0)
            {
                CheckUseCaseFolder(config, child, "WebApi", findings);
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
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in WebApi/, which holds only one folder per use case.", file, 1, null));
            }
        }
    }

    /// <summary>Reports every folder under a module Infrastructure folder that names no Persistence or ExternalSystems folder and no declared use case of the module.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="infrastructure">The absolute Infrastructure folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="declared">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckInfrastructureLevel(ConsumerConfig config, string infrastructure, string moduleName, IReadOnlyList<(string Module, string? Aggregate, string Name)> declared, List<Finding> findings)
    {
        var owned = declared
            .Where(useCase => SameName(moduleName, useCase.Module))
            .ToList();

        foreach (var child in ChildFolders(infrastructure))
        {
            var name = Path.GetFileName(child);

            if (InfrastructureFolders.Contains(name))
            {
                continue;
            }

            if (owned.Any(useCase => SameName(name, useCase.Name)))
            {
                CheckUseCaseFolder(config, child, "Infrastructure", findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no Infrastructure folder.", child, 1, null));
        }
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

    /// <summary>Reports every subfolder under a folder that holds files only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="folder">The absolute folder that holds files only.</param>
    /// <param name="kind">The folder kind as printed: use case, Ports, Notifications, ExternalSystems or ReadModels.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckUseCaseFilesOnly(ConsumerConfig config, string folder, string kind, List<Finding> findings)
    {
        foreach (var child in ChildFolders(folder))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' sits in {kind} '{Path.GetFileName(folder)}', which holds files only.", child, 1, null));
        }
    }

    /// <summary>
    ///     Reports the layout of one use case folder: any subfolder, and each file that sits in the
    ///     wrong layer. A transport type, such as an endpoint or a request or response model, belongs
    ///     in WebApi, and every other file belongs in Application or in Infrastructure for the use
    ///     case's own adapters. The folder decides what a model or row is: beside the endpoint it
    ///     shapes the wire contract, while in Application it is a result part the query projects.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The absolute use case folder under Application, WebApi or Infrastructure.</param>
    /// <param name="layer">The layer folder holding the use case folder: Application, WebApi or Infrastructure.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/application#use-this-use-case-layout</follows>
    private static void CheckUseCaseFolder(ConsumerConfig config, string useCase, string layer, List<Finding> findings)
    {
        CheckUseCaseFilesOnly(config, useCase, "use case", findings);
        CheckTransportFiles(config, useCase, layer, findings);
    }

    /// <summary>
    ///     Reports each file in a use case folder that sits in the wrong layer: a transport type, such as an
    ///     endpoint or a request or response model, belongs in WebApi, and every other file belongs in Application
    ///     or in Infrastructure for the use case's own adapters. The folder decides what a model or row is: beside the
    ///     endpoint it shapes the wire contract, while in Application it is a result part the query projects.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The use case folder under Application, WebApi or Infrastructure.</param>
    /// <param name="layer">The layer folder holding the use case folder: Application, WebApi or Infrastructure.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/application#use-this-use-case-layout</follows>
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
    ///     The declared trigger and read model folders by module, container and folder name. A reaction or job that runs a use
    ///     case takes no folder here, since its code sits in that use case's folder under Application. A trigger that only sends
    ///     takes a Notifications folder, and one that only calls takes an ExternalSystems folder, each named for its first
    ///     effect. Each read model takes a ReadModels folder, named for its plural.
    /// </summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, container and folder name of each trigger and read model placed in a module folder.</returns>
    /// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
    /// <follows>litespec/backend/dotnet/architecture#place-each-read-model-in-its-application-read-models-folder</follows>
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
                if (TriggerHomeOf(record) is { } home && home.Container != "Application")
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
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding the files.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
    private static void CheckMessageFiles(ConsumerConfig config, string modulesRoot, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, List<Finding> findings)
    {
        var single = SourceRoots.IsSingleModulesProject(modulesRoot);
        var own = single ? null : SourceRoots.ModuleNameOf(modulesRoot);

        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories).OrderBy(child => child, StringComparer.Ordinal))
        {
            var segments = PathText.Relative(modulesRoot, file).Split('/');

            if (segments.Length < 2
                || segments.Any(segment => SourceTree.SkippedFolders.Contains(segment))
                || segments[0].Equals("Composition", StringComparison.OrdinalIgnoreCase))
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
            var belongs = single
                ? $"{segments[0]}/{layer}/{useCaseFolder}"
                : $"{layer}/{useCaseFolder}";

            findings.Add(new Finding("code-placement-drift", $"File '{relative}' holds a message, result or transport model outside a use case folder; it belongs in '{belongs}'.", file, 1, null));
        }
    }

    /// <summary>Whether a module-relative path sits in a use case folder under Application, WebApi or Infrastructure.</summary>
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
