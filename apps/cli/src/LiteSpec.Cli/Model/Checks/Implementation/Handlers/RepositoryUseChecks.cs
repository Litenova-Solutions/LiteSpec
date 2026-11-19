using LiteSpec.Cli.Model.Checks.Implementation.Placement;
using LiteSpec.Cli.Model.Readers.Dotnet;
using LiteSpec.Cli.Model.Records;
using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The repository-use checks: no collaborator holds a repository, no repository is read inside a loop, a collection query filters in the store, every aggregate repository is implemented, and every repository implementation sits in its aggregate's Persistence folder.</summary>
/// <follows>litespec/backend/application#keep-repository-reads-out-of-loops</follows>
/// <follows>litespec/backend/persistence#page-and-filter-collection-queries-in-the-store</follows>
/// <follows>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</follows>
internal static class RepositoryUseChecks
{
    /// <summary>Runs the repository-use checks.</summary>
    /// <param name="model">The loaded project model that holds every record and the scanned code.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    internal static void Run(ProjectModel model, List<Finding> findings)
    {
        CheckCollaboratorHoldsRepository(model, findings);
        CheckRepositoryReadInLoop(model, findings);
        CheckQueryFiltersInMemory(model, findings);
        CheckRepositoryUnimplemented(model, findings);
        CheckRepositoryImplementationPlaced(model, findings);
    }

    /// <summary>The title and identifier a reaction or job finding names: the holder record when known, else the class.</summary>
    /// <param name="host">The host row whose class names the holder when no record does.</param>
    /// <param name="holder">The holder record identifier, or null when no record is known.</param>
    /// <returns>The prose kind title and the identifier the finding names.</returns>
    private static (string Title, string Id) HolderTitle(HostRow host, string? holder) =>
        holder switch
        {
            not null when holder.StartsWith("use-case/", StringComparison.Ordinal) => ("Use case", holder),
            not null when holder.StartsWith("reaction/", StringComparison.Ordinal) => ("Reaction", holder),
            not null when holder.StartsWith("job/", StringComparison.Ordinal) => ("Job", holder),
            not null => ("Reaction", holder),
            null => ("Reaction", host.Class),
        };

    /// <summary>Reports each collaborator a handler or host builds directly whose constructor takes a repository.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler and host.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckCollaboratorHoldsRepository(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.Collaborators is null)
            {
                continue;
            }

