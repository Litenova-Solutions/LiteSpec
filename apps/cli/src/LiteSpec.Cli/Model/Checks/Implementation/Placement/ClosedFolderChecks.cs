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

/// <summary>The closed-folder check: the Kernel, the Platform, each module project, each Contracts project, each aggregate and use case folder, each port folder with its adapter folder, and each frontend modules root hold only their fixed folders and the folders a declared record names.</summary>
/// <follows>litespec/backend/dotnet/architecture#keep-closed-folder-sets-at-every-level</follows>
/// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
/// <follows>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
/// <follows>litespec/backend/application#use-this-use-case-layout</follows>
/// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
internal static class ClosedFolderChecks
{
    /// <summary>Fixed folders at the Platform root: one folder per technical capability.</summary>
    static readonly HashSet<string> PlatformFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence", "Messaging", "Access", "Auditing", "Http", "CountryPacks", "Modules",
    };

    /// <summary>Fixed folders at a module project root, whatever its aggregates, use cases, ports, triggers and read models declare.</summary>
    static readonly HashSet<string> ModuleFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Domain", "Application", "Ports", "Infrastructure", "WebApi",
    };

    /// <summary>Fixed folders under one aggregate in Domain: its events and refusals.</summary>
    static readonly HashSet<string> AggregateSubFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Events", "Exceptions",
    };

    /// <summary>Fixed folders under a module Application folder besides its use case folders: its trigger homes.</summary>
    static readonly HashSet<string> ApplicationFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notifications", "ExternalSystems", "ReadModels",
    };

    /// <summary>Fixed folders under a module Infrastructure folder besides its adapter folders: its persistence.</summary>
    static readonly HashSet<string> InfrastructureFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Persistence",
    };

    /// <summary>The layer folders a use case folder sits under.</summary>
    static readonly HashSet<string> UseCaseHomeLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Application", "WebApi",
    };

    /// <summary>Fixed folders under a layer folder that hold no use case: the trigger homes and persistence.</summary>
    static readonly HashSet<string> FixedLayerFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notifications", "ExternalSystems", "ReadModels", "Persistence",
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

        foreach (var modulesRoot in model.Config.ModulesRoots)
        {
            if (modulesRoot.Length == 0 || !Directory.Exists(modulesRoot))
            {
                continue;
            }

            if (SourceRoots.IsSingleModulesProject(modulesRoot))
            {
                CheckSingleModulesRoot(model.Config, modulesRoot, modules, aggregates, useCases, publicUseCases, homes, findings);
            }
            else
            {
                CheckModuleProject(model.Config, modulesRoot, modules, aggregates, useCases, publicUseCases, homes, findings);
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
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckSingleModulesRoot(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        foreach (var module in ChildFolders(modulesRoot))
        {
            var name = Path.GetFileName(module);

            if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, module)}' names no declared module.", module, 1, null));
                continue;
            }

            CheckModuleLevel(config, module, name, aggregates, useCases, publicUseCases, homes, nestedContracts: true, findings);
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
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleProject(ConsumerConfig config, string modulesRoot, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        if (SourceRoots.IsContractsProject(modulesRoot))
        {
            CheckContractsProject(config, modulesRoot, modules, aggregates, useCases, publicUseCases, findings);
            return;
        }

        var name = SourceRoots.ModuleNameOf(modulesRoot) ?? Path.GetFileName(modulesRoot);

        if (modules.Count > 0 && !modules.Any(declared => SameName(name, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, modulesRoot)}' names no declared module.", modulesRoot, 1, null));
            return;
        }

        CheckModuleLevel(config, modulesRoot, name, aggregates, useCases, publicUseCases, homes, nestedContracts: false, findings);
    }

    /// <summary>Reports the Contracts project whose owner names no declared module, and every folder under it that breaks the Contracts closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The Contracts project holding one module's contracts.</param>
    /// <param name="modules">The declared module names.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckContractsProject(ConsumerConfig config, string contracts, List<string> modules, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        var owner = SourceRoots.ModuleNameOf(contracts) ?? Path.GetFileName(contracts);

        if (modules.Count > 0 && !modules.Any(declared => SameName(owner, declared)))
        {
            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, contracts)}' names no declared module.", contracts, 1, null));
            return;
        }

        CheckContractsLevel(config, contracts, owner, aggregates, useCases, publicUseCases, findings);
    }

    /// <summary>Reports every folder under one module that breaks the module level's closed set.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="module">The absolute module folder.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="nestedContracts">Whether a nested Contracts folder is allowed: only in the one-modules-project deviation.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckModuleLevel(ConsumerConfig config, string module, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, IReadOnlyList<(string Module, string Container, string Folder)> homes, bool nestedContracts, List<Finding> findings)
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
                    CheckDomainLevel(config, child, moduleName, aggregates, findings);
                }
                else if (string.Equals(name, "Application", StringComparison.OrdinalIgnoreCase))
                {
                    CheckApplicationLevel(config, child, moduleName, aggregates, useCases, homes, findings);
                }
                else if (string.Equals(name, "Ports", StringComparison.OrdinalIgnoreCase))
                {
                    CheckPortsLevel(config, child, adapterNames, findings);
                }
                else if (string.Equals(name, "Infrastructure", StringComparison.OrdinalIgnoreCase))
                {
                    CheckInfrastructureLevel(config, child, portNames, findings);
                }
                else if (string.Equals(name, "WebApi", StringComparison.OrdinalIgnoreCase))
                {
                    CheckWebApiLevel(config, child, moduleName, aggregates, useCases, findings);
                }

                continue;
            }

            if (nestedContracts && string.Equals(name, "Contracts", StringComparison.OrdinalIgnoreCase))
            {
                CheckContractsLevel(config, child, moduleName, aggregates, useCases, publicUseCases, findings);
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no module folder.", child, 1, null));
        }
    }

    /// <summary>Reports every folder under the module contracts that names no declared aggregate or public module-level use case of the owner, every file directly beside them, every interface, and every command of no public use case.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsLevel(ConsumerConfig config, string contracts, string owner, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        foreach (var child in ChildFolders(contracts))
        {
            var name = Path.GetFileName(child);

            if (aggregates.Any(aggregate => SameName(owner, aggregate.Module) && SameName(name, aggregate.Expected)))
            {
                CheckContractsAggregate(config, child, owner, name, aggregates, useCases, publicUseCases, findings);
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

    /// <summary>Reports every folder under one Contracts aggregate folder that names no public use case of that aggregate, and every query or command message or result directly beside them.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="aggregate">The absolute aggregate folder under Contracts.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="name">The aggregate folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
    private static void CheckContractsAggregate(ConsumerConfig config, string aggregate, string owner, string name, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, IReadOnlyList<(string Module, string? Aggregate, string Name)> useCases, IReadOnlyList<(string Module, string? Aggregate, string Name, string Id, string Kind)> publicUseCases, List<Finding> findings)
    {
        var owned = publicUseCases
            .Where(useCase => SameName(owner, useCase.Module)
                && useCase.Aggregate is not null
                && SameName(name, ExpectedOf(aggregates, owner, useCase.Aggregate)))
            .ToList();

        foreach (var child in ChildFolders(aggregate))
        {
            var childName = Path.GetFileName(child);

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

            findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, child)}' names no public use case of aggregate '{name}'.", child, 1, null));
        }

        foreach (var file in ChildFiles(aggregate))
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
                var shape = kind == "query" ? "a query message" : "a command message";
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds {shape} outside a use case folder.", file, 1, null));
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' holds the message of public {kind} '{match.Id}'; it belongs in '{Naming.Pascal(match.Name)}/'.", file, 1, null));
        }
    }

    /// <summary>Reports every interface and every command file under the module contracts that is no public command's message in its use case folder, which publish data only.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="contracts">The absolute Contracts project or Contracts folder of one module.</param>
    /// <param name="owner">The owner module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="publicUseCases">The public use cases by module, aggregate, name, identifier and kind.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
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
        // aggregate folder, and a file nested past a use case folder, so those stay silent here.
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
    /// <follows>litespec/backend/dotnet/architecture#publish-only-data-from-each-contracts-project</follows>
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

    /// <summary>Whether the identifier names a module.</summary>
    /// <param name="id">The identifier under test.</param>
    /// <returns>True when the identifier opens with the module prefix.</returns>
    private static bool IsModuleId(string id) =>
        Identifiers.TrySplit(id, out var prefix, out _, out _) && prefix == RecordTypes.Module;

    /// <summary>Reports every folder under a module Domain folder that names no declared aggregate of the module: one plural folder per declared aggregate, so a singular folder reports too.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="domain">The absolute Domain folder of one module.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckDomainLevel(ConsumerConfig config, string domain, string moduleName, IReadOnlyList<(string Module, string Aggregate, string Expected)> aggregates, List<Finding> findings)
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
                CheckHomeLevel(config, child, moduleName, name, homes, findings);
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
                findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in Application/, which holds only one folder per aggregate and use case, Notifications, ExternalSystems and ReadModels.", file, 1, null));
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

    /// <summary>Reports every folder under a module Infrastructure folder that names no Persistence folder and no port folder of the module: each adapter folder pairs with one port folder of the same name.</summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="infrastructure">The absolute Infrastructure folder of one module.</param>
    /// <param name="portNames">The port folder names under the module Ports folder.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckInfrastructureLevel(ConsumerConfig config, string infrastructure, IReadOnlyList<string> portNames, List<Finding> findings)
    {
        foreach (var child in ChildFolders(infrastructure))
        {
            var name = Path.GetFileName(child);

            if (InfrastructureFolders.Contains(name))
            {
                continue;
            }

            if (portNames.Any(port => SameName(name, port)))
            {
                continue;
            }

            findings.Add(new Finding("code-placement-drift", $"Adapter folder '{PathText.Relative(config.Root, child)}' names no port folder; its port sits in 'Ports/{name}/'.", child, 1, null));
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

    /// <summary>
    ///     Reports every file directly in a module Ports folder, which holds only one folder
    ///     per port, every port folder with no adapter folder of the same name, and checks
    ///     each port folder for its interface and named types.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="ports">The absolute Ports folder of one module.</param>
    /// <param name="adapterNames">The adapter folder names under the module Infrastructure folder, without Persistence.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortsLevel(ConsumerConfig config, string ports, IReadOnlyList<string> adapterNames, List<Finding> findings)
    {
        foreach (var child in ChildFolders(ports))
        {
            CheckPortFolder(config, child, findings);

            var name = Path.GetFileName(child);

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

    /// <summary>
    ///     Reports the shape of one port folder: no interface or more than one, an interface
    ///     whose name is not the folder name with an I prefix, and a type no member signature of the
    ///     interface names, directly or through one named type. A port folder holds files only.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/dotnet/architecture#keep-application-types-in-their-layer-folders</follows>
    private static void CheckPortFolder(ConsumerConfig config, string port, List<Finding> findings)
    {
        CheckUseCaseFilesOnly(config, port, "port", findings);

        var name = Path.GetFileName(port);
        var relative = PathText.Relative(config.Root, port);
        var types = PortTypesOf(port);
        var faces = types.Where(type => type.IsInterface).OrderBy(type => type.Name, StringComparer.Ordinal).ToList();

        if (faces.Count == 0)
        {
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds no interface; it holds the interface 'I{name}' and only the types its signatures name.", port, 1, null));
            return;
        }

        if (faces.Count > 1)
        {
            var held = string.Join(", ", faces.Select(face => $"'{face.Name}'"));
            findings.Add(new Finding("code-placement-drift", $"Port folder '{relative}' holds {faces.Count} interfaces ({held}); it holds only the interface 'I{name}' and only the types its signatures name.", port, 1, null));
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

        foreach (var type in types)
        {
            if (!type.IsInterface && !named.Contains(type.Name))
            {
                findings.Add(new Finding("code-placement-drift", $"Type '{type.Name}' in port folder '{relative}' is named by no signature of '{face.Name}'; move it to Domain/{{Aggregates}}/ or the use case folder that uses it.", type.File, type.Line, null));
            }
        }
    }

    /// <summary>One top-level type declared in a port folder: its name, file, line, whether it is an interface, and the simple type names its member signatures name.</summary>
    /// <param name="Name">The simple type name as declared.</param>
    /// <param name="File">The file declaring the type.</param>
    /// <param name="Line">The 1-based line of the type name.</param>
    /// <param name="IsInterface">Whether the declaration is an interface.</param>
    /// <param name="SignatureNames">The simple type names its members name: parameter and return types with their type arguments, then property, field and event types.</param>
    sealed record PortDeclaredType(string Name, string File, int Line, bool IsInterface, HashSet<string> SignatureNames);

    /// <summary>The top-level types each C# file directly in a port folder declares, in file order, with the simple type names each member signature names.</summary>
    /// <param name="port">The absolute port folder under Ports.</param>
    /// <returns>The declared types in file order, each with its signature names; a file that cannot be read reads as empty.</returns>
    private static List<PortDeclaredType> PortTypesOf(string port)
    {
        var types = new List<PortDeclaredType>();

        foreach (var file in ChildFiles(port))
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

                if (declared is TypeDeclarationSyntax type)
                {
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
                types.Add(new PortDeclaredType(identifier.ValueText, file, line, declared is InterfaceDeclarationSyntax, names));
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
    /// <param name="kind">The folder kind as printed: use case, port, Notifications, ExternalSystems or ReadModels.</param>
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
    ///     in WebApi, and every other file belongs in Application. The folder decides what a model or
    ///     row is: beside the endpoint it shapes the wire contract, while in Application it is a
    ///     result part the query projects.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="useCase">The absolute use case folder under Application or WebApi.</param>
    /// <param name="layer">The layer folder holding the use case folder: Application or WebApi.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/application#use-this-use-case-layout</follows>
    private static void CheckUseCaseFolder(ConsumerConfig config, string useCase, string layer, List<Finding> findings)
    {
        CheckUseCaseFilesOnly(config, useCase, "use case", findings);
        CheckTransportFiles(config, useCase, layer, findings);
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

    /// <summary>The declared aggregates by module, aggregate folder and expected plural folder.</summary>
    /// <param name="model">The loaded project model that holds every record.</param>
    /// <returns>The module, aggregate folder and expected plural folder of each aggregate record placed in a module folder.</returns>
    private static List<(string Module, string Aggregate, string Expected)> DeclaredAggregates(ProjectModel model)
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
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="modulesRoot">The module project holding the files.</param>
    /// <param name="aggregates">The declared aggregates by module, aggregate folder and expected plural folder.</param>
    /// <param name="useCases">The declared use cases by module, aggregate and name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <follows>litespec/backend/architecture#give-each-use-case-its-own-models</follows>
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
