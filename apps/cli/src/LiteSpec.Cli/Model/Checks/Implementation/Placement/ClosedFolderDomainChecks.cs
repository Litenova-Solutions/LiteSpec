using LiteSpec.Cli.Files;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Model;
using LiteSpec.Standards;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the domain: the module Domain folder with one plural folder per declared aggregate, and each aggregate with its fixed grouping folders.</summary>
internal static partial class ClosedFolderChecks
{
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
}
