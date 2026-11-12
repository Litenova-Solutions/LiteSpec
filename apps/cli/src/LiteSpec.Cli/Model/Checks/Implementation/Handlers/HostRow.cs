using LiteSpec.Cli.Model.Code;

namespace LiteSpec.Cli.Model.Checks.Implementation.Handlers;

/// <summary>One event handler or job host row: the holder ids with the shape the checks read.</summary>
internal sealed record HostRow(
    string Class,
    string File,
    int Line,
    string? RecordId,
    string? UseCaseId,
    IReadOnlyList<CodeRepository>? Repositories,
    IReadOnlyList<CodeRefusal>? Refusals,
    IReadOnlyList<CodeThrown>? Thrown,
    IReadOnlyList<CodeDispatch>? Dispatches,
    int DependencyCount,
    IReadOnlyList<CodeCollaborator>? Collaborators,
    IReadOnlyList<CodeLoopRead>? LoopReads)
{
    /// <summary>Every event handler and job host as one row each, reactions then jobs.</summary>
    /// <param name="model">The loaded project model that holds each scanned reaction and job host.</param>
    /// <returns>One row per host, reactions first and jobs after.</returns>
    internal static IReadOnlyList<HostRow> All(ProjectModel model)
    {
        var hosts = new List<HostRow>(model.Code.Reactions.Count + model.Code.Jobs.Count);

        foreach (var reaction in model.Code.Reactions)
        {
            hosts.Add(new HostRow(
                reaction.Class, reaction.File, reaction.Line, reaction.RecordId, reaction.UseCaseId,
                reaction.Repositories, reaction.Refusals, reaction.Thrown, reaction.Dispatches,
                reaction.DependencyCount, reaction.Collaborators, reaction.LoopReads));
        }

        foreach (var job in model.Code.Jobs)
        {
            hosts.Add(new HostRow(
                job.Class, job.File, job.Line, job.RecordId, job.UseCaseId,
                job.Repositories, job.Refusals, job.Thrown, job.Dispatches,
                job.DependencyCount, job.Collaborators, job.LoopReads));
        }

        return hosts;
    }
}
