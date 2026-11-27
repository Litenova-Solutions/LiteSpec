using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Model;
using static LiteSpec.Cli.Model.Checks.Implementation.Placement.PlacementPath;

namespace LiteSpec.Cli.Model.Checks.Implementation.Placement;

/// <summary>The closed-folder check for the declared records: the modules, aggregates, use cases, facts, ports and trigger homes the folder checks read.</summary>
internal static partial class ClosedFolderChecks
{
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
    ///     case of its own module takes no folder here, since its code sits in that use case's folder under Application. One that
    ///     runs another module's use case takes the folder of that use case inside its own module: container
    ///     <c>Application/{Aggregates}</c>, or <c>Application</c> for a module-level use case. A trigger that only sends
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
                if (TriggerHomeOf(model, record) is not { } home)
                {
                    continue;
                }

                if (home.Container != "Application")
                {
                    homes.Add((path.Module, home.Container, home.Name));
                }
                else if (FirstIdOf(record.EdgeIds(RecordFields.Runs)) is { } run
                    && model.ById.TryGetValue(run, out var useCase)
                    && PathOf(useCase).Module is { } runModule
                    && !SameName(runModule, path.Module))
                {
                    homes.Add((path.Module, home.Aggregate is null ? "Application" : $"Application/{home.Aggregate}", home.Name));
                }
            }
            else if (record.Type == "read-model" && path.Name is not null)
            {
                homes.Add((path.Module, "ReadModels", ReadModelFolderOf(path.Name)));
            }
        }

        return homes;
    }
}
