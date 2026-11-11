using LiteSpec.Model;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>The repository-use checks: no collaborator holds a repository, no repository is read inside a loop, a collection query filters in the store, and every aggregate repository is implemented.</summary>
/// <follows>litespec/backend/application#read-no-repository-inside-a-loop</follows>
/// <follows>litespec/backend/persistence#read-collection-queries-in-the-store-with-paging-and-filters</follows>
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
    ///     no Infrastructure folder carries no persistence layer for it to
    ///     judge, and an aggregate with no domain repository interface (a store,
    ///     or one whose Domain declares none) names nothing to implement.
    /// </summary>
    /// <param name="model">The loaded project model that holds every use case and the scanned repository implementations.</param>
    /// <param name="findings">The list that receives each finding this check raises.</param>
    private static void CheckRepositoryUnimplemented(ProjectModel model, List<Finding> findings)
    {
        if (!Directory.Exists(model.Config.InfrastructureRoot))
        {
            return;
        }

        var implemented = model.Code.RepositoryImplementations
            .Select(entry => entry.Interface)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var useCase in model.Records.Where(record => record.Type == RecordTypes.UseCase))
        {
            foreach (var aggregateId in useCase.Ids(RecordFields.Changes))
            {
                if (!model.ById.TryGetValue(aggregateId, out var aggregate) || aggregate.Type != RecordTypes.Aggregate)
                {
                    continue;
                }

                if (aggregate.Field(RecordFields.Kind) == "store")
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