            foreach (var collaborator in handler.Collaborators)
            {
                findings.Add(new Finding(
                    "collaborator-repository-invalid",
                    $"Use case '{handler.UseCaseId}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository.",
                    collaborator.File,
                    collaborator.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var host in HostRow.All(model))
        {
            if (host.Collaborators is null)
            {
                continue;
            }

            var holder = host.RecordId ?? host.UseCaseId;
            var (kind, id) = HolderTitle(host, holder);

            foreach (var collaborator in host.Collaborators)
            {
                findings.Add(new Finding(
                    "collaborator-repository-invalid",
                    $"{kind} '{id}' builds '{collaborator.Type}' with 'new', whose constructor takes a repository.",
                    collaborator.File,
                    collaborator.Line,
                    holder));
            }
        }
    }

    /// <summary>Reports each repository read a handler or host performs inside a loop.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler and host.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRepositoryReadInLoop(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.LoopReads is null)
            {
                continue;
            }

            foreach (var read in handler.LoopReads)
            {
                findings.Add(new Finding(
                    "repository-read-invalid",
                    $"Use case '{handler.UseCaseId}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    handler.UseCaseId));
            }
        }

        foreach (var host in HostRow.All(model))
        {
            if (host.LoopReads is null)
            {
                continue;
            }

            var holder = host.RecordId ?? host.UseCaseId;
            var (kind, id) = HolderTitle(host, holder);

            foreach (var read in host.LoopReads)
            {
                findings.Add(new Finding(
                    "repository-read-invalid",
                    $"{kind} '{id}' reads repository '{read.Repository}' inside a loop with '{read.Method}'.",
                    read.File,
                    read.Line,
                    holder));
            }
        }
    }

    /// <summary>Reports each query handler that filters a repository list in memory.</summary>
    /// <param name="model">The loaded project model that holds each scanned handler.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckQueryFiltersInMemory(ProjectModel model, List<Finding> findings)
    {
        foreach (var handler in model.Code.Handlers)
        {
            if (handler.MemoryFilters is null)
            {
                continue;
            }

            foreach (var filter in handler.MemoryFilters)
            {
                findings.Add(new Finding(
                    "query-filter-invalid",
                    $"Use case '{handler.UseCaseId}' filters a repository list in memory.",
                    filter.File,
                    filter.Line,
                    handler.UseCaseId));
            }
        }
    }

    /// <summary>
    ///     Reports each aggregate a use case changes whose domain repository
    ///     interface no Infrastructure class implements. The check reads the
    ///     Infrastructure layer through the backend reader, so a consumer with
    ///     no Infrastructure folder in any module carries no persistence layer
    ///     for it to judge, and only writes entries naming an aggregate are
    ///     read: a store and an aggregate whose Domain declares no repository
    ///     name nothing to implement.
    /// </summary>
    /// <summary>Whether any module holds an Infrastructure folder: the module project itself, or one module folder under the single modules project.</summary>
    /// <param name="model">The loaded project model that holds the module roots.</param>
    /// <returns>True when an Infrastructure folder exists in any module.</returns>
    private static bool HasInfrastructureFolder(ProjectModel model)
    {
        foreach (var modules in model.Config.ModulesRoots)
        {
            if (modules.Length == 0 || !Directory.Exists(modules))
            {
                continue;
            }

            if (!SourceRoots.IsSingleModulesProject(modules))
            {
                if (Directory.Exists(Path.Combine(modules, "Infrastructure")))
                {
                    return true;
                }

                continue;
            }

            try
            {
                foreach (var module in Directory.EnumerateDirectories(modules))
                {
                    if (Directory.Exists(Path.Combine(module, "Infrastructure")))
                    {
                        return true;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }
        }

        return false;
    }

    /// <summary>Reports each repository implementation outside its aggregate's Persistence folder. The interface names the aggregate: <c>IExpenseRepository</c> implements the expense aggregate, so its class sits in <c>Infrastructure/Persistence/Expenses/</c>. An interface naming no declared aggregate states no folder, so it is left out.</summary>
    /// <param name="model">The loaded project model that holds every aggregate and the scanned repository implementations.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRepositoryImplementationPlaced(ProjectModel model, List<Finding> findings)
    {
        foreach (var implementation in model.Code.RepositoryImplementations
            .OrderBy(entry => entry.File, StringComparer.Ordinal)
            .ThenBy(entry => entry.Line))
        {
            if (AggregateOfRepository(model, implementation.Interface) is not { } aggregate)
            {
                continue;
            }

            var path = PlacementPath.PathOf(aggregate);

            if (path.Module is null || path.Aggregate is null)
            {
                continue;
            }

            var expected = PlacementPath.ExpectedAggregateFolder(model, path.Module, path.Aggregate);

            if (!UnderPersistenceAggregate(model, implementation.File, path.Module, expected))
            {
                findings.Add(new Finding(
                    "code-placement-drift",
                    $"Repository '{implementation.Class}' implements '{implementation.Interface}', so it belongs in 'Infrastructure/Persistence/{expected}/'.",
                    implementation.File,
                    implementation.Line,
                    aggregate.Id));
            }
        }
    }

    /// <summary>The declared aggregate a repository interface names, or null when it names none: the interface stem without its I prefix and Repository suffix meets the aggregate id name.</summary>
    /// <param name="model">The loaded project model that holds every aggregate record.</param>
    /// <param name="face">The repository interface name, such as IExpenseRepository.</param>
    /// <returns>The named aggregate record, or null.</returns>
    private static Record? AggregateOfRepository(ProjectModel model, string face)
    {
        const string prefix = "I";
        const string suffix = "Repository";

        if (!face.StartsWith(prefix, StringComparison.Ordinal)
            || !face.EndsWith(suffix, StringComparison.Ordinal)
            || face.Length <= prefix.Length + suffix.Length)
        {
            return null;
        }

        var stem = face[prefix.Length..^suffix.Length];

        foreach (var record in model.Records)
        {
            if (record.Type != RecordTypes.Aggregate
                || !Identifiers.TrySplit(record.Id, out _, out _, out var name)
                || name.Length == 0)
            {
                continue;
            }

            if (PlacementPath.SameName(stem, name))
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>Whether the file sits in the module's Persistence folder for the aggregate.</summary>
    /// <param name="model">The loaded project model that holds the module roots.</param>
    /// <param name="file">The full path of the file under test.</param>
    /// <param name="module">The module the aggregate sits in.</param>
    /// <param name="expected">The expected plural aggregate folder.</param>
    /// <returns>True when the file sits under Infrastructure/Persistence and the aggregate folder; otherwise false.</returns>
    private static bool UnderPersistenceAggregate(ProjectModel model, string file, string module, string expected)
    {
        foreach (var modules in model.Config.ModulesRoots)
        {
            if (modules.Length == 0 || !SourceRoots.IsUnder(modules, file))
            {
                continue;
            }

            string moduleDir;

            if (SourceRoots.IsSingleModulesProject(modules))
            {
                if (ModuleDirectory(modules, module) is not { } single)
                {
                    continue;
                }

                moduleDir = single;
            }
            else if (SourceRoots.IsContractsProject(modules))
            {
                continue;
            }
            else if (SourceRoots.ModuleNameOf(modules) is { } own && PlacementPath.SameName(own, module))
            {
                moduleDir = modules;
            }
            else
            {
                continue;
            }

            if (SourceRoots.IsUnder(Path.Combine(moduleDir, "Infrastructure", "Persistence", expected), file))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The module folder under the single modules root that spells the module, or null when none does.</summary>
    /// <param name="modules">The single modules project holding every module.</param>
    /// <param name="module">The module name the record states.</param>
    /// <returns>The module folder as written on disk, or null.</returns>
    private static string? ModuleDirectory(string modules, string module)
    {
        try
        {
            return Directory.EnumerateDirectories(modules)
                .FirstOrDefault(child => PlacementPath.SameName(Path.GetFileName(child), module));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Raises a finding for each written aggregate with no repository implementation.</summary>
    /// <param name="model">The loaded project model that holds every use case and the scanned repository implementations.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRepositoryUnimplemented(ProjectModel model, List<Finding> findings)
    {
        if (!HasInfrastructureFolder(model))
        {
            return;
        }

        var implemented = model.Code.RepositoryImplementations
            .Select(entry => entry.Interface)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            foreach (var aggregateId in useCase.Ids(RecordFields.Writes))
            {
                if (!model.ById.TryGetValue(aggregateId, out var aggregate) || aggregate.Type != RecordTypes.Aggregate)
                {
                    continue;
                }

                if (!Identifiers.TrySplit(aggregateId, out _, out _, out var name))
                {
                    continue;
                }

                var face = $"I{Naming.Pascal(name)}Repository";

                if (!model.Code.DomainTypes.Any(type => type.Name == face))
                {
                    continue;
                }

                if (implemented.Contains(face))
                {
                    continue;
                }

                findings.Add(new Finding(
                    "repository-unimplemented",
                    $"Aggregate '{aggregateId}' changed by '{useCase.Id}' has no Infrastructure repository implementing '{face}'.",
                    useCase.File,
                    useCase.Line,
                    useCase.Id));
            }
        }
    }
}
