using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the module roots: the single modules project, each module project, each Contracts project, and the module level inside each one.</summary>
internal static partial class ClosedFolderChecks
{
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
}
