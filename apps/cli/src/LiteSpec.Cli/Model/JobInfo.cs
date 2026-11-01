namespace LiteSpec.Model;

/// <summary>One job host, linked to its job through its mark.</summary>
sealed record JobInfo(
    string Class,
    string File,
    int Line,
    string? UseCaseId,
    string? RecordId = null,
    IReadOnlyList<CodeRepository>? Repositories = null,
    IReadOnlyList<CodeRefusal>? Refusals = null,
    IReadOnlyList<CodeThrown>? Thrown = null,
    IReadOnlyList<CodeDispatch>? Dispatches = null,
    int DependencyCount = 0,
    IReadOnlyList<CodeCollaborator>? Collaborators = null,
    IReadOnlyList<CodeLoopRead>? LoopReads = null);
