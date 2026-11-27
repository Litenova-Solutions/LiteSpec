using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the layer folders: each Application and WebApi folder with its aggregate and use case folders, the trigger and settings homes, and each use case folder with its layer files.</summary>
internal static partial class ClosedFolderChecks
{
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

            if (CheckCrossModuleTriggerFolder(config, child, moduleName, homes, findings))
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

    /// <summary>
    ///     Checks one folder under a module Application folder against the homes of the module's reactions and jobs that run
    ///     another module's use case: a module-level home is a use case folder holding files only, and an aggregate home holds
    ///     only the use case folders its triggers run, each holding files only.
    /// </summary>
    /// <param name="config">The project configuration that holds the consumer root for relative paths.</param>
    /// <param name="child">The absolute folder directly under Application.</param>
    /// <param name="moduleName">The module folder name as written on disk.</param>
    /// <param name="homes">The declared trigger and read model folders by module, container and folder name.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    /// <returns>True when the folder is a cross-module trigger home, so the caller reports nothing more for it.</returns>
    /// <follows>litespec/workspace/naming#place-each-trigger-with-what-it-runs</follows>
    private static bool CheckCrossModuleTriggerFolder(ConsumerConfig config, string child, string moduleName, IReadOnlyList<(string Module, string Container, string Folder)> homes, List<Finding> findings)
    {
        var name = Path.GetFileName(child);
        var mine = homes.Where(home => SameName(moduleName, home.Module)).ToList();

        if (mine.Any(home => home.Container == "Application" && string.Equals(home.Folder, name, StringComparison.OrdinalIgnoreCase)))
        {
            CheckUseCaseFilesOnly(config, child, "use case", findings);
            return true;
        }

        var useCases = mine
            .Where(home => string.Equals(home.Container, $"Application/{name}", StringComparison.OrdinalIgnoreCase))
            .Select(home => home.Folder)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (useCases.Count == 0)
        {
            return false;
        }

        foreach (var folder in ChildFolders(child))
        {
            if (useCases.Contains(Path.GetFileName(folder)))
            {
                CheckUseCaseFilesOnly(config, folder, "use case", findings);
            }
            else
            {
                findings.Add(new Finding("code-placement-drift", $"Folder '{PathText.Relative(config.Root, folder)}' names no declared use case.", folder, 1, null));
            }
        }

        foreach (var file in ChildFiles(child))
        {
            findings.Add(new Finding("code-placement-drift", $"File '{PathText.Relative(config.Root, file)}' sits directly in an aggregate folder, which holds only use case folders.", file, 1, null));
        }

        return true;
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
}
