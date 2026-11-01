using System.Text.Json;

namespace LiteSpec.Model.Checks;

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
    IReadOnlyList<CodeLoopRead>? LoopReads);
